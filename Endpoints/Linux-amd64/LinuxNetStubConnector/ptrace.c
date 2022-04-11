// ptrace function fork shenanigans adapted from https://github.com/eklitzke/ptrace-call-userspace

#include "ptrace.h"

// number of bytes in a JMP/CALL rel32 instruction
#define REL32_SZ 5

// copy in the string including the trailing null byte
static const char* format = "instruction pointer = %p\n";

// text seen in /proc/<pid>/maps for text areas
static const char* text_area = " r-xp ";

// this should be a string that will uniquely identify libc in /proc/<pid>/maps
static const char* libc_string = "/libc-2";

// find the location of a shared library in memory
void* find_library(pid_t pid, const char* libname) {
	char filename[32];
	snprintf(filename, sizeof(filename), "/proc/%d/maps", pid);
	FILE* f = fopen(filename, "r");
	char* line = NULL;
	size_t line_size = 0;

	while (getline(&line, &line_size, f) >= 0) {
		char* pos = strstr(line, libname);
		if (pos != NULL && strstr(line, text_area)) {
			long val = strtol(line, NULL, 16);
			free(line);
			fclose(f);
			return (void*)val;
		}
	}
	free(line);
	fclose(f);
	return NULL;
}

// Update the text area of pid at the area starting at where. The data copied
// should be in the new_text buffer whose size is given by len. If old_text is
// not null, the original text data will be copied into it. Therefore old_text
// must have the same size as new_text.
int poke_text(pid_t pid, void* where, void* new_text, void* old_text,
	size_t len) {
	if (len % sizeof(void*) != 0) {
		return -1;
	}

	unsigned long poke_data;
	for (size_t copied = 0; copied < len; copied += sizeof(poke_data)) {
		memmove(&poke_data, new_text + copied, sizeof(poke_data));
		if (old_text != NULL) {
			errno = 0;
			long peek_data = ptrace(PTRACE_PEEKTEXT, pid, where + copied, NULL);
			if (peek_data == -1 && errno) {
				perror("PTRACE_PEEKTEXT");
				return -1;
			}
			memmove(old_text + copied, &peek_data, sizeof(peek_data));
		}
		if (ptrace(PTRACE_POKETEXT, pid, where + copied, (void*)poke_data) < 0) {
			perror("PTRACE_POKETEXT");
			return -1;
		}
	}
	return 0;
}

int do_wait(const char* name) {
	int status;
	if (wait(&status) == -1) {
		perror("wait");
		return -1;
	}
	if (WIFSTOPPED(status)) {
		if (WSTOPSIG(status) == SIGTRAP) {
			return 0;
		}
		return -1;
	}
	return -1;

}

int singlestep(pid_t pid) {
	if (ptrace(PTRACE_SINGLESTEP, pid, NULL, NULL)) {
		perror("PTRACE_SINGLESTEP");
		return -1;
	}
	return do_wait("PTRACE_SINGLESTEP");
}

void check_yama(void) {
	FILE* yama_file = fopen("/proc/sys/kernel/yama/ptrace_scope", "r");
	if (yama_file == NULL) {
		return;
	}
	char yama_buf[8];
	memset(yama_buf, 0, sizeof(yama_buf));
	fread(yama_buf, 1, sizeof(yama_buf), yama_file);
	if (strcmp(yama_buf, "0\n") != 0) {
		printf("\nThe likely cause of this failure is that your system has "
			"kernel.yama.ptrace_scope = %s",
			yama_buf);
		printf("If you would like to disable Yama, you can run: "
			"sudo sysctl kernel.yama.ptrace_scope=0\n");
	}
	fclose(yama_file);
}

int32_t compute_jmp(void* from, void* to) {
	int64_t delta = (int64_t)to - (int64_t)from - REL32_SZ;
	if (delta < INT_MIN || delta > INT_MAX) {
		printf("cannot do relative jump of size %li; did you compile with -fPIC?\n",
			delta);
		exit(1);
	}
	return (int32_t)delta;

}

