using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Memory
{
    public class Imps
    {
        [DllImport("kernel32.dll")]
        public static extern nint OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId
        );

#if WINXP
#else
        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
            out MEMORY_BASIC_INFORMATION32 lpBuffer, nuint dwLength);

        [DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx")]
        public static extern nuint Native_VirtualQueryEx(nint hProcess, nuint lpAddress,
            out MEMORY_BASIC_INFORMATION64 lpBuffer, nuint dwLength);


        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);
#endif

        [DllImport("kernel32.dll")]
        public static extern nint OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SuspendThread(nint hThread);

        [DllImport("kernel32.dll")]
        public static extern int ResumeThread(nint hThread);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(
            nint hProcess,
            nuint lpBaseAddress,
            string lpBuffer,
            nuint nSize,
            out nint lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern uint GetPrivateProfileString(
            string lpAppName,
            string lpKeyName,
            string lpDefault,
            StringBuilder lpReturnedString,
            uint nSize,
            string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(
            nint hProcess,
            nuint lpAddress,
            nuint dwSize,
            uint dwFreeType
        );

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, [Out] byte[] lpBuffer,
            nuint nSize, nint lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, [Out] byte lpBuffer,
            nuint nSize, nint lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, [Out] short lpBuffer,
            nuint nSize, nint lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, long lpBuffer, nuint nSize,
            ulong lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(nint hProcess, nuint lpBaseAddress, nint lpBuffer, nuint nSize,
            out ulong lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
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

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            nint hModule,
            string procName
        );

        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(
            nint hObject
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern nint GetModuleHandle(
            string lpModuleName
        );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern int WaitForSingleObject(
            nint handle,
            int milliseconds
        );

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, nuint nSize,
            nint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, long lpBuffer, nuint nSize,
            nint lpNumberOfBytesWritten);

        // Added to avoid casting to UIntPtr
        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(nint hProcess, nuint lpBaseAddress, byte[] lpBuffer, nuint nSize,
            out nint lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern nint CreateRemoteThread(
            nint hProcess,
            nint lpThreadAttributes,
            uint dwStackSize,
            nuint lpStartAddress, // raw Pointer into remote process  
            nuint lpParameter,
            uint dwCreationFlags,
            out nint lpThreadId
        );

        [DllImport("kernel32")]
        public static extern bool IsWow64Process(nint hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        //ReSharper disable once InconsistentNaming
        public static extern nint CreateToolhelp32Snapshot([In] uint dwFlags, [In] uint th32ProcessID);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool Process32First([In] nint hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        public static extern bool Module32First(nint hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32.dll")]
        public static extern bool Module32Next(nint hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool Process32Next([In] nint hSnapshot, ref PROCESSENTRY32 lppe);

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
        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern NTSTATUS NtCreateThreadEx(out IntPtr hProcess, AccessMask desiredAccess,
            IntPtr objectAttributes, UIntPtr processHandle, IntPtr startAddress, IntPtr parameter,
            ThreadCreationFlags inCreateSuspended, Int32 stackZeroBits, Int32 sizeOfStack, Int32 maximumStackSize,
            IntPtr attributeList);

        // privileges
        public const int ProcessCreateThread = 0x0002;
        public const int ProcessQueryInformation = 0x0400;
        public const int ProcessVmOperation = 0x0008;
        public const int ProcessVmWrite = 0x0020;
        public const int ProcessVmRead = 0x0010;

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

        internal enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000
        }

        public struct SYSTEM_INFO
        {
            public ushort ProcessorArchitecture;
            ushort reserved;
            public uint PageSize;
            public UIntPtr MinimumApplicationAddress;
            public UIntPtr MaximumApplicationAddress;
            public IntPtr ActiveProcessorMask;
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
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
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
        public enum ThreadAccess : int
        {
            Terminate = (0x0001),
            SuspendResume = (0x0002),
            GetContext = (0x0008),
            SetContext = (0x0010),
            SetInformation = (0x0020),
            QueryInformation = (0x0040),
            SetThreadToken = (0x0080),
            Impersonate = (0x0100),
            DirectImpersonation = (0x0200)
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

        //inner struct used only internally
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            const int MAX_PATH = 260;
            private uint dwSize;
            private uint cntUsage;
            private uint th32ProcessID;
            private nint th32DefaultHeapID;
            private uint th32ModuleID;
            private uint cntThreads;
            private uint th32ParentProcessID;
            private int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            private string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public struct MODULEENTRY32
        {
            private uint dwSize;
            private uint th32ModuleID;
            private uint th32ProcessID;
            private uint GlblcntUsage;
            private uint ProccntUsage;
            private nint modBaseAddr;
            private uint modBaseSize;
            private nint hModule;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            private string szModule;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            private string szExePath;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern int NtQueryInformationThread(
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
}