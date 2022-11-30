using System;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.ComponentModel;
using static Memory.Imps;

namespace Memory;

/// <summary>
/// Memory.dll class. Full documentation at https://github.com/erfg12/memory.dll/wiki
/// </summary>
public partial class Mem
{
    public Proc MProc = new();

    public UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer)
    {
        UIntPtr retVal;

        // TODO: Need to change this to only check once.
        if (MProc.Is64Bit || IntPtr.Size == 8)
        {
            // 64 bit
            MEMORY_BASIC_INFORMATION64 tmp64 = new();
            retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp64, new((uint)Marshal.SizeOf(tmp64)));

            lpBuffer.BaseAddress = tmp64.BaseAddress;
            lpBuffer.AllocationBase = tmp64.AllocationBase;
            lpBuffer.AllocationProtect = tmp64.AllocationProtect;
            lpBuffer.RegionSize = (long)tmp64.RegionSize;
            lpBuffer.State = tmp64.State;
            lpBuffer.Protect = tmp64.Protect;
            lpBuffer.Type = tmp64.Type;

            return retVal;
        }

        MEMORY_BASIC_INFORMATION32 tmp32 = new();

        retVal = Native_VirtualQueryEx(hProcess, lpAddress, out tmp32, new((uint)Marshal.SizeOf(tmp32)));

        lpBuffer.BaseAddress = tmp32.BaseAddress;
        lpBuffer.AllocationBase = tmp32.AllocationBase;
        lpBuffer.AllocationProtect = tmp32.AllocationProtect;
        lpBuffer.RegionSize = tmp32.RegionSize;
        lpBuffer.State = tmp32.State;
        lpBuffer.Protect = tmp32.Protect;
        lpBuffer.Type = tmp32.Type;

        return retVal;
    }

    /// <summary>
    /// Open the PC game process with all security and access rights.
    /// </summary>
    /// <param name="pid">Use process name or process ID here.</param>
    /// <returns>Process opened successfully or failed.</returns>
    /// <param name="failReason">Show reason open process fails</param>
    public bool OpenProcess(int pid, out string failReason)
    {
        if (pid <= 0)
        {
            failReason = "OpenProcess given proc ID 0.";
            Debug.WriteLine("ERROR: OpenProcess given proc ID 0.");
            return false;
        }


        if (MProc.Process != null && MProc.Process.Id == pid)
        {
            failReason = "mProc.Process is null";
            return true;
        }

        try
        {
            MProc.Process = Process.GetProcessById(pid);

            if (MProc.Process is { Responding: false })
            {
                Debug.WriteLine("ERROR: OpenProcess: Process is not responding or null.");
                failReason = "Process is not responding or null.";
                return false;
            }

            MProc.Handle = Imps.OpenProcess(0x1F0FFF, true, pid);

            try
            {
                Process.EnterDebugMode();
            }
            catch (Win32Exception)
            {
                //Debug.WriteLine("WARNING: You are not running with raised privileges! Visit https://github.com/erfg12/memory.dll/wiki/Administrative-Privileges"); 
            }

            if (MProc.Handle == IntPtr.Zero)
            {
                int eCode = Marshal.GetLastWin32Error();
                Debug.WriteLine(
                    "ERROR: OpenProcess has failed opening a handle to the target process (GetLastWin32ErrorCode: " +
                    eCode + ")");
                Process.LeaveDebugMode();
                MProc = null;
                failReason = "failed opening a handle to the target process(GetLastWin32ErrorCode: " + eCode + ")";
                return false;
            }

            // Lets set the process to 64bit or not here (cuts down on api calls)
            MProc.Is64Bit = Environment.Is64BitOperatingSystem &&
                            (IsWow64Process(MProc.Handle, out bool retVal) && !retVal);

            MProc.MainModule = MProc.Process.MainModule;

            //GetModules();

            Debug.WriteLine("Process #" + MProc.Process + " is now open.");
            failReason = "";
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ERROR: OpenProcess has crashed. " + ex);
            failReason = "OpenProcess has crashed. " + ex;
            return false;
        }
    }


    /// <summary>
    /// Open the PC game process with all security and access rights.
    /// </summary>
    /// <param name="proc">Use process name or process ID here.</param>
    /// <param name="failReason">Show reason open process fails</param>
    /// <returns></returns>
    public bool OpenProcess(string proc, out string failReason)
    {
        return OpenProcess(GetProcIdFromName(proc), out failReason);
    }

    /// <summary>
    /// Open the PC game process with all security and access rights.
    /// </summary>
    /// <param name="proc">Use process name or process ID here.</param>
    /// <returns></returns>
    public bool OpenProcess(string proc)
    {
        return OpenProcess(GetProcIdFromName(proc), out string _);
    }

    /// <summary>
    /// Open the PC game process with all security and access rights.
    /// </summary>
    /// <param name="pid">Use process name or process ID here.</param>
    /// <returns></returns>
    public bool OpenProcess(int pid)
    {
        return OpenProcess(pid, out string _);
    }

    public void SetFocus()
    {
        SetForegroundWindow(MProc.Process.MainWindowHandle);
    }

    /// <summary>
    /// Get the process ID number by process name.
    /// </summary>
    /// <param name="name">Example: "eqgame". Use task manager to find the name. Do not include .exe</param>
    /// <returns></returns>
    public int GetProcIdFromName(string name) //new 1.0.2 function
    {
        Process[] processlist = Process.GetProcesses();

        if (name.ToLower().Contains(".exe"))
            name = name.Replace(".exe", "");
        if (name.ToLower().Contains(".bin")) // test
            name = name.Replace(".bin", "");

        foreach (Process theProcess in processlist)
        {
            if (theProcess.ProcessName.Equals(name,
                    StringComparison
                        .CurrentCultureIgnoreCase)) //find (name).exe in the process list (use task manager to find the name)
                return theProcess.Id;
        }

        return 0; //if we fail to find it
    }


    /// <summary>
    /// Get code. If just the ini file name is given with no path, it will assume the file is next to the executable.
    /// </summary>
    /// <param name="name">label for address or code</param>
    /// <param name="iniFile">path and name of ini file</param>
    /// <returns></returns>
    public string LoadCode(string name, string iniFile)
    {
        StringBuilder returnCode = new(1024);

        if (!string.IsNullOrEmpty(iniFile))
        {
            if (File.Exists(iniFile))
            {
                _ = GetPrivateProfileString("codes", name, "", returnCode, (uint)returnCode.Capacity, iniFile);
                //Debug.WriteLine("read_ini_result=" + read_ini_result); number of characters returned
            }
            else
                Debug.WriteLine("ERROR: ini file \"" + iniFile + "\" not found!");
        }
        else
            returnCode.Append(name);

        return returnCode.ToString();
    }

    private int LoadIntCode(string name, string path)
    {
        try
        {
            int intValue = Convert.ToInt32(LoadCode(name, path), 16);
            return intValue >= 0 ? intValue : 0;
        }
        catch
        {
            Debug.WriteLine("ERROR: LoadIntCode function crashed!");
            return 0;
        }
    }

    /// <summary>
    /// Make a named pipe (if not already made) and call to a remote function.
    /// </summary>
    /// <param name="func">remote function to call</param>
    /// <param name="name">name of the thread</param>
    public void ThreadStartClient(string func, string name)
    {
        //ManualResetEvent SyncClientServer = (ManualResetEvent)obj;
        using NamedPipeClientStream pipeStream = new NamedPipeClientStream(name);
        if (!pipeStream.IsConnected)
            pipeStream.Connect();

        //MessageBox.Show("[Client] Pipe connection established");
        using StreamWriter sw = new StreamWriter(pipeStream);
        if (!sw.AutoFlush)
            sw.AutoFlush = true;
        sw.WriteLine(func);
    }

    #region protection

    public bool ChangeProtection(string code, MemoryProtection newProtection, out MemoryProtection oldProtection)
    {
        nuint theCode = FollowMultiLevelPointer(code);
        if (theCode == nuint.Zero || MProc.Handle == nint.Zero)
        {
            oldProtection = default;
            return false;
        }

        return VirtualProtectEx(MProc.Handle, theCode, MProc.Is64Bit ? 8 : 4, newProtection, out oldProtection);
    }

    public bool ChangeProtection(nuint address, string code, MemoryProtection newProtection,
        out MemoryProtection oldProtection)
    {
        nuint addy = code != ""
            ? FollowMultiLevelPointer(address.ToString("X") + code)
            : address;
        if (addy != nuint.Zero
            && MProc.Handle != nint.Zero)
            return VirtualProtectEx(MProc.Handle, addy,
                MProc.Is64Bit ? 8 : 4, newProtection, out oldProtection);
        oldProtection = default;
        return false;
    }

    #endregion

    /// <summary>
    /// Convert code from string to real address. If path is not blank, will pull from ini file.
    /// </summary>
    /// <param name="name">label in ini file or code</param>
    /// <param name="path">path to ini file (OPTIONAL)</param>
    /// <param name="size">size of address (default is 8)</param>
    /// <returns></returns>
    public nuint GetCode(string name, string path = "", int size = 8)
    {
        if (MProc == null)
            return nuint.Zero;

        if (MProc.Is64Bit)
        {
            if (size == 8) size = 16; //change to 64bit
            return Get64BitCode(name, path, size); //jump over to 64bit code grab
        }

        string theCode = !string.IsNullOrEmpty(path) ? LoadCode(name, path) : name;

        if (string.IsNullOrEmpty(theCode))
        {
            //Debug.WriteLine("ERROR: LoadCode returned blank. NAME:" + name + " PATH:" + path);
            return nuint.Zero;
        }

        //Debug.WriteLine("Found code=" + theCode + " NAME:" + name + " PATH:" + path);
        // remove spaces
        if (theCode.Contains(' '))
            theCode = theCode.Replace(" ", string.Empty);

        if (!theCode.Contains('+') && !theCode.Contains(','))
        {
            try
            {
                return new(Convert.ToUInt64(theCode, 16));
            }
            catch
            {
                Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                return nuint.Zero;
            }
        }

        string newOffsets = theCode;

        if (theCode.Contains('+'))
            newOffsets = theCode.Substring(theCode.IndexOf('+') + 1);

        byte[] memoryAddress = new byte[size];

        if (newOffsets.Contains(','))
        {
            List<int> offsetsList = new List<int>();

            string[] newerOffsets = newOffsets.Split(',');
            foreach (string oldOffsets in newerOffsets)
            {
                string test = oldOffsets;
                if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                int preParse;
                if (!oldOffsets.Contains('-'))
                    preParse = int.Parse(test, NumberStyles.AllowHexSpecifier);
                else
                {
                    test = test.Replace("-", "");
                    preParse = int.Parse(test, NumberStyles.AllowHexSpecifier);
                    preParse *= -1;
                }

                offsetsList.Add(preParse);
            }

            int[] offsets = offsetsList.ToArray();

            if (theCode.Contains("base") || theCode.Contains("main"))
                ReadProcessMemory(MProc.Handle, (nuint)((int)MProc.MainModule.BaseAddress + offsets[0]),
                    memoryAddress, (nuint)size, nint.Zero);
            else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains('+'))
            {
                string[] moduleName = theCode.Split('+');
                nint altModule = nint.Zero;
                if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                    !moduleName[0].ToLower().Contains(".bin"))
                {
                    string theAddr = moduleName[0];
                    if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                    altModule = nint.Parse(theAddr, NumberStyles.HexNumber);
                }
                else
                {
                    try
                    {
                        altModule = GetModuleAddressByName(moduleName[0]);
                    }
                    catch
                    {
                        Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                        //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                    }
                }

                ReadProcessMemory(MProc.Handle, (nuint)((int)altModule + offsets[0]), memoryAddress,
                    (nuint)size, nint.Zero);
            }
            else
                ReadProcessMemory(MProc.Handle, (nuint)(offsets[0]), memoryAddress, (nuint)size, nint.Zero);

            uint num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.

            nuint base1 = nuint.Zero;

            for (int i = 1; i < offsets.Length; i++)
            {
                base1 = new(Convert.ToUInt64(num1 + offsets[i]));
                ReadProcessMemory(MProc.Handle, base1, memoryAddress, (nuint)size, nint.Zero);
                num1 = BitConverter.ToUInt32(memoryAddress, 0); //ToUInt64 causes arithmetic overflow.
            }

            return base1;
        }
        else // no offsets
        {
            int trueCode = Convert.ToInt32(newOffsets, 16);
            nint altModule = nint.Zero;
            //Debug.WriteLine("newOffsets=" + newOffsets);
            if (theCode.ToLower().Contains("base") || theCode.ToLower().Contains("main"))
                altModule = MProc.MainModule.BaseAddress;
            else if (!theCode.ToLower().Contains("base") && !theCode.ToLower().Contains("main") &&
                     theCode.Contains("+"))
            {
                string[] moduleName = theCode.Split('+');
                if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                    !moduleName[0].ToLower().Contains(".bin"))
                {
                    string theAddr = moduleName[0];
                    if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                    altModule = nint.Parse(theAddr, NumberStyles.HexNumber);
                }
                else
                {
                    try
                    {
                        altModule = GetModuleAddressByName(moduleName[0]);
                    }
                    catch
                    {
                        Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                        //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                    }
                }
            }
            else
                altModule = GetModuleAddressByName(theCode.Split('+')[0]);

            return (UIntPtr)((int)altModule + trueCode);
        }
    }

    /// <summary>
    /// Retrieve mProc.Process module base address by name
    /// </summary>
    /// <param name="name">name of module</param>
    /// <returns></returns>
    public nint GetModuleAddressByName(string name)
    {
        return MProc.Process.Modules.Cast<ProcessModule>().SingleOrDefault(m =>
            string.Equals(m.ModuleName, name, StringComparison.OrdinalIgnoreCase))!.BaseAddress;
    }

    /// <summary>
    /// Convert code from string to real address. If path is not blank, will pull from ini file.
    /// </summary>
    /// <param name="name">label in ini file OR code</param>
    /// <param name="path">path to ini file (OPTIONAL)</param>
    /// <param name="size">size of address (default is 16)</param>
    /// <returns></returns>
    public nuint Get64BitCode(string name, string path = "", int size = 16)
    {
        string theCode = !string.IsNullOrEmpty(path) ? LoadCode(name, path) : name;

        if (string.IsNullOrEmpty(theCode))
            return nuint.Zero;

        // remove spaces
        if (theCode.Contains(' '))
            theCode = theCode.Replace(" ", string.Empty);

        string newOffsets = theCode;
        if (theCode.Contains('+'))
            newOffsets = theCode[(theCode.IndexOf('+') + 1)..];

        byte[] memoryAddress = new byte[size];

        if (!theCode.Contains('+') && !theCode.Contains(','))
        {
            try
            {
                return new(Convert.ToUInt64(theCode, 16));
            }
            catch
            {
                Console.WriteLine("Error in GetCode(). Failed to read address " + theCode);
                return nuint.Zero;
            }
        }

        if (newOffsets.Contains(','))
        {
            List<long> offsetsList = new();

            string[] newerOffsets = newOffsets.Split(',');
            foreach (string oldOffsets in newerOffsets)
            {
                string test = oldOffsets;
                if (oldOffsets.Contains("0x")) test = oldOffsets.Replace("0x", "");
                long preParse;
                if (!oldOffsets.Contains("-"))
                    preParse = long.Parse(test, NumberStyles.AllowHexSpecifier);
                else
                {
                    test = test.Replace("-", "");
                    preParse = long.Parse(test, NumberStyles.AllowHexSpecifier);
                    preParse = preParse * -1;
                }

                offsetsList.Add(preParse);
            }

            long[] offsets = offsetsList.ToArray();

            if (theCode.Contains("base") || theCode.Contains("main"))
                ReadProcessMemory(MProc.Handle, (nuint)(MProc.MainModule.BaseAddress + offsets[0]),
                    memoryAddress, (nuint)size, nint.Zero);
            else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains('+'))
            {
                string[] moduleName = theCode.Split('+');
                nint altModule = nint.Zero;
                if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                    !moduleName[0].ToLower().Contains(".bin"))
                    altModule = (nint)long.Parse(moduleName[0], NumberStyles.HexNumber);
                else
                {
                    try
                    {
                        altModule = GetModuleAddressByName(moduleName[0]);
                    }
                    catch
                    {
                        Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                        //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                    }
                }

                ReadProcessMemory(MProc.Handle, (nuint)(altModule + offsets[0]), memoryAddress,
                    (nuint)size, nint.Zero);
            }
            else // no offsets
                ReadProcessMemory(MProc.Handle, (nuint)offsets[0], memoryAddress, (nuint)size, nint.Zero);

            long num1 = BitConverter.ToInt64(memoryAddress, 0);

            nuint base1 = nuint.Zero;

            for (int i = 1; i < offsets.Length; i++)
            {
                base1 = new(Convert.ToUInt64(num1 + offsets[i]));
                ReadProcessMemory(MProc.Handle, base1, memoryAddress, (nuint)size, nint.Zero);
                num1 = BitConverter.ToInt64(memoryAddress, 0);
            }

            return base1;
        }
        else
        {
            long trueCode = Convert.ToInt64(newOffsets, 16);
            nint altModule = nint.Zero;
            if (theCode.Contains("base") || theCode.Contains("main"))
                altModule = MProc.MainModule.BaseAddress;
            else if (!theCode.Contains("base") && !theCode.Contains("main") && theCode.Contains("+"))
            {
                string[] moduleName = theCode.Split('+');
                if (!moduleName[0].ToLower().Contains(".dll") && !moduleName[0].ToLower().Contains(".exe") &&
                    !moduleName[0].ToLower().Contains(".bin"))
                {
                    string theAddr = moduleName[0];
                    if (theAddr.Contains("0x")) theAddr = theAddr.Replace("0x", "");
                    altModule = nint.Parse(theAddr, NumberStyles.HexNumber);
                }
                else
                {
                    try
                    {
                        altModule = GetModuleAddressByName(moduleName[0]);
                    }
                    catch
                    {
                        Debug.WriteLine("Module " + moduleName[0] + " was not found in module list!");
                        //Debug.WriteLine("Modules: " + string.Join(",", mProc.Modules));
                    }
                }
            }
            else
                altModule = GetModuleAddressByName(theCode.Split('+')[0]);

            return (nuint)(altModule + trueCode);
        }
    }

    public nuint FollowMultiLevelPointer(string path)
    {
        nuint base1 = nuint.Zero;
        string[] offsets = path.Split(',');
        if (offsets[0].Contains("base") || offsets[0].Contains("main"))
        {
            base1 = (nuint)MProc.MainModule.BaseAddress.ToInt64();
            string[] additions = offsets[0].Split('+');
            if (additions.Length > 1)
                for (int i = 1; i < additions.Length; i++)
                    base1 += nuint.Parse(additions[i], NumberStyles.HexNumber);
        }
        else if (!int.TryParse(offsets[0], out _)) //this is so genius
        {
            string[] additions = offsets[0].Split('+');
            base1 = (nuint)GetModuleAddressByName(additions[0]).ToInt64();
            if (additions.Length > 1)
                for (int i = 1; i < additions.Length; i++)
                    base1 += nuint.Parse(additions[i], NumberStyles.HexNumber);
        }
        nuint[] offsetsInt = new nuint[offsets.Length - 1];
        for (int i = 1; i < offsets.Length; i++)
        {
            string[] additions = offsets[i].Split('+');
            
            offsetsInt[i - 1] = nuint.Parse(additions[0], NumberStyles.HexNumber);
            if (additions.Length > 1)
                for (int j = 1; j < additions.Length; j++)
                    offsetsInt[i - 1] += nuint.Parse(additions[j], NumberStyles.HexNumber);
        }
        nuint address = base1;
        for (int i = 0; i < offsetsInt.Length; i++)
        {
            if (i == 0) address = ReadMemory<nuint>(base1);
            if (i == offsetsInt.Length - 1)
            {
                address += offsetsInt[i];
                return address;
            }
            address = ReadMemory<nuint>(address + offsetsInt[i]);
        }
        return address;
    }

    /// <summary>
    /// Close the process when finished.
    /// </summary>
    public void CloseProcess()
    {
        CloseHandle(MProc.Handle);
        MProc = null;
    }

    /// <summary>
    /// Inject a DLL file.
    /// </summary>
    /// <param name="strDllName">path and name of DLL file. Ex: "C:\MyTrainer\inject.dll" or "inject.dll" if the DLL file is in the same directory as the trainer.</param>
    public bool InjectDll(string strDllName)
    {
        if (MProc.Process == null)
        {
            // check if process is open first
            Debug.WriteLine("Inject failed due to mProc.Process being null. Is the process not open?");
            return false;
        }

        if (MProc.Process.Modules.Cast<ProcessModule>().Any(pm => pm.ModuleName.StartsWith("inject", StringComparison.InvariantCultureIgnoreCase)))
        {
            return false;
        }

        if (!MProc.Process.Responding)
            return false;

        int lenWrite = strDllName.Length + 1;
        nuint allocMem = VirtualAllocEx(MProc.Handle, (nuint)null, (uint)lenWrite, MemCommit | MemReserve,
            Readwrite);

        WriteProcessMemory(MProc.Handle, allocMem, strDllName, (nuint)lenWrite, out nint _);
        nuint gameProc = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

        nint hThread = CreateRemoteThread(MProc.Handle, (nint)null, 0, gameProc, allocMem, 0, out nint _);

        int result = WaitForSingleObject(hThread, 10 * 1000);
        if (result == 0x00000080L || result == 0x00000102L)
        {
            _ = CloseHandle(hThread);
            return false;
        }

        VirtualFreeEx(MProc.Handle, allocMem, nuint.Zero, 0x8000);

        _ = CloseHandle(hThread);

        return true;
    }

