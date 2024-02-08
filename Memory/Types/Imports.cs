using System;
using System.Runtime.InteropServices;

namespace Memory;
public static class Imps
{
    [DllImport("kernel32.dll")]
    public static extern nint OpenProcess(
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int dwProcessId
    );

    [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static extern nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
        out MemoryBasicInformation32 lpBuffer, nuint dwLength);

    [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static extern nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
        out MemoryBasicInformation64 lpBuffer, nuint dwLength);

    [DllImport("kernel32.dll")]
    public static extern void GetSystemInfo(out SystemInfo lpSystemInfo);

    [DllImport("kernel32.dll")]
    public static extern unsafe bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, void* lpBuffer, nuint nSize,
        ulong lpNumberOfBytesRead);

    [DllImport("kernel32.dll")]
    public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, nint lpBuffer, nuint nSize,
        out ulong lpNumberOfBytesRead);
    
    [DllImport("kernel32.dll")]
    public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead = 0);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nuint VirtualAllocEx(
        nint hProcess,
        nuint lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
    );

    [DllImport("kernel32.dll")]
    public static extern bool VirtualProtectEx(nint hProcess, nuint lpAddress,
        nint dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

    [DllImport("kernel32.dll")]
    public static extern bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, nuint nSize,
        nint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    public static extern unsafe bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, void* lpBuffer, nuint nSize,
        nint lpNumberOfBytesWritten);
    
    [DllImport("kernel32")]
    public static extern bool IsWow64Process(nint hProcess, [MarshalAs(UnmanagedType.Bool)] out bool lpSystemInfo);
    
    // used for memory allocation
    public const uint MemFree = 0x10000;
    public const uint MemCommit = 0x00001000;
    public const uint MemReserve = 0x00002000;

    public const uint Readonly = 0x02;
    public const uint Readwrite = 0x04;
    public const uint Writecopy = 0x08;
    public const uint ExecuteReadwrite = 0x40;
    public const uint ExecuteWritecopy = 0x80;
    public const uint Execute = 0x10;
    public const uint ExecuteRead = 0x20;

    public const uint Guard = 0x100;
    public const uint Noaccess = 0x01;

    public const uint MemPrivate = 0x20000;
    public const uint MemImage = 0x1000000;
    public const uint MemMapped = 0x40000;

    public struct SystemInfo
    {
        public ushort ProcessorArchitecture;
        private ushort _reserved;
        public uint PageSize;
        public nuint MinimumApplicationAddress;
        public nuint MaximumApplicationAddress;
        public nint ActiveProcessorMask;
        public uint NumberOfProcessors;
        public uint ProcessorType;
        public uint AllocationGranularity;
        public ushort ProcessorLevel;
        public ushort ProcessorRevision;
    }

    public struct MemoryBasicInformation32
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public uint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    public struct MemoryBasicInformation64
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public uint Alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint Alignment2;
    }

    public struct MemoryBasicInformation
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public long RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    [Flags]
    public enum MemoryProtection : uint
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierFlag = 0x100,
        NoCacheModifierFlag = 0x200,
        WriteCombineModifierFlag = 0x400
    }
}