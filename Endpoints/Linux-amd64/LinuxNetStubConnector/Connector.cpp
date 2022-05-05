#include "Connector.h"
#include <sstream>
#include <boost/regex.hpp>
#include <boost/filesystem.hpp>
#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <cstdio>
#include <iostream>
#include <memory>
#include <stdexcept>
#include <string>
#include <array>
#include <sys/uio.h>
#include <sys/ptrace.h>
#include <sys/sysinfo.h>
#include <sys/wait.h>
#include <fmt/format.h>
#include <map>
#include <iostream>
#include <fstream>

int g_pid = 0;
std::vector<RPC_PROC_MAP_INFO> g_maps;

namespace fs = boost::filesystem;

void ProcessManager::BindProcess(int pid)
{
	g_pid = pid;
	//ptrace(PTRACE_ATTACH, pid, 0, 0);
}

void ProcessManager::UnbindProcess()
{
	//ptrace(PTRACE_DETACH, g_pid, 0, 0);
	g_pid = 0;
}

uint64_t ProcessManager::Read(uint64_t pid, uint64_t addr, void* val, uint64_t size)
{
	
	ptrace(PTRACE_ATTACH, pid, 0, 0);
	int status32 = 0;
	if (waitpid(pid, &status32, 0) == -1 || !WIFSTOPPED(status32)) {
		return status32;
	}
	/*struct iovec local {}, remote{};
	local.iov_base = &val;
	local.iov_len = size;
	remote.iov_base = (void*)addr;
	remote.iov_len = size;
	uint64_t status64 = (uint64_t)process_vm_readv(pid, &local, 1, &remote, 1, 0);*/
	const char* memfilename = fmt::format("/proc/{}/mem", pid).c_str();
	FILE* memfile = fopen(memfilename, "rb+");
	if (memfile == nullptr) {
		printf("Error! Memory file '%s' was not found!\n", memfilename);
		return 0;
	}
	fseek(memfile, addr, SEEK_SET);
	uint64_t status64 = fread(val, 1, size, memfile);
	ptrace(PTRACE_DETACH, g_pid, 1, 0);
	fclose(memfile);
	return status64;
}

uint64_t ProcessManager::Write(uint64_t pid, uint64_t addr, void* val, uint64_t size)
{
	ptrace(PTRACE_ATTACH, pid, 0, 0);
	int status32 = 0;
	if (waitpid(pid, &status32, 0) == -1 || !WIFSTOPPED(status32)) {
		return status32;
	}
	const char* memfilename = fmt::format("/proc/{}/mem", pid).c_str();
	FILE* memfile = fopen(memfilename, "rb+");
	if (memfile == nullptr) {
		printf("Error! Memory file '%s' was not found!\n", memfilename);
		return 0;
	}
	fseek(memfile, addr, SEEK_SET);
	uint64_t status = fwrite(val, 1, size, memfile);
	ptrace(PTRACE_DETACH, g_pid, 1, 0);
	fclose(memfile);
	return status;
	/*struct iovec local {}, remote{};
	local.iov_base = &val;
	local.iov_len = size;
	remote.iov_base = (void*)addr;
	remote.iov_len = size;
	return (uint64_t)process_vm_writev(pid, &local, 1, &remote, 1, 0);*/
}

std::vector<int> ProcessManager::GetPIDs()
{
	std::vector<int> ret = std::vector<int>();
	fs::directory_iterator end_iterator;
	fs::directory_iterator iterator = fs::directory_iterator("/proc");
	while (iterator != end_iterator) {
		++iterator;
		if (fs::is_directory(*iterator) && atoi(iterator->path().relative_path().string().substr(2).c_str())) {
			ret.push_back(atoi(iterator->path().relative_path().string().substr(2).c_str()));
		}
	}
	return ret;
}

int ProcessManager::GetPIDByName(const char* name)
{
	//thx https://stackoverflow.com/questions/15686450/get-process-id-by-name
	char buf[512];
	/*char cmd[512];
	sprintf(cmd, "pidof -s %s", name);*/
	std::string awkcmd = "awk '{print $2}'";
	auto cmd = fmt::format("ps aux | grep {} -m 1 | {}", name, awkcmd);
	FILE* cmd_pipe = popen(cmd.c_str(), "r");
	fgets(buf, 512, cmd_pipe);
	printf("command '%s' returned '%s'\n", cmd.c_str(), buf);
	return strtoul(buf, NULL, 10);
}