#if WINXP
#else
    /// <summary>
    /// Creates a code cave to write custom opcodes in target process
    /// </summary>
    /// <param name="code">Address to create the trampoline</param>
    /// <param name="newBytes">The opcodes to write in the code cave</param>
    /// <param name="replaceCount">The number of bytes being replaced</param>
    /// <param name="size">size of the allocated region</param>
    /// <param name="makeTrampoline">whether to replace the bytes with the trampoline or not</param>
    /// <remarks>Please ensure that you use the proper replaceCount
    /// if you replace halfway in an instruction you may cause bad things</remarks>
    /// <returns>UIntPtr to created code cave for use for later deallocation</returns>
    public nuint CreateTrampoline(string code, byte[] newBytes, int replaceCount, int size = 0x1000,
        bool makeTrampoline = true)
    {
        if (replaceCount < 5)
            return nuint.Zero; // returning UIntPtr.Zero instead of throwing an exception
        // to better match existing code

        nuint theCode = FollowMultiLevelPointer(code);

        // if x64 we need to try to allocate near the address so we dont run into the +-2GB limit of the 0xE9 jmp

        nuint caveAddress = nuint.Zero;
        nuint preferred = theCode;

        for (int i = 0; i < 10 && caveAddress == nuint.Zero; i++)
        {
            caveAddress = VirtualAllocEx(MProc.Handle, FindFreeBlockForRegion(preferred, (uint)size), (uint)size,
                MemCommit | MemReserve, ExecuteReadwrite);

            if (caveAddress == nuint.Zero)
                preferred = nuint.Add(preferred, 0x10000);
        }

        // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
        if (caveAddress == nuint.Zero)
            caveAddress = VirtualAllocEx(MProc.Handle, nuint.Zero, (uint)size, MemCommit | MemReserve,
                ExecuteReadwrite);

        int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        // (to - from - 5)
        int offset = (int)((long)caveAddress - (long)theCode - 5);

        byte[] jmpBytes = new byte[5 + nopsNeeded];
        jmpBytes[0] = 0xE9;
        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (int i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[5 + newBytes.Length];
        offset = (int)((long)theCode + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xE9;
        BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

        WriteArrayMemory(caveAddress, caveBytes);

        if (makeTrampoline) WriteArrayMemory(theCode, jmpBytes);

        return caveAddress;
    }

    public UIntPtr CreateFarTrampoline(string code, byte[] newBytes, int replaceCount, int size = 0x1000,
        bool makeTrampoline = true, string file = "")
    {
        if (replaceCount < 14)
            return UIntPtr.Zero; // returning UIntPtr.Zero instead of throwing an exception
        // to better match existing code

        UIntPtr theCode = FollowMultiLevelPointer(code);
        UIntPtr address = theCode;

        // We're using a 14-byte 0xFF jmp instruction now, meaning no matter what we won't run into a limit.

        UIntPtr caveAddress = UIntPtr.Zero;

        // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
        if (caveAddress == UIntPtr.Zero)
            caveAddress = VirtualAllocEx(MProc.Handle, UIntPtr.Zero, (uint)size, MemCommit | MemReserve,
                ExecuteReadwrite);

        int nopsNeeded = replaceCount > 14 ? replaceCount - 14 : 0;

        byte[] jmpBytes = new byte[14 + nopsNeeded];
        jmpBytes[0] = 0xFF;
        jmpBytes[1] = 0x25;
        BitConverter.GetBytes((long)caveAddress).CopyTo(jmpBytes, 6);

        for (int i = 14; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[newBytes.Length + 14];

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xFF;
        caveBytes[newBytes.Length + 1] = 0x25;
        BitConverter.GetBytes((long)address + jmpBytes.Length).CopyTo(caveBytes, newBytes.Length + 6);

        WriteArrayMemory(caveAddress, caveBytes);

        if (makeTrampoline) WriteArrayMemory(address, jmpBytes);

        return caveAddress;
    }

    public UIntPtr CreateCallTrampoline(string code, byte[] newBytes, int replaceCount, byte[] varBytes = null!,
        int varOffset = 0, int size = 0x1000, bool makeTrampoline = true)
    {
        if (replaceCount < 16)
            return UIntPtr.Zero; // returning UIntPtr.Zero instead of throwing an exception
        // to better match existing code

        UIntPtr theCode = FollowMultiLevelPointer(code);
        UIntPtr address = theCode;

        // This uses a 16-byte call instruction. Makes it easier to translate aob scripts that return at different places.

        UIntPtr caveAddress = UIntPtr.Zero;

        if (caveAddress == UIntPtr.Zero)
            caveAddress = VirtualAllocEx(MProc.Handle, UIntPtr.Zero, (uint)size, 0x1000 | 0x2000, 0x40);

        int nopsNeeded = replaceCount > 16 ? replaceCount - 16 : 0;

        byte[] jmpBytes = new byte[16 + nopsNeeded];
        jmpBytes[0] = 0xFF;
        jmpBytes[1] = 0x15;
        jmpBytes[2] = 0x02;
        //00 00 00
        jmpBytes[6] = 0xEB;
        jmpBytes[7] = 0x08;
        BitConverter.GetBytes((long)caveAddress).CopyTo(jmpBytes, 8);

        for (int i = 16; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[newBytes.Length + 1];

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xC3;

        WriteArrayMemory(caveAddress, caveBytes);
        if (makeTrampoline) WriteArrayMemory(address, jmpBytes);

        if (varBytes != null!)
            WriteArrayMemory(caveAddress + (nuint)caveBytes.Length + (nuint)varOffset, varBytes);

        return caveAddress;
    }

    public nuint CreateTrampoline(nuint address, string code, byte[] newBytes, int replaceCount,
        int size = 0x1000, bool makeTrampoline = true, string file = "")
    {
        if (replaceCount < 5)
            return nuint.Zero; // returning UIntPtr.Zero instead of throwing an exception
        // to better match existing code

        nuint theCode = address + (nuint)LoadIntCode(code, file);

        // if x64 we need to try to allocate near the address so we dont run into the +-2GB limit of the 0xE9 jmp

        nuint caveAddress = nuint.Zero;
        nuint preferred = theCode;

        for (int i = 0; i < 10 && caveAddress == nuint.Zero; i++)
        {
            caveAddress = VirtualAllocEx(MProc.Handle, FindFreeBlockForRegion(preferred, (uint)size), (uint)size,
                MemCommit | MemReserve, ExecuteReadwrite);

            if (caveAddress == nuint.Zero)
                preferred = nuint.Add(preferred, 0x10000);
        }

        // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
        if (caveAddress == nuint.Zero)
            caveAddress = VirtualAllocEx(MProc.Handle, nuint.Zero, (uint)size, MemCommit | MemReserve,
                ExecuteReadwrite);

        int nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;

        // (to - from - 5)
        int offset = (int)((long)caveAddress - (long)theCode - 5);

        byte[] jmpBytes = new byte[5 + nopsNeeded];
        jmpBytes[0] = 0xE9;
        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (int i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[5 + newBytes.Length];
        offset = (int)((long)theCode + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xE9;
        BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);

        WriteArrayMemory(caveAddress, caveBytes);

        if (makeTrampoline) WriteArrayMemory(theCode, jmpBytes);

        return caveAddress;
    }

    public nuint CreateFarTrampoline(nuint address, string code, byte[] newBytes, int replaceCount,
        int size = 0x1000, bool makeTrampoline = true, string file = "")
    {
        if (replaceCount < 14)
            return nuint.Zero; // returning UIntPtr.Zero instead of throwing an exception
        // to better match existing code

        nuint theCode = address + (nuint)LoadIntCode(code, file);
        nuint theAddress = theCode;

        // We're using a 14-byte 0xFF jmp instruction now, meaning no matter what we won't run into a limit.

        nuint caveAddress = nuint.Zero;

        // Failed to allocate memory around the address we wanted let windows handle it and hope for the best?
        if (caveAddress == nuint.Zero)
            caveAddress = VirtualAllocEx(MProc.Handle, nuint.Zero, (uint)size, MemCommit | MemReserve,
                ExecuteReadwrite);

        int nopsNeeded = replaceCount > 14 ? replaceCount - 14 : 0;

        byte[] jmpBytes = new byte[14 + nopsNeeded];
        jmpBytes[0] = 0xFF;
        jmpBytes[1] = 0x25;
        BitConverter.GetBytes((long)caveAddress).CopyTo(jmpBytes, 6);

        for (int i = 14; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[newBytes.Length + 14];

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xFF;
        caveBytes[newBytes.Length + 1] = 0x25;
        BitConverter.GetBytes((long)theAddress + jmpBytes.Length).CopyTo(caveBytes, newBytes.Length + 6);

        WriteArrayMemory(caveAddress, caveBytes);
        if (makeTrampoline) WriteArrayMemory(address, jmpBytes);

        return caveAddress;
    }

    public nuint CreateCallTrampoline(nuint address, string code, byte[] newBytes, int replaceCount,
        byte[] varBytes = null!, int varOffset = 0, int size = 0x1000, bool makeTrampoline = true, string file = "")
    {
        if (replaceCount < 16)
            return nuint.Zero; // returning UIntPtr.Zero instead of throwing an exception
        // to better match existing code

        nuint theCode = address + (nuint)LoadIntCode(code, file);
        nuint theAddress = theCode;

        // This uses a 16-byte call instruction. Makes it easier to translate aob scripts that return at different places.

        nuint caveAddress = nuint.Zero;

        if (caveAddress == nuint.Zero)
            caveAddress = VirtualAllocEx(MProc.Handle, nuint.Zero, (uint)size, 0x1000 | 0x2000, 0x40);

        int nopsNeeded = replaceCount > 16 ? replaceCount - 16 : 0;

        byte[] jmpBytes = new byte[16 + nopsNeeded];
        jmpBytes[0] = 0xFF;
        jmpBytes[1] = 0x15;
        jmpBytes[2] = 0x02;
        //00 00 00
        jmpBytes[6] = 0xEB;
        jmpBytes[7] = 0x08;
        BitConverter.GetBytes((long)caveAddress).CopyTo(jmpBytes, 8);

        for (int i = 16; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        byte[] caveBytes = new byte[newBytes.Length + 1];

        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xC3;

        WriteArrayMemory(caveAddress, caveBytes);
        if (makeTrampoline) WriteArrayMemory(theAddress, jmpBytes);

        if (varBytes != null!)
            WriteArrayMemory(caveAddress + (nuint)caveBytes.Length + (nuint)varOffset, varBytes);

        return caveAddress;
    }

    public enum TrampolineType
    {
        Jump,
        JumpFar,
        Call
    }

    public byte[] CalculateTrampoline(nuint address, nuint target, TrampolineType type, int replaceCount)
    {
        byte[] trampolineBytes = new byte[replaceCount];

        switch (type)
        {
            case TrampolineType.Jump:
                trampolineBytes[0] = 0xE9;
                BitConverter.GetBytes((int)((long)target - (long)address - 5)).CopyTo(trampolineBytes, 1);
                break;
            case TrampolineType.JumpFar:
                trampolineBytes[0] = 0xFF;
                trampolineBytes[1] = 0x25;
                BitConverter.GetBytes((long)target).CopyTo(trampolineBytes, 6);
                break;
            case TrampolineType.Call:
                trampolineBytes[0] = 0xFF;
                trampolineBytes[1] = 0x15;
                trampolineBytes[2] = 0x02;
                //00 00 00
                trampolineBytes[6] = 0xEB;
                trampolineBytes[7] = 0x08;
                BitConverter.GetBytes((long)target).CopyTo(trampolineBytes, 8);
                break;
            default:
                throw new("Achievement unlocked: How Did We Get Here?");
        }

        // Fill the rest with nops
        for (int i = type switch
             {
                 TrampolineType.Jump => 5,
                 TrampolineType.JumpFar => 14,
                 TrampolineType.Call => 16,
                 _ => throw new("Achievement unlocked: How Did We Get Here?")
             };
             i < trampolineBytes.Length;
             i++)
        {
            trampolineBytes[i] = 0x90;
        }

        return trampolineBytes;
    }

    public byte[] CalculateTrampoline(string address, nuint target, TrampolineType type, int replaceCount)
    {
        byte[] trampolineBytes = new byte[replaceCount];

        nuint theAddress = FollowMultiLevelPointer(address);
        switch (type)
        {
            case TrampolineType.Jump:
                trampolineBytes[0] = 0xE9;
                BitConverter.GetBytes((int)((long)target - (long)theAddress - 5)).CopyTo(trampolineBytes, 1);
                break;
            case TrampolineType.JumpFar:
                trampolineBytes[0] = 0xFF;
                trampolineBytes[1] = 0x25;
                BitConverter.GetBytes((long)target).CopyTo(trampolineBytes, 6);
                break;
            case TrampolineType.Call:
                trampolineBytes[0] = 0xFF;
                trampolineBytes[1] = 0x15;
                trampolineBytes[2] = 0x02;
                //00 00 00
                trampolineBytes[6] = 0xEB;
                trampolineBytes[7] = 0x08;
                BitConverter.GetBytes((long)target).CopyTo(trampolineBytes, 8);
                break;
            default:
                throw new("Achievement unlocked: How Did We Get Here?");
        }

        // Fill the rest with nops
        for (int i = type switch
             {
                 TrampolineType.Jump => 5,
                 TrampolineType.JumpFar => 14,
                 TrampolineType.Call => 16,
                 _ => throw new("Achievement unlocked: How Did We Get Here?")
             };
             i < trampolineBytes.Length;
             i++)
        {
            trampolineBytes[i] = 0x90;
        }

        return trampolineBytes;
    }

    private nuint FindFreeBlockForRegion(nuint baseAddress, uint size)
    {
        nuint minAddress = nuint.Subtract(baseAddress, 0x70000000);
        nuint maxAddress = nuint.Add(baseAddress, 0x70000000);

        nuint ret = nuint.Zero;

        GetSystemInfo(out SYSTEM_INFO si);

        if (MProc.Is64Bit)
        {
            if ((long)minAddress > (long)si.MaximumApplicationAddress ||
                (long)minAddress < (long)si.MinimumApplicationAddress)
                minAddress = si.MinimumApplicationAddress;

            if ((long)maxAddress < (long)si.MinimumApplicationAddress ||
                (long)maxAddress > (long)si.MaximumApplicationAddress)
                maxAddress = si.MaximumApplicationAddress;
        }
        else
        {
            minAddress = si.MinimumApplicationAddress;
            maxAddress = si.MaximumApplicationAddress;
        }

        nuint current = minAddress;

        while (VirtualQueryEx(MProc.Handle, current, out MEMORY_BASIC_INFORMATION mbi).ToUInt64() != 0)
        {
            if ((long)mbi.BaseAddress > (long)maxAddress)
                return nuint.Zero; // No memory found, let windows handle

            if (mbi.State == MemFree && mbi.RegionSize > size)
            {
                nuint tmpAddress;
                if ((long)mbi.BaseAddress % si.AllocationGranularity > 0)
                {
                    // The whole size can not be used
                    tmpAddress = mbi.BaseAddress;
                    int offset = (int)(si.AllocationGranularity -
                                       ((long)tmpAddress % si.AllocationGranularity));

                    // Check if there is enough left
                    if (mbi.RegionSize - offset >= size)
                    {
                        // yup there is enough
                        tmpAddress = nuint.Add(tmpAddress, offset);

                        if ((long)tmpAddress < (long)baseAddress)
                        {
                            tmpAddress = nuint.Add(tmpAddress, (int)(mbi.RegionSize - offset - size));

                            if ((long)tmpAddress > (long)baseAddress)
                                tmpAddress = baseAddress;

                            // decrease tmpAddress until its aligned properly
                            tmpAddress = nuint.Subtract(tmpAddress,
                                (int)((long)tmpAddress % si.AllocationGranularity));
                        }

                        // if the difference is closer then use that
                        if (Math.Abs((long)tmpAddress - (long)baseAddress) <
                            Math.Abs((long)ret - (long)baseAddress))
                            ret = tmpAddress;
                    }
                }
                else
                {
                    tmpAddress = mbi.BaseAddress;

                    if ((long)tmpAddress < (long)baseAddress) // try to get it the closest possible 
                        // (so to the end of the region - size and
                        // aligned by system allocation granularity)
                    {
                        tmpAddress = nuint.Add(tmpAddress, (int)(mbi.RegionSize - size));

                        if ((long)tmpAddress > (long)baseAddress)
                            tmpAddress = baseAddress;

                        // decrease until aligned properly
                        tmpAddress =
                            nuint.Subtract(tmpAddress, (int)((long)tmpAddress % si.AllocationGranularity));
                    }

                    if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                        ret = tmpAddress;
                }
            }

            if (mbi.RegionSize % si.AllocationGranularity > 0)
                mbi.RegionSize += si.AllocationGranularity - (mbi.RegionSize % si.AllocationGranularity);

            nuint previous = current;
            current = new(mbi.BaseAddress + (nuint)mbi.RegionSize);

            if ((long)current >= (long)maxAddress)
                return ret;

            if ((long)previous >= (long)current)
                return ret; // Overflow
        }

        return ret;
    }
#endif

    public static void SuspendProcess(int pid)
    {
        Process process = Process.GetProcessById(pid);

        if (process.ProcessName == string.Empty)
            return;

        foreach (ProcessThread pT in process.Threads)
        {
            nint pOpenThread = OpenThread(ThreadAccess.SuspendResume, false, (uint)pT.Id);
            if (pOpenThread == nint.Zero)
                continue;

            _ = SuspendThread(pOpenThread);
            _ = CloseHandle(pOpenThread);
        }
    }

    public static void ResumeProcess(int pid)
    {
        Process process = Process.GetProcessById(pid);
        if (process.ProcessName == string.Empty)
            return;

        foreach (ProcessThread pT in process.Threads)
        {
            nint pOpenThread = OpenThread(ThreadAccess.SuspendResume, false, (uint)pT.Id);
            if (pOpenThread == nint.Zero)
                continue;

            int suspendCount;
            do
            {
                suspendCount = ResumeThread(pOpenThread);
            } while (suspendCount > 0);

            _ = CloseHandle(pOpenThread);
        }
    }

#if WINXP
#else
    public async Task PutTaskDelay(int delay)
    {
        await Task.Delay(delay);
    }
#endif

    public void AppendAllBytes(string path, byte[] bytes)
    {
        using FileStream stream = new(path, FileMode.Append);
        stream.Write(bytes, 0, bytes.Length);
    }

    public byte[] FileToBytes(string path, bool dontDelete = false)
    {
        byte[] newArray = File.ReadAllBytes(path);
        if (!dontDelete)
            File.Delete(path);
        return newArray;
    }

    public string MSize()
    {
        return MProc.Is64Bit ? "x16" : "x8";
    }

    /// <summary>
    /// Convert a byte array to hex values in a string.
    /// </summary>
    /// <param name="ba">your byte array to convert</param>
    /// <returns></returns>
    public static string ByteArrayToHexString(byte[] ba)
    {
        StringBuilder hex = new(ba.Length * 2);
        int i = 1;
        foreach (byte b in ba)
        {
            if (i == 16)
            {
                hex.Append($"{b:x2}{Environment.NewLine}");
                i = 0;
            }
            else
                hex.Append($"{b:x2} ");

            i++;
        }

        return hex.ToString().ToUpper();
    }

    public static string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
        {
            hex.Append($"{b:x2} ");
        }

        return hex.ToString();
    }

    public ulong GetMinAddress()
    {
        GetSystemInfo(out SYSTEM_INFO si);
        return si.MinimumApplicationAddress;
    }

    /// <summary>
    /// Dump memory page by page to a dump.dmp file. Can be used with Cheat Engine.
    /// </summary>
    public bool DumpMemory(string file = "dump.dmp")
    {
        Debug.Write("[DEBUG] memory dump starting... (" + DateTime.Now.ToString("h:mm:ss tt") + ")" +
                    Environment.NewLine);
        GetSystemInfo(out SYSTEM_INFO sysInfo);

        nuint procMinAddress = sysInfo.MinimumApplicationAddress;

        // saving the values as long ints so I won't have to do a lot of casts later
        long procMinAddressL = (long)procMinAddress; //(Int64)procs.MainModule.BaseAddress;
        long procMaxAddressL = MProc.Process.VirtualMemorySize64 + procMinAddressL;

        //int arrLength = 0;
        if (File.Exists(file))
            File.Delete(file);


        while (procMinAddressL < procMaxAddressL)
        {
            VirtualQueryEx(MProc.Handle, procMinAddress, out MEMORY_BASIC_INFORMATION memInfo);
            byte[] buffer = new byte[memInfo.RegionSize];
            nuint test = (nuint)memInfo.RegionSize;
            nuint test2 = memInfo.BaseAddress;

            ReadProcessMemory(MProc.Handle, test2, buffer, test, IntPtr.Zero);

            AppendAllBytes(file, buffer); //due to memory limits, we have to dump it then store it in an array.
            //arrLength += buffer.Length;

            procMinAddressL += memInfo.RegionSize;
            procMinAddress = new((ulong)procMinAddressL);
        }


        Debug.Write("[DEBUG] memory dump completed. Saving dump file to " + file + ". (" +
                    DateTime.Now.ToString("h:mm:ss tt") + ")" + Environment.NewLine);
        return true;
    }

    /// <summary>
    /// get a list of available threads in opened process
    /// </summary>
    public void GetThreads()
    {
        if (MProc.Process == null)
        {
            Debug.WriteLine("mProc.Process is null so GetThreads failed.");
            return;
        }

        foreach (ProcessThread thd in MProc.Process.Threads)
        {
            Debug.WriteLine("ID:" + thd.Id + " State:" + thd.ThreadState + " Address:" + thd.StartAddress +
                            " Priority:" + thd.PriorityLevel);
        }
    }

    /// <summary>
    /// Get thread base address by ID. Provided by github.com/osadrac
    /// </summary>
    /// <param name="threadId"></param>
    /// <returns></returns>
    /// <exception cref="Win32Exception"></exception>
    public static nint GetThreadStartAddress(int threadId)
    {
        nint hThread = OpenThread(ThreadAccess.QueryInformation, false, (uint)threadId);
        if (hThread == nint.Zero)
            throw new Win32Exception();
        nint buf = Marshal.AllocHGlobal(nint.Size);
        try
        {
            int result = NtQueryInformationThread(hThread,
                ThreadInfoClass.ThreadQuerySetWin32StartAddress,
                buf, nint.Size, nint.Zero);
            if (result != 0)
                throw new Win32Exception($"NtQueryInformationThread failed; NTSTATUS = {result:X8}");
            return Marshal.ReadIntPtr(buf);
        }
        finally
        {
            _ = CloseHandle(hThread);
            Marshal.FreeHGlobal(buf);
        }
    }

    /// <summary>
    /// suspend a thread by ID
    /// </summary>
    /// <param name="threadId">the thread you wish to suspend by ID</param>
    /// <returns></returns>
    public bool SuspendThreadById(int threadId)
    {
        foreach (ProcessThread thd in MProc.Process.Threads)
        {
            if (thd.Id != threadId)
                continue;

            Debug.WriteLine("Found thread " + threadId);

            nint threadHandle = OpenThread(ThreadAccess.SuspendResume, false, (uint)threadId);

            if (threadHandle == nint.Zero)
                break;

            if (SuspendThread(threadHandle) == -1)
            {
                Debug.WriteLine("Thread failed to suspend");
                _ = CloseHandle(threadHandle);
                break;
            }

            Debug.WriteLine("Thread suspended!");
            _ = CloseHandle(threadHandle);
            return true;
        }

        return false;
    }
}