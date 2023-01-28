using System;
using System.Runtime.InteropServices;
using System.Text;
// ReSharper disable UnassignedField.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

namespace Memory;
public static partial class Imps
{
    [LibraryImport("kernel32.dll")]
    public static partial nint OpenProcess(
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int dwProcessId
    );

#if WINXP
#else
    [LibraryImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static partial nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
        out MEMORY_BASIC_INFORMATION32 lpBuffer, nuint dwLength);

    [LibraryImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
    public static partial nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
        out MEMORY_BASIC_INFORMATION64 lpBuffer, nuint dwLength);


    [LibraryImport("kernel32.dll")]
    public static partial void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
#endif

    [LibraryImport("kernel32.dll")]
    public static partial nint OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial int SuspendThread(nint hThread);

    [LibraryImport("kernel32.dll")]
    internal static partial int ResumeThread(nint hThread);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(
        nint hProcess,
        nuint lpBaseAddress,
        string lpBuffer,
        nuint nSize,
        out nint lpNumberOfBytesWritten
    );

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetPrivateProfileString(
        string lpAppName,
        string lpKeyName,
        string lpDefault,
        StringBuilder lpReturnedString,
        uint nSize,
        string lpFileName);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualFreeEx(
        nint hProcess,
        nuint lpAddress,
        nuint dwSize,
        uint dwFreeType
    );

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, [MarshalAs(UnmanagedType.LPArray)] out byte[] lpBuffer,
        nuint nSize, nint lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, out byte lpBuffer,
        nuint nSize, nint lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, out short lpBuffer,
        nuint nSize, nint lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, long lpBuffer, nuint nSize,
        ulong lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, nint lpBuffer, nuint nSize,
        out ulong lpNumberOfBytesRead);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nuint VirtualAllocEx(
        nint hProcess,
        nuint lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
    );

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualProtectEx(nint hProcess, nuint lpAddress,
        nint dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nuint GetProcAddress(
        nint hModule,
        string procName
    );

    [LibraryImport("kernel32.dll")]
    internal static partial int CloseHandle(
        nint hObject
    );

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandle(
        string lpModuleName
    );

    [LibraryImport("kernel32", SetLastError = true)]
    internal static partial int WaitForSingleObject(
        nint handle,
        int milliseconds
    );

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, nuint nSize,
        nint lpNumberOfBytesWritten);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, long lpBuffer, nuint nSize,
        nint lpNumberOfBytesWritten);

    // Added to avoid casting to UIntPtr

    [LibraryImport("kernel32")]
    public static partial nint CreateRemoteThread(
        nint hProcess,
        nint lpThreadAttributes,
        uint dwStackSize,
        nuint lpStartAddress, // raw Pointer into remote process  
        nuint lpParameter,
        uint dwCreationFlags,
        out nint lpThreadId
    );

    [LibraryImport("kernel32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWow64Process(nint hProcess, [MarshalAs(UnmanagedType.Bool)] out bool lpSystemInfo);

    /*
     typedef NTSTATUS (WINAPI *LPFUN_NtCreateThreadEx)
        (
          OUT PHANDLE hThread,
          IN ACCESS_MASK DesiredAccess,
          IN LPVOID ObjectAttributes,
          IN HANDLE ProcessHandle,
          IN LPTHREAD_START_ROUTINE lpStartAddress,
          IN LPVOID lpParameter,
          IN BOOL CreateSuspended,
          IN ULONG StackZeroBits,
          IN ULONG SizeOfStackCommit,
          IN ULONG SizeOfStackReserve,
          OUT LPVOID lpBytesBuffer
        );
     */
    [LibraryImport("ntdll.dll", SetLastError = true)]
    internal static partial NTSTATUS NtCreateThreadEx(out nint hProcess, AccessMask desiredAccess,
        nint objectAttributes, nuint processHandle, nint startAddress, nint parameter,
        ThreadCreationFlags inCreateSuspended, int stackZeroBits, int sizeOfStack, int maximumStackSize,
        nint attributeList);

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

    internal enum NTSTATUS
    {
        Success = 0x00
    }

    internal enum AccessMask
    {
        SpecificRightsAll = 0xFFFF,
        StandardRightsAll = 0x1F0000
    }

    internal enum ThreadCreationFlags
    {
        Immediately = 0x0,
        CreateSuspended = 0x01,
        HideFromDebugger = 0x04,
        StackSizeParamIsAReservation = 0x10000
    }

    public struct SYSTEM_INFO
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

    public struct MEMORY_BASIC_INFORMATION32
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public uint RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    public struct MEMORY_BASIC_INFORMATION64
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

    public struct MEMORY_BASIC_INFORMATION
    {
        public nuint BaseAddress;
        public nuint AllocationBase;
        public uint AllocationProtect;
        public long RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    private enum SnapshotFlags : uint
    {
        HeapList = 0x00000001,
        Process = 0x00000002,
        Thread = 0x00000004,
        Module = 0x00000008,
        Module32 = 0x00000010,
        Inherit = 0x80000000,
        All = 0x0000001F,
        NoHeaps = 0x40000000
    }

    [Flags]
    public enum ThreadAccess
    {
        Terminate = 0x0001,
        SuspendResume = 0x0002,
        GetContext = 0x0008,
        SetContext = 0x0010,
        SetInformation = 0x0020,
        QueryInformation = 0x0040,
        SetThreadToken = 0x0080,
        Impersonate = 0x0100,
        DirectImpersonation = 0x0200
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

    [LibraryImport("ntdll.dll", SetLastError = true)]
    internal static partial int NtQueryInformationThread(
        nint threadHandle,
        ThreadInfoClass threadInformationClass,
        nint threadInformation,
        int threadInformationLength,
        nint returnLengthPtr);

    public enum ThreadInfoClass
    {
        ThreadQuerySetWin32StartAddress = 9
    }
}