void ProcessManager::GetProcessMaps()
{
	std::vector<RPC_PROC_MAP_INFO> ret = std::vector<RPC_PROC_MAP_INFO>();
	if (!g_pid) {
		printf("[map]: cannot parse the memory map of %d\n", g_pid);
		return;
	}
	procmaps_iterator* pmparser_it = pmparser_parse(g_pid);
	if (pmparser_it == nullptr) {
		printf("[map]: cannot parse the memory map of %d\n", g_pid);
		return;
	}
	procmaps_struct* maps_tmp = nullptr;
	uint64_t index = 0;
	while ((maps_tmp = pmparser_next(pmparser_it)) != nullptr) {
		RPC_PROC_MAP_INFO mapinfo{};
		strcpy(mapinfo.name, " ");
		strcpy(mapinfo.filename, fs::path(maps_tmp->pathname).filename().c_str());
		mapinfo.pid = g_pid;
		mapinfo.start_address = (uint64_t)maps_tmp->addr_start;
		mapinfo.end_address = (uint64_t)maps_tmp->addr_end;
		mapinfo.size = maps_tmp->length;
		mapinfo.index = index;
		mapinfo.is_executable = maps_tmp->is_x;
		mapinfo.is_readable = maps_tmp->is_r;
		mapinfo.is_writable = maps_tmp->is_w;
		ret.push_back(mapinfo);
		index++;
	}
	g_maps = ret;
	return;
}


#include <sys/socket.h>
#include <arpa/inet.h>
#include <netinet/in.h>
#include <netinet/tcp.h>

int g_fd = -1;

int RPC_SendData(int fd, void* data, int length) {
	int left = length;
	int offset = 0;
	int sent = 0;
	printf("Sending data with length %d\n", length);

	while (left > 0) {
		if (left > RPC_MAX_DATA_LEN) {
			sent = send(fd, data + offset, RPC_MAX_DATA_LEN, 0);
		} else {
			sent = send(fd, data + offset, left, 0);
		}
		if (!sent)
			return 0;
		offset += sent;
		left -= sent;
	}
	return offset;
}

int RPC_RecvData(int fd, void* data, int length, int force) {
	uint32_t left = length;
	uint32_t offset = 0;
	uint32_t recieved = 0;

	printf("Recieving data with length %d\n", length);

	while (left > 0) {
		if (left > RPC_MAX_DATA_LEN) {
			recieved = recv(fd, data + offset, RPC_MAX_DATA_LEN, 0);
		}
		else {
			recieved = recv(fd, data + offset, left, 0);
		}

		if (!recieved) {
			if (!force) {
				return offset;
			}
		}

		offset += recieved;
		left -= recieved;
	}
	return offset;
}

int RPC_SendStatus(int fd, uint32_t status) {
	printf("Sending status 0x%08X\n", status);
	if (RPC_SendData(fd, &status, sizeof(uint32_t))) {
		uint32_t response = 0;
		RPC_RecvData(fd, &response, sizeof(uint32_t), 1);
		return 0;
	} else {
		return 1;
	}
}

#include "ptrace.h"

int RPC_HandleAlloc(int fd, struct RPC_CMD_HDR_ALLOC_INFO* alloc_info) {
	/*uint64_t addr = (uint64_t)alloc_rwx_on_process(alloc_info->pid, alloc_info->size);
	if (addr == 0 || addr == -1) {
		RPC_SendStatus(fd, RPC_STATUS_ALLOCATION_ERROR);
		printf("alloc_rwx_on_process errored out with errorno %s\n", strerror(errno));
		return -1;
	}
	RPC_SendStatus(fd, 0);
	int r = RPC_SendData(fd, &addr, sizeof(uint64_t));
	if (!r) {
		r = -1;
		return r;
	}*/
	return 0;
}