void* alloc_rwx_on_process(pid_t pid, size_t alloc_size) {
	// attach to the process
	if (ptrace(PTRACE_ATTACH, pid, NULL, NULL)) {
		perror("PTRACE_ATTACH");
		check_yama();
		return -1;
	}

	// wait for the process to actually stop
	if (waitpid(pid, 0, WUNTRACED | WCONTINUED) == -1) {
		perror("wait");
		return -1;
	}

	// save the register state of the remote process
	struct user_regs_struct oldregs;
	if (ptrace(PTRACE_GETREGS, pid, NULL, &oldregs)) {
		perror("PTRACE_GETREGS");
		ptrace(PTRACE_DETACH, pid, NULL, NULL);
		return -1;
	}
	void* rip = (void*)oldregs.rip;

	// First, we are going to allocate some memory for ourselves so we don't
	// need
	// to stop on the remote process' memory. We will do this by directly
	// invoking
	// the mmap(2) system call and asking for a single page.
	struct user_regs_struct newregs;
	memmove(&newregs, &oldregs, sizeof(newregs));
	newregs.rax = 9;                           // mmap
	newregs.rdi = 0;                           // addr
	newregs.rsi = alloc_size;                   // length
	newregs.rdx = PROT_READ | PROT_WRITE | PROT_EXEC;       // prot
	newregs.r10 = MAP_PRIVATE | 0x20; // flags
	newregs.r8 = -1;                           // fd
	newregs.r9 = 0;                            //  offset

	uint8_t old_word[8];
	uint8_t new_word[8];
	new_word[0] = 0x0f; // SYSCALL
	new_word[1] = 0x05; // SYSCALL
	new_word[2] = 0xff; // JMP %rax
	new_word[3] = 0xe0; // JMP %rax

	// insert the SYSCALL instruction into the process, and save the old word
	if (poke_text(pid, rip, new_word, old_word, sizeof(new_word))) {
		poke_text(pid, rip, old_word, NULL, sizeof(old_word));
		if (ptrace(PTRACE_DETACH, pid, NULL, NULL)) {
			perror("PTRACE_DETACH");
		}
		return NULL;
	}

	// set the new registers with our syscall arguments
	if (ptrace(PTRACE_SETREGS, pid, NULL, &newregs)) {
		perror("PTRACE_SETREGS");
		poke_text(pid, rip, old_word, NULL, sizeof(old_word));
		if (ptrace(PTRACE_DETACH, pid, NULL, NULL)) {
			perror("PTRACE_DETACH");
		}
		return NULL;
	}

	// invoke mmap(2)
	if (singlestep(pid)) {
		poke_text(pid, rip, old_word, NULL, sizeof(old_word));
		if (ptrace(PTRACE_DETACH, pid, NULL, NULL)) {
			perror("PTRACE_DETACH");
		}
		return NULL;
	}

	// read the new register state, so we can see where the mmap went
	if (ptrace(PTRACE_GETREGS, pid, NULL, &newregs)) {
		perror("PTRACE_GETREGS");
		return -1;
	}

	// this is the address of the memory we allocated
	void* mmap_memory = (void*)newregs.rax;
	if (mmap_memory == (void*)-1) {
		poke_text(pid, rip, old_word, NULL, sizeof(old_word));
		if (ptrace(PTRACE_DETACH, pid, NULL, NULL)) {
			perror("PTRACE_DETACH");
		}
		return NULL;
	}

	poke_text(pid, rip, old_word, NULL, sizeof(old_word));

	if (ptrace(PTRACE_SETREGS, pid, NULL, &oldregs)) {
		perror("PTRACE_SETREGS");
		goto fail;
	}

	// detach the process
	if (ptrace(PTRACE_DETACH, pid, NULL, NULL)) {
		perror("PTRACE_DETACH");
		goto fail;
	}

	return mmap_memory;

fail:
	poke_text(pid, rip, old_word, NULL, sizeof(old_word));
	if (ptrace(PTRACE_DETACH, pid, NULL, NULL)) {
		perror("PTRACE_DETACH");
	}
	return NULL;
}