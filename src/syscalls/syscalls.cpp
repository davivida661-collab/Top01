#pragma once
#ifdef PLATFORM_LINUX_MOCK
#include "syscalls.h"
bool SyscallResolver::Initialize(){ return true; }
namespace syscall {
NTSTATUS NtOpenProcess(HANDLE*, ACCESS_MASK, void*, void*){ return -1; }
NTSTATUS NtReadVirtualMemory(HANDLE,PVOID,PVOID,SIZE_T,SIZE_T*){ return -1; }
NTSTATUS NtWriteVirtualMemory(HANDLE,PVOID,PVOID,SIZE_T,SIZE_T*){ return -1; }
NTSTATUS NtAllocateVirtualMemory(HANDLE,PVOID*,ULONG_PTR,SIZE_T*,ULONG,ULONG){ return -1; }
NTSTATUS NtClose(HANDLE){ return 0; }
}
#endif
