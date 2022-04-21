#include "Connector.h"

HANDLE g_process;

//Returns the last Win32 error, in string format. Returns an empty string if there is no error. (https://stackoverflow.com/questions/1387064/how-to-get-the-error-message-from-the-error-code-returned-by-getlasterror)
std::string GetErrorString()
{
	//Get the error message ID, if any.
	DWORD errorMessageID = ::GetLastError();
	if (errorMessageID == 0) {
		return std::string(); //No error message has been recorded
	}

	LPSTR messageBuffer = nullptr;

	//Ask Win32 to give us the string version of that message ID.
	//The parameters we pass in, tell Win32 to create the buffer that holds the message for us (because we don't yet know how long the message string will be).
	size_t size = FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL, errorMessageID, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPSTR)&messageBuffer, 0, NULL);

	//Copy the error message into a std::string.
	std::string message(messageBuffer, size);

	//Free the Win32's string's buffer.
	LocalFree(messageBuffer);

	return message;
}
void ProcessManager::BindProcess(HANDLE handle)
{
	g_process = handle;
	//ptrace(PTRACE_ATTACH, pid, 0, 0);
}

void ProcessManager::UnbindProcess()
{
	//ptrace(PTRACE_DETACH, g_process, 0, 0);
	g_process = 0;
}

uint32_t ProcessManager::Read(HANDLE handle, uint32_t addr, void* val, uint32_t size)
{
	SIZE_T read;
	int result = ReadProcessMemory(handle, (LPVOID)addr, val, size, &read);

	if (result == 0) {
		printf("ReadProcessMemory failed with %s", GetErrorString().c_str());
		return 0;
	}

	return read;

}

uint32_t ProcessManager::Write(HANDLE handle, uint32_t addr, void* val, uint32_t size)
{
	SIZE_T written;
	int result = WriteProcessMemory(handle, (LPVOID)addr, val, size, &written);

	if (result == 0) {
		printf("WriteProcessMemory failed with %s", GetErrorString().c_str());
		return 0;
	}
	return written;
}

// https://gist.github.com/baiyanhuang/902894
#include <psapi.h>
#include <atlstr.h>
#include <string>
#include <iostream>
#include <vector>

#pragma comment (lib, "Psapi.lib")


HANDLE GetProcessByTCHAR(const TCHAR* szProcessName)
{
	if (szProcessName == NULL) return NULL;
	CString strProcessName = szProcessName;

	DWORD aProcesses[1024], cbNeeded, cProcesses;
	if (!EnumProcesses(aProcesses, sizeof(aProcesses), &cbNeeded))
		return NULL;

	// Calculate how many process identifiers were returned.
	cProcesses = cbNeeded / sizeof(DWORD);

	// Print the name and process identifier for each process.
	for (unsigned int i = 0; i < cProcesses; i++)
	{
		DWORD dwProcessID = aProcesses[i];
		// Get a handle to the process.
		HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, FALSE, dwProcessID);

		// Get the process name.
		TCHAR szEachProcessName[MAX_PATH];
		if (NULL != hProcess)
		{
			HMODULE hMod;
			DWORD cbNeeded;

			if (EnumProcessModules(hProcess, &hMod, sizeof(hMod), &cbNeeded))
			{
				GetModuleBaseName(hProcess, hMod, szEachProcessName, sizeof(szEachProcessName) / sizeof(TCHAR));
			}
		}

		if (strProcessName.CompareNoCase(szEachProcessName) == 0)
			return hProcess;

		CloseHandle(hProcess);
	}

	return NULL;
}

// https://stackoverflow.com/questions/16342976/how-to-convert-char-to-tchar

HANDLE ProcessManager::GetProcessByName(const char* name)
{
	USES_CONVERSION;
	TCHAR szName[128];
	_tcscpy(szName, A2T(name));
	return GetProcessByTCHAR(szName);
}

std::vector<RPC_PROC_MAP_INFO> g_maps;

void ProcessManager::GetProcessMaps()
{
	printf("Getting process maps...\n");
	g_maps = std::vector<RPC_PROC_MAP_INFO>();
	if (!g_process) {
		printf("[map]: cannot parse the memory map of %d\n", g_process);
		return;
	}
	printf("Getting system info...\n");
	LPSYSTEM_INFO system_info = (LPSYSTEM_INFO)malloc(sizeof(SYSTEM_INFO));
	GetSystemInfo(system_info);
	auto minaddr = (uint32_t)system_info->lpMinimumApplicationAddress;
	auto maxaddr = (uint32_t)system_info->lpMaximumApplicationAddress;
	auto addr = minaddr;

	int index = 0;
	while (addr < maxaddr) {
		PMEMORY_BASIC_INFORMATION lpBuffer = (PMEMORY_BASIC_INFORMATION)malloc(sizeof(MEMORY_BASIC_INFORMATION));
		//printf("Querying address 0x%08X...\n", addr);
		if (VirtualQueryEx(g_process, (LPCVOID)addr, lpBuffer, sizeof(MEMORY_BASIC_INFORMATION)) == 0) {
			break;
		}
		if (lpBuffer->State != MEM_COMMIT ||
			lpBuffer->Protect == PAGE_NOACCESS) {
			addr += 0x1000;
			continue;
		}

		RPC_PROC_MAP_INFO map{};
		strcpy(map.name, " ");
		printf("Getting filename from region 0x%08X...\n", addr);
		GetMappedFileNameA(g_process, (LPVOID)addr, map.filename, 255);
		map.pad1 = 0;
		map.handle = g_process;
		map.start_address = (uint32_t)lpBuffer->BaseAddress;
		map.size = (uint32_t)lpBuffer->RegionSize;
		map.end_address = map.start_address + map.size;
		map.protect = lpBuffer->Protect;
		map.index = index++;
		g_maps.push_back(map);
		addr += map.size;
	}
}

