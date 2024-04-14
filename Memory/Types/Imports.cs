using System;
using System.Runtime.InteropServices;

namespace Memory;
public static partial class Imps
{
    [LibraryImport("kernel32.dll")]
    public static partial nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static partial nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress, out MemoryBasicInformation32 lpBuffer, nuint dwLength);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static partial nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress, out MemoryBasicInformation64 lpBuffer, nuint dwLength);

    [LibraryImport("kernel32.dll")]
    public static partial void GetSystemInfo(out SystemInfo lpSystemInfo);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, void* lpBuffer, nuint nSize, ulong lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, nint lpBuffer, nuint nSize, out ulong lpNumberOfBytesRead);
    
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, int dwSize, int lpNumberOfBytesRead = 0);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nuint VirtualAllocEx(nint hProcess, nuint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualProtectEx(nint hProcess, nuint lpAddress, nint dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, nuint nSize, nint lpNumberOfBytesWritten);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static unsafe partial bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, void* lpBuffer, nuint nSize, nint lpNumberOfBytesWritten);
    
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWow64Process(nint hProcess, [MarshalAs(UnmanagedType.Bool)] out bool lpSystemInfo);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualFreeEx(nint hProcess, nuint lpAddress, nuint dwSize, uint dwFreeType);
    
    public const uint MemFree = 0x10000;
    public const uint MemCommit = 0x00001000;
    public const uint MemReserve = 0x00002000;
    public const uint MemRelease = 0x8000;

    public const uint ReadOnly = 0x02;
    public const uint ReadWrite = 0x04;
    public const uint WriteCopy = 0x08;
    public const uint ExecuteReadwrite = 0x40;
    public const uint ExecuteWriteCopy = 0x80;
    public const uint Execute = 0x10;
    public const uint ExecuteRead = 0x20;

    public const uint Guard = 0x100;
    public const uint NoAccess = 0x01;

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