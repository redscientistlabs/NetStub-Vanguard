/*
 *  Connector.cpp
 *  netstub-powermac-client
 *
 *  Created by Christian on 2/26/22.
 *
 */
 
 // code adapted from github.com/jogolden/jkpatch/blob/master/kpayload/source/rpc.c

#include <string>
#include <vector>
#include <sstream>
#include <iostream>

#include <unistd.h>
#include <stdlib.h>
#include <pthread.h>
#include <regex.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <netinet/in.h>
#include <netinet/tcp.h>

#include <boost/regex.hpp>
#include <boost/filesystem/operations.hpp>
#include <boost/filesystem/fstream.hpp>
#include <boost/format.hpp>
#include <boost/algorithm/string.hpp>

#include "Connector.h"
#include "xnumem.h"
std::vector<RPC_PROC_MAP_INFO> g_maps;
uint32_t ReverseEndian(uint32_t i) {
	return bswap_32(i);
}

uint64_t ReverseEndian(uint64_t i) {
	return bswap_64(i);
};

namespace fs = boost::filesystem;

int g_fd = -1;

int g_pid = 0;

static const char* regex_string = "(__[^ ]*) *([abcdef0123456789]*)-([abcdef0123456789]*)[ \\[K1234567890\\]]*([-/rwx]*) .{6}  (.*)";

std::vector<std::string> split_string(const std::string& str, const std::string& delim) {
	std::vector<std::string> strings;
	
	boost::split(strings, str, boost::is_any_of(delim));
	
	return strings;
}

int GetNumberOfLinesInFile (FILE* file) {
	fseek(file, 0L, SEEK_SET);
	char c;
	int count = 0;
	for (c = getc(file); c != EOF; c = getc(file)) {
		if (c == '\n')
			count = count + 1;
	}
	return count;
}

uint32_t IntegerFromHexString(std::string str) {
	uint32_t x;
	std::stringstream ss;
	ss << std::hex << str;
	ss >> x;
	return x;
}

void load_string_file(const fs::path& p, std::string& str) {
	fs::ifstream file;
	file.open(p, std::ios_base::binary);
	size_t sz = static_cast<size_t>(fs::file_size(p));
	str.resize(sz, '\0');
	file.read(&str[0], sz);
}

std::vector<RPC_PROC_MAP_INFO> GetProcessMaps(int pid) {
	
	/* char* cmd;
		sprintf(cmd, "vmmap -w -interleaved %d | grep __ > vmmap_output.txt", pid); */
	std::string cmd = (boost::format("vmmap -w -interleaved %1% | grep __ > vmmap_output.txt") % pid).str();
	system(cmd.c_str());
	std::string buf;
	load_string_file("vmmap_output.txt", buf);
	
	
	
	boost::regex e(regex_string);
	boost::smatch what;
	
	std::vector<std::string> strings = split_string(buf, "\n");
	
	std::vector<RPC_PROC_MAP_INFO> infos;
	size_t count = 0;
	for (int i = 0; i < strings.size(); i++) {
		std::string& string = strings[i];
		RPC_PROC_MAP_INFO info;
		info.pid = pid;
		if (boost::regex_match(string, what, e, boost::match_extra)) {
			printf("%s\n", string.c_str());
			strncpy(info.name, what[1].str().c_str(), 32);
			info.start_address = ((uint32_t)IntegerFromHexString(what[2].str()));
			info.end_address = ((uint32_t)IntegerFromHexString(what[3].str()));
			info.size = info.end_address - info.start_address;
			strncpy(info.prot, what[4].str().c_str(), 7);
			strncpy(info.filename, what[5].str().c_str(), 255);
			info.pad1 = 0;
			info.pad2 = 0;
			info.index = count;
			count++;
			infos.push_back(info);
		}
	}
	
	return infos;
}

int RPC_SendData(int fd, uint8_t* data, int length) {
	uint32_t left = length;
	uint32_t offset = 0;
	uint32_t sent = 0;
	
	printf("Sending data with length %d\n", length);
	
	while (left > 0) {
		if (left > RPC_MAX_DATA_LEN) {
			sent = send(fd, data + offset, RPC_MAX_DATA_LEN, 0);
		} else {
			sent = send(fd, data + offset, left, 0);
		}
		
		if (!sent) {
			return 0;
		}
		
		offset += sent;
		left -= sent;
	}
	return offset;
}