int RPC_HandleRead(int fd, struct RPC_CMD_HDR_PROC_MEM_ACCESS* read_info) {
	uint8_t* data = nullptr;
	int r = 0;
	auto length = read_info->length;
	auto left = length;
	uint64_t offset = 0;
	uint8_t* test = (uint8_t*)malloc(1);
	printf("Reading %d bytes of memory from process %d at 0x%16X.\n", length, read_info->pid, read_info->address);
	uint64_t read_result = ProcessManager::Read(read_info->pid, read_info->address, test, 1);
	if (test == nullptr || read_result != 1) {
		r = 1;
		goto error;
	}
	while (left > 0) {
		auto read = left;
		if (left > RPC_MAX_DATA_LEN) {
			read = RPC_MAX_DATA_LEN;
		}
		data = (uint8_t*)malloc(read);
		read_result = ProcessManager::Read(read_info->pid, read_info->address + offset, data, read);
		if (data == nullptr || read_result != read) {
			if (read_result == -1)
				printf("read errored out! %s\n", strerror(errno));
			else {
				printf("unknown read error occured!\n");
			}

			goto error;
		}
		else {
			r = RPC_SendData(fd, data, read);
			if (!r) {
				r = 1;
				goto error;
			} else {

			}
		}
		left -= read;
		offset += read;
	}
	return r;
error:
	if (data) {
		free(data);
	}

	return r;
}

int RPC_HandleWrite(int fd, struct RPC_CMD_HDR_PROC_MEM_ACCESS* write_info) {
	uint8_t* data = nullptr;
	int r = 0;
	auto length = write_info->length;
	if (length > RPC_MAX_DATA_LEN) {
		r = 1;
		goto end;
	}
	data = (uint8_t*)malloc(length);
	RPC_RecvData(fd, data, length, 1);
	r = ProcessManager::Write(write_info->pid, write_info->address, data, length);
	if (r == length)
		r = 1;
end:
	if (data) {
		free(data);
	}
	return r;
}

void RPC_HandleInfo(int fd, struct RPC_CMD_HDR_PROC_INFO* info) {
	char* name;
	int namesize = 0;
	while (info->name[namesize] != 0x00) {
		namesize++;
	}
	name = (char*)malloc(namesize);
	strcpy(name, info->name);
	int pid = ProcessManager::GetPIDByName(name);
	if (pid != 0) {
		if (g_pid) {
			ProcessManager::UnbindProcess();
		}
		ProcessManager::BindProcess(pid);
	}
	if (g_pid == 0) {
		RPC_SendStatus(fd, RPC_STATUS_PROC_INVALID);
		return;
	}
	info->pid = g_pid;

	RPC_SendStatus(fd, 0);

	ProcessManager::GetProcessMaps();

	info->num_maps = g_maps.size();
	printf("\"%s\" has %d maps\n", info->name, info->num_maps);
	if (!RPC_SendData(fd, (uint8_t*)info, sizeof(RPC_CMD_HDR_PROC_INFO))) {
		RPC_SendStatus(fd, RPC_STATUS_GETINFO_ERROR);
	}
	else {
		RPC_SendStatus(fd, 0);
	}
}

void RPC_HandleMap(int fd, struct RPC_CMD_HDR_REQUESTED_MAP* request) {
	auto index = request->index;
	RPC_PROC_MAP_INFO* info = (RPC_PROC_MAP_INFO*)malloc(sizeof(RPC_PROC_MAP_INFO));
	*info = g_maps[index];
	if (!RPC_SendData(fd, (uint8_t*)info, sizeof(RPC_PROC_MAP_INFO))) {
	}
	else {
	}
}

int RPC_HandleSignalHandler(int fd) {
	std::ofstream file;
	file.open("~/.tmp/cicero/cmd");
	file << "InstallHandlers";
	file.close();
	return 0;
}

int HandleRPCCommand(int fd, struct RPC_PACKET* packet) {
	uint32_t cmd = (packet->cmd);
	printf("Processing rpc command 0x%08X...\n", cmd);
	if (!RPC_VALID_CMD(cmd)) {
		return 1;
	}
	switch (cmd) {
	case RPC_CMD_READPROC: {
		RPC_HandleRead(fd, (struct RPC_CMD_HDR_PROC_MEM_ACCESS*)packet->data);
		break;
	}
	case RPC_CMD_WRITEPROC: {
		RPC_HandleWrite(fd, (struct RPC_CMD_HDR_PROC_MEM_ACCESS*)packet->data);
		break;
	}
	case RPC_CMD_PROCINFO: {
		RPC_HandleInfo(fd, (struct RPC_CMD_HDR_PROC_INFO*)packet->data);
		break;
	}
	case RPC_CMD_MAPINFO: {
		RPC_HandleMap(fd, (struct RPC_CMD_HDR_REQUESTED_MAP*)packet->data);
		break;
	}
	case RPC_CMD_ALLOCATE: {
		RPC_HandleAlloc(fd, (struct RPC_CMD_HDR_ALLOC_INFO*)packet->data);
		break;
	}
	case RPC_CMD_SIGHANDLE: {
		RPC_HandleSignalHandler(fd);
		break;
	}
	case RPC_CMD_DISCONNECT: {
		Connector::Disconnect();
		break;
	}
	default:
		break;
	}
	return 0;
}

