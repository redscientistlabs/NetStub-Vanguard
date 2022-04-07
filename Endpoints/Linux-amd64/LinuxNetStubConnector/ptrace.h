#ifdef __cplusplus
extern "C" {
#endif

#include <assert.h>
#include <ctype.h>
#include <dlfcn.h>
#include <errno.h>
#include <getopt.h>
#include <limits.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <sys/ptrace.h>
#include <sys/mman.h>
#include <sys/types.h>
#include <sys/user.h>
#include <sys/wait.h>
#include <sys/sysinfo.h>
#include <unistd.h>

void* alloc_rwx_on_process(pid_t pid, size_t alloc_size);

#ifdef __cplusplus
}
#endif