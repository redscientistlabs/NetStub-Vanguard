/*
 *  Connector.h
 *  netstub-powermac-client
 *
 *  Created by Christian on 2/26/22.
 *
 */

#pragma once
#include <string>
#include <stdint.h>
#include <sys/types.h>
#include <mach/vm_map.h>

#define RPC_PACKET_MAGIC 0xABADBEEF
#define RPC_PORT 7828

#define RPC_CMD_READPROC   0xBB000001
#define RPC_CMD_WRITEPROC  0xBB000002
#define RPC_CMD_PROCINFO   0xBB000003
#define RPC_CMD_MAPINFO    0xBB000004
#define RPC_CMD_DISCONNECT 0xBB0000FF

#define RPC_VALID_CMD(cmd) (((cmd & 0xFF000000) >> 24) == 0xBB)

#define RPC_STATUS_SUCCESS         0x00
#define RPC_STATUS_READ_ERROR      0xF0
#define RPC_STATUS_WRITE_ERROR     0xF1
#define RPC_STATUS_PROC_INVALID    0xF2
#define RPC_STATUS_GETINFO_ERROR   0xF3
#define RPC_STATUS_TOO_MUCH_DATA   0xE0

#define RPC_MAX_DATA_LEN 8192

#include "byteswap.h"


struct RPC_PACKET {
	uint32_t magic;
	uint32_t cmd;
	uint32_t len;
	uint8_t* data;
};

struct RPC_STATUS {
	uint32_t magic;
	uint8_t status;
};

struct RPC_CMD_HDR_PROC_MEM_ACCESS {
	uint32_t pid;
	uint32_t address;
	uint32_t length;
};

struct RPC_PROC_MAP_INFO {
	char name[32];
	char filename[255];
	char pad1;
	uint32_t pid;
	uint32_t start_address;
	uint32_t end_address;
	uint32_t size;
	char prot[7];
	char pad2;
	uint32_t index;
};

struct RPC_CMD_HDR_REQUESTED_MAP {
	uint32_t index;
};

struct RPC_CMD_HDR_PROC_INFO {
	char name[64];
	uint32_t pid;
	uint32_t num_maps;
};



class Connector {
public:
	static void Init();
	static void Disconnect();
};