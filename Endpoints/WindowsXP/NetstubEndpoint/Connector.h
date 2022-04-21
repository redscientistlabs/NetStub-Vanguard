#pragma once

#define WIN32_LEAN_AND_MEAN

#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iphlpapi.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string>

#pragma comment(lib, "IPHLPAPI.lib")

#pragma comment(lib, "ws2_32.lib")

#define RPC_PACKET_MAGIC 0xABADBEEF
#define RPC_PORT 0xBEEF
#define RPC_PORT_STRING "48879"

#define RPC_CMD_READPROC   0xBB000001
#define RPC_CMD_WRITEPROC  0xBB000002
#define RPC_CMD_PROCINFO   0xBB000003
#define RPC_CMD_MAPINFO    0xBB000004
#define RPC_CMD_FLUSHINSN  0xBB000005
#define RPC_CMD_SETPROTECT 0xBB000006
#define RPC_CMD_DISCONNECT 0xBB0000FF

#define RPC_VALID_CMD(cmd)   (((cmd & 0xFF000000) >> 24) == 0xBB)
#define RPC_IS_ERROR(status) (((status & 0xFF000000) >> 24) == 0x80)

#define RPC_STATUS_SUCCESS    0x00000000
#define RPC_STATUS_READ_ERROR 0x80001000
#define RPC_STATUS_WRITE_ERROR 0x80001001
#define RPC_STATUS_PROC_INVALID 0x80001002
#define RPC_STATUS_GETINFO_ERROR 0x80001003
#define RPC_STATUS_TOO_MUCH_DATA 0xFF000001

#define RPC_MAX_DATA_LEN 8192

struct RPC_PACKET {
	uint32_t magic;
	uint32_t cmd;
	uint32_t len;
	uint8_t* data;
};

struct RPC_CMD_HDR_PROC_MEM_ACCESS {
	HANDLE handle;
	uint32_t address;
	uint32_t length;
};

struct RPC_CMD_HDR_MEM_PROTECTION {
	HANDLE handle;
	uint32_t address;
	uint32_t length;
	uint32_t protection;
};

struct RPC_PROC_MAP_INFO {
	char name[32];
	char filename[255];
	char pad1;
	HANDLE handle;
	uint32_t start_address;
	uint32_t end_address;
	uint32_t size;
	DWORD protect;
	uint32_t index;
};

struct RPC_CMD_HDR_REQUESTED_MAP {
	uint32_t index;
};

struct RPC_CMD_HDR_PROC_INFO {
	char name[64];
	HANDLE handle;
	uint32_t num_maps;
};


class Connector {
public:
	static void Init();
	static inline void Disconnect() {}
};

class ProcessManager
{
public:
	static void BindProcess(HANDLE handle);
	static void UnbindProcess();
	static uint32_t Read(HANDLE handle, uint32_t addr, void* val, uint32_t size);
	static uint32_t Write(HANDLE handle, uint32_t addr, void* val, uint32_t size);
	static HANDLE GetProcessByName(const char* name);
	static void GetProcessMaps();
};