int RPC_RecvData(int fd, uint8_t* data, int length, int force) {
	uint32_t left = length;
	uint32_t offset = 0;
	uint32_t recieved = 0;
	
	printf("Recieving data with length %d\n", length);
	
	while (left > 0) {
		if (left > RPC_MAX_DATA_LEN) {
			recieved = recv(fd, data + offset, RPC_MAX_DATA_LEN, 0);
		} else {
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

int RPC_SendStatus(int fd, uint8_t status) {
	uint8_t d = status;
	printf("Sending status 0x%02X\n", status);
	if (RPC_SendData(fd, &d, sizeof(uint8_t))) {
		RPC_RecvData(fd, &d, sizeof(uint8_t), 1);
		return 0;
	} else {
		return 1;
	}
}

int RPC_HandleRead(int fd, struct RPC_CMD_HDR_PROC_MEM_ACCESS* pread) {
	uint8_t* data = NULL;
	size_t n = 1;
	int r = 0;
	uint32_t length = ReverseEndian(pread->length);
	uint32_t left = length;
	uint32_t offset = 0;
	
	uint8_t* test = NULL;
	printf("Reading %d bytes of memory for process %d at 0x%08X.\n", length, ReverseEndian(pread->pid), ReverseEndian(pread->address));
	test = xnumem::xnu_read(ReverseEndian(pread->pid), (ReverseEndian(pread->address)), &n);
	
	if (test == NULL) {
		//RPC_SendStatus(fd, RPC_STATUS_READ_ERROR);
		r = 1;
		goto error;
	}
	
	//RPC_SendStatus(fd, RPC_STATUS_SUCCESS);
	
	if (length == 4) {
		printf("Current address: %08Xh.\n", ReverseEndian(pread->address) + offset);
		data = xnumem::xnu_read(ReverseEndian(pread->pid), ((ReverseEndian(pread->address))), (size_t*)&length);
		if (data == NULL) {
			goto error;
		} else {
			r = RPC_SendData(fd, data, length);
			if (!r) {
				r = 1;
				goto error;
			}
		}
		return r;
	}
	
	while (left > 0) {
		size_t read = left;
		if (left > RPC_MAX_DATA_LEN) {
			read = RPC_MAX_DATA_LEN;
		}
		printf("Current address: %08Xh.\n", ReverseEndian(pread->address) + offset);
		data = xnumem::xnu_read(ReverseEndian(pread->pid), ((ReverseEndian(pread->address) + offset)), &read);
		
		if (data == NULL) {
			goto error;
		} else {
			r = RPC_SendData(fd, data, read);
			if (!r) {
				r = 1;
				goto error;
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

int RPC_HandleWrite(int fd, struct RPC_CMD_HDR_PROC_MEM_ACCESS* pwrite) {
	uint8_t* data = NULL;
	int r = 0;
	size_t length = ReverseEndian(pwrite->length);
	
	if (length > RPC_MAX_DATA_LEN) {
		//RPC_SendStatus(fd, RPC_STATUS_TOO_MUCH_DATA);
		r = 1;
		goto error;
	}
	
	//RPC_SendStatus(fd, RPC_STATUS_SUCCESS);
	data = (uint8_t*)malloc(length);
	RPC_RecvData(fd, data, length, 1);
	
	r = xnumem::xnu_write(ReverseEndian(pwrite->pid), (ReverseEndian(pwrite->address)), data, length);
	
	if (r) {
		//RPC_SendStatus(fd, RPC_STATUS_WRITE_ERROR);
		printf("Error writing to %08Xh", ReverseEndian(pwrite->address));
		r = 1;
		goto error;
	} else {
		//RPC_SendStatus(fd, RPC_STATUS_SUCCESS);
	}
	return r;
error:
	if (data) {
		free(data);
	}
	return r;
}

void RPC_HandleInfo(int fd, struct RPC_CMD_HDR_PROC_INFO* info) {
	g_pid = xnumem::procpid(info->name);
	if (g_pid == 0) {
		RPC_SendStatus(fd, RPC_STATUS_PROC_INVALID);
		return;
	}
	info->pid = g_pid;
	
	RPC_SendStatus(fd, 0x00);
	
	g_maps = GetProcessMaps(g_pid);
	
	info->num_maps = g_maps.size();
	printf("\"%s\" has %d maps\n", info->name, info->num_maps);
	if (!RPC_SendData(fd, (uint8_t*)info, 72)) {
		RPC_SendStatus(fd, 0xF3);
	} else {
		RPC_SendStatus(fd, 0x00);
	}
}

void RPC_HandleMap(int fd, struct RPC_CMD_HDR_REQUESTED_MAP* request) {
	uint32_t index = request->index;
	RPC_PROC_MAP_INFO* info = (RPC_PROC_MAP_INFO*)malloc(sizeof(RPC_PROC_MAP_INFO));
	*info = g_maps[index];
	
	if (!RPC_SendData(fd, (uint8_t*)info, sizeof(RPC_PROC_MAP_INFO))) {
	} else {
	}
}

int HandleRPCCommand(int fd, struct RPC_PACKET* packet) {
	uint32_t cmd = ReverseEndian(packet->cmd);
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
	case RPC_CMD_DISCONNECT: {
		return 1;
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
	uint32_t length = 0;
	int r = 0;
	while (1) {
		//sleep(1);
		r = RPC_RecvData(fd, (uint8_t*)&packet, 12, 0);
		
		if (!r) {
			continue;
		}
		
		if (ReverseEndian(packet.magic) != RPC_PACKET_MAGIC) {
			printf("ERROR: Packet magic was 0x%08X when I expected 0x%08X!\n", ReverseEndian(packet.magic),
					RPC_PACKET_MAGIC);
			continue;
		}
		
		if (r != 12) {
			continue;
		}
		
		length = ReverseEndian(packet.len);
		
		printf("Recieved a packet from fd %d with accompanying data of length %d\n",fd,length);
		
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
		} else {
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

int StartRPC() {
	struct sockaddr_in servaddr;
	int fd = -1;
	int newfd = -1;
	int r = 0;
	fd = socket(AF_INET, SOCK_STREAM, 0);
	int optval = -1;
	if (fd < 0) {
		printf("Error: Could not make socket!\n");
		goto error;
	}
	
	// set it to not generate SIGPIPE
	setsockopt(fd, SOL_SOCKET, SO_NOSIGPIPE, (void*)&optval, sizeof(int));
	
	// non blocking socket
	optval = 1;
	setsockopt(fd, SOL_SOCKET, 0x1200, (void*)&optval, sizeof(int));
	
	// no delay to merge packets
	optval = 1;
	setsockopt(fd, IPPROTO_TCP, TCP_NODELAY, (void*)&optval, sizeof(int));
	
	memset(&servaddr, NULL, sizeof(servaddr));
	servaddr.sin_family = AF_INET;
	servaddr.sin_addr.s_addr = INADDR_ANY;
	servaddr.sin_port = htons(RPC_PORT);
	
	if ((bind(fd, (struct sockaddr*)&servaddr, sizeof(servaddr))) == -1) {
		printf("Error binding!\n");
		goto error;
	}
	if ((r = listen(fd, 16))) {
		printf("Error listening!\n");
		goto error;
	}
	printf("Started rpc server!\n");
	while (1) {
		newfd = accept(fd, NULL, NULL);
		if (newfd > -1) {
			g_fd = newfd;
			printf ("Connected to fd %d\n", g_fd);
			// set it to not generate SIGPIPE
			int optval = 1;
			setsockopt(newfd, SOL_SOCKET, SO_NOSIGPIPE, (void*)&optval, sizeof(int));
	
			// non blocking socket
			optval = 1;
			setsockopt(newfd, SOL_SOCKET, 0x1200, (void*)&optval, sizeof(int));
	
			// no delay to merge packets
			optval = 1;
			setsockopt(newfd, IPPROTO_TCP, TCP_NODELAY, (void*)&optval, sizeof(int));
			
			if (RPCHandler(newfd)) {
				continue;
			}
		}
	}
	return 0;
error:
	close(fd);
	return 1;
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