void FlushProcessCodeCache(HANDLE handle, uint32_t baseAddress, uint32_t size) {
	FlushInstructionCache(handle, (LPCVOID)baseAddress, size);
}

int RPC_SendData(SOCKET sock, void* data, int length) {
	int left = length;
	int offset = 0;
	int sent = 0;
	printf("Sending data with length %d\n", length);

	while (left > 0) {
		if (left > RPC_MAX_DATA_LEN) {
			sent = send(sock, (const char*)data + offset, RPC_MAX_DATA_LEN, 0);
		}
		else {
			sent = send(sock, (const char*)data + offset, left, 0);
		}
		if (!sent)
			return 0;
		offset += sent;
		left -= sent;
	}
	return offset;
}

int RPC_RecvData(SOCKET sock, void* data, int length, int force) {
	uint32_t left = length;
	uint32_t offset = 0;
	uint32_t recieved = 0;

	printf("Recieving data with length %d\n", length);

	while (left > 0) {
		if (left > RPC_MAX_DATA_LEN) {
			recieved = recv(sock, (char*)data, RPC_MAX_DATA_LEN, 0);
		}
		else {
			recieved = recv(sock, (char*)data + offset, left, 0);
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
	}
	else {
		return 1;
	}
}

int RPC_HandleRead(int fd, struct RPC_CMD_HDR_PROC_MEM_ACCESS* read_info) {
	uint8_t* data = nullptr;
	int r = 0;
	auto length = read_info->length;
	auto left = length;
	uint32_t offset = 0;
	uint8_t* test = (uint8_t*)malloc(1);
	printf("Reading %d bytes of memory from process %d at 0x%16X.\n", length, read_info->handle, read_info->address);
	uint32_t read_result = ProcessManager::Read(read_info->handle, read_info->address, test, 1);
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
		read_result = ProcessManager::Read(read_info->handle, read_info->address + offset, data, read);
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
			}
			else {

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
	r = ProcessManager::Write(write_info->handle, write_info->address, data, length);
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
	HANDLE pid = ProcessManager::GetProcessByName(name);
	if (pid != 0) {
		if (g_process) {
			ProcessManager::UnbindProcess();
		}
		ProcessManager::BindProcess(pid);
	}
	if (g_process == 0) {
		RPC_SendStatus(fd, RPC_STATUS_PROC_INVALID);
		return;
	}
	info->handle = g_process;

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
	case RPC_CMD_FLUSHINSN: {
		auto data = (struct RPC_CMD_HDR_PROC_MEM_ACCESS*)packet->data;
		FlushProcessCodeCache(data->handle, data->address, data->length);
		break;
	}
	case RPC_CMD_SETPROTECT: {
		auto data = (struct RPC_CMD_HDR_MEM_PROTECTION*)packet->data;
		DWORD mp{};
		if (!VirtualProtectEx(data->handle, (LPVOID)data->address, data->length, data->protection, &mp))
			printf("VirtualProtectEx failed with %s", GetErrorString().c_str());
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
	uint32_t length = 0;
	int r = 0;
	while (1) {
		//sleep(1);
		r = RPC_RecvData(fd, (uint8_t*)&packet, sizeof(RPC_PACKET) - sizeof(uint32_t), 0);

		if (!r) {
			continue;
		}

		if ((packet.magic) != RPC_PACKET_MAGIC) {
			printf("ERROR: Packet magic was 0x%08X when I expected 0x%08X!\n", (packet.magic),
				RPC_PACKET_MAGIC);
			continue;
		}

		if (r != sizeof(RPC_PACKET) - sizeof(uint32_t)) {
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

void Connector::Init() {
	WSADATA wsaData;
	int iResult;

	SOCKET ListenSocket = INVALID_SOCKET;
	SOCKET ClientSocket = INVALID_SOCKET;

	struct addrinfo* result = nullptr;
	struct addrinfo hints;

	iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0) {
		printf("WSAStartup failed with error: %d\n", iResult);
		return;
	}

	ZeroMemory(&hints, sizeof(hints));

	hints.ai_family = AF_INET;
	hints.ai_socktype = SOCK_STREAM;
	hints.ai_protocol = IPPROTO_TCP;
	hints.ai_flags = AI_PASSIVE;

	iResult = getaddrinfo(nullptr, RPC_PORT_STRING, &hints, &result);
	if (iResult != 0) {
		printf("getaddrinfo failed with error: %d\n", iResult);
		WSACleanup();
		return;
	}
	ListenSocket = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
	if (ListenSocket == INVALID_SOCKET) {
		printf("socket failed with error: %ld\n", WSAGetLastError());
		freeaddrinfo(result);
		WSACleanup();
		return;
	}

	iResult = bind(ListenSocket, result->ai_addr, (int)result->ai_addrlen);
	if (iResult == SOCKET_ERROR) {
		printf("bind failed with error: %d\n", WSAGetLastError());
		freeaddrinfo(result);
		closesocket(ListenSocket);
		WSACleanup();
		return;
	}

	freeaddrinfo(result);

	iResult = listen(ListenSocket, SOMAXCONN);
	if (iResult == SOCKET_ERROR) {
		printf("listen failed with error: %d\n", WSAGetLastError());
		closesocket(ListenSocket);
		WSACleanup();
		return;
	}
	printf("Opened socket. Enter ipconfig in a command line to check your local IPv4 address. I don't know how to automate that at the moment.\n");
	while (true) {
		ClientSocket = accept(ListenSocket, NULL, NULL);

		if (ClientSocket != INVALID_SOCKET) {
			printf("Connected to client %d\n", ClientSocket);
			RPCHandler(ClientSocket);
		}
	}
}