int RPCHandler(int vfd) {
	int fd = vfd;
	struct RPC_PACKET packet;
	uint8_t* data = NULL;
	uint64_t length = 0;
	int r = 0;
	while (1) {
		//sleep(1);
		r = RPC_RecvData(fd, (uint8_t*)&packet, sizeof(RPC_PACKET) - sizeof(uint64_t), 0);

		if (!r) {
			continue;
		}

		if ((packet.magic) != RPC_PACKET_MAGIC) {
			printf("ERROR: Packet magic was 0x%08X when I expected 0x%08X!\n", (packet.magic),
				RPC_PACKET_MAGIC);
			continue;
		}

		if (r != sizeof(RPC_PACKET) - sizeof(uint64_t)) {
			continue;
		}

		length = (packet.len);

		printf("Recieved a packet from fd %d with accompanying data of length %d\n", fd, length);

		if (length) {
			// check
			if (length > RPC_MAX_DATA_LEN) {
				continue;
			}

			// allocate data
			data = (uint8_t*)malloc(length);
			if (!data) {
				goto error;
			}

			r = RPC_RecvData(fd, data, length, 1);
			if (!r) {
				goto error;
			}

			// set data;
			packet.data = data;
		}
		else {
			packet.data = NULL;
		}

		r = HandleRPCCommand(fd, &packet);

		if (data) {
			free(data);
			data = NULL;
		}

		if (r) {
			goto error;
		}
	}
	return 0;
error:
	return 1;
}

std::string GetIPv4Address() {
	char buf[512];
	const char* cmd = "hostname -I | awk '{print $1}'";
	FILE* cmd_pipe = popen(cmd, "r");
	fgets(buf, 512, cmd_pipe);
	return std::string(buf);
}

int StartRPC() {
	struct sockaddr_in servaddr {};;
	int fd = -1;
	int newfd = -1;
	int r = 0;
	fd = socket(AF_INET, SOCK_STREAM, 0);
	int optval = -1;
	if (fd < 0) {
		printf("Error: Could not make socket!\n");
		close(fd);
		return 1;
	}

	//// set it to not generate SIGPIPE
	//setsockopt(fd, SOL_SOCKET, SO_NO, (void*)&optval, sizeof(int));

	//// non blocking socket
	//optval = 1;
	//setsockopt(fd, SOL_SOCKET, SOCK_NONBLOCK, (void*)&optval, sizeof(int));

	// no delay to merge packets
	optval = 1;
	setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, (void*)&optval, sizeof(int));

	memset(&servaddr, 0, sizeof(servaddr));
	servaddr.sin_family = AF_INET;
	servaddr.sin_addr.s_addr = INADDR_ANY;
	servaddr.sin_port = htons(RPC_PORT);

	if ((bind(fd, (struct sockaddr*)&servaddr, sizeof(servaddr))) == -1) {
		printf("Error binding!\n");
		close(fd);
		return 1;
	}
	if ((r = listen(fd, 16))) {
		printf("Error listening!\n");
		close(fd);
		return 1;
	}
	auto myIP = GetIPv4Address();
	printf("Started rpc server at %s!\n", myIP.c_str());

	while (1) {
		newfd = accept(fd, NULL, NULL);
		if (newfd > -1) {
			g_fd = newfd;
			printf("Connected to fd %d\n", g_fd);
			//// set it to not generate SIGPIPE
			//int optval = 1;
			//setsockopt(newfd, SOL_SOCKET, SO_NOSIGPIPE, (void*)&optval, sizeof(int));

			//// non blocking socket
			//optval = 1;
			//setsockopt(newfd, SOL_SOCKET, 0x1200, (void*)&optval, sizeof(int));

			// no delay to merge packets
			optval = 1;
			setsockopt(newfd, IPPROTO_TCP, TCP_NODELAY, (void*)&optval, sizeof(int));

			if (RPCHandler(newfd)) {
				continue;
			}
		}
	}
	return 0;
}

void Connector::Init() {
	printf("Starting rpc server...\n");
	int r = StartRPC();
	if (r == 1) {
		exit(-1);
	}
}

void Connector::Disconnect() {
	close(g_fd);
}