#pragma once

#include <string>
#include <stdint.h>
#include <vector>
#include <sys/types.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <stdbool.h>
#include <unistd.h>
#include <proc_maps_parser/pmparser.h>

#define RPC_PACKET_MAGIC 0xABADBEEF
#define RPC_PORT 0xBEEF

#define RPC_CMD_READPROC   0xBB000001
#define RPC_CMD_WRITEPROC  0xBB000002
#define RPC_CMD_PROCINFO   0xBB000003
#define RPC_CMD_MAPINFO    0xBB000004
#define RPC_CMD_ALLOCATE   0xBB000005
#define RPC_CMD_FREEMEM    0xBB000006
#define RPC_CMD_SIGHANDLE  0xBB000007
#define RPC_CMD_DISCONNECT 0xBB0000FF

#define RPC_VALID_CMD(cmd)   (((cmd & 0xFF000000) >> 24) == 0xBB)
#define RPC_IS_ERROR(status) (((status & 0xFF000000) >> 24) == 0x80)

#define RPC_STATUS_SUCCESS    0x00000000
#define RPC_STATUS_READ_ERROR 0x80001000
#define RPC_STATUS_WRITE_ERROR 0x80001001
#define RPC_STATUS_PROC_INVALID 0x80001002
#define RPC_STATUS_GETINFO_ERROR 0x80001003
#define RPC_STATUS_ALLOCATION_ERROR 0x80001004
#define RPC_STATUS_FREEMEM_ERROR 0x80001005
#define RPC_STATUS_TOO_MUCH_DATA 0xFF000001

#define RPC_MAX_DATA_LEN 8192

struct RPC_PACKET {
	uint32_t magic;
	uint32_t cmd;
	uint64_t len;
	uint8_t* data;
};

struct RPC_STATUS {
	uint32_t magic;
	uint32_t status;
};

struct RPC_CMD_HDR_PROC_MEM_ACCESS {
	uint64_t pid;
	uint64_t address;
	uint64_t length;
};

struct RPC_PROC_MAP_INFO {
	char name[32];
	char filename[255];
	char pad1;
	uint64_t pid;
	uint64_t start_address;
	uint64_t end_address;
	uint64_t size;
	short is_readable;
	short is_writable;
	short is_executable;
	char pad2;
	uint64_t index;
};

struct RPC_CMD_HDR_REQUESTED_MAP {
	uint64_t index;
};

struct RPC_CMD_HDR_PROC_INFO {
	char name[64];
	uint64_t pid;
	uint64_t num_maps;
};

struct RPC_CMD_HDR_ALLOC_INFO {
	uint64_t pid;
	uint64_t size;
	short is_readable;
	short is_writable;
	short is_executable;
	short pad;
};

struct RPC_CMD_HDR_FREEMEM_INFO {
	uint64_t pid;
	uint64_t addr;
};

class Connector {
public:
	static void Init();
	static void Disconnect();
};

class ProcessManager
{
public:
	static void BindProcess(int pid);
	static void UnbindProcess();
	static uint64_t Read(uint64_t pid, uint64_t addr,  void* val, uint64_t size);
	static uint64_t Write(uint64_t pid, uint64_t addr, void* val, uint64_t size);
	static std::vector<int> GetPIDs();
	static int GetPIDByName(const char* name);
	static void GetProcessMaps();
};