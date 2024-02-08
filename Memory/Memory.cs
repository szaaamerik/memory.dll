using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public readonly Proc MProc = new();

    public nuint VirtualQueryEx(nint hProcess, nuint lpAddress, out MemoryBasicInformation lpBuffer)
    {
        nuint retVal;

        if (MProc.Is64Bit)
        {
            MemoryBasicInformation64 tmp64 = new();
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

        MemoryBasicInformation32 tmp32 = new();
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

    public enum OpenProcessResults
    {
        InvalidArgument = 0,
        NotResponding,
        FailedOpeningHandle,
        Success,
    }

    public OpenProcessResults OpenProcess(int pid)
    {
        if (pid <= 0)
        {
            return OpenProcessResults.InvalidArgument;
        }
        
        MProc.Process = Process.GetProcessById(pid);
        if (MProc.Process is { Responding: false })
        {
            return OpenProcessResults.NotResponding;
        }

        const int processAllAccess = 0x1F0FFF;
        MProc.Handle = Imps.OpenProcess(processAllAccess, false, pid);
        if (MProc.Handle == nint.Zero)
        {
            return OpenProcessResults.FailedOpeningHandle;
        }

        MProc.Is64Bit = IsWow64Process(MProc.Handle, out var retVal) && !retVal;
        return OpenProcessResults.Success;
    }

    public OpenProcessResults OpenProcess(string proc)
    {
        return string.IsNullOrWhiteSpace(proc) ? OpenProcessResults.InvalidArgument : OpenProcess(GetProcIdFromName(proc));
    }
    
    public static int GetProcIdFromName(string name)
    {
        var processlist = Process.GetProcesses();
        if (name.ToLower().Contains(".exe"))
        {
            name = name.Replace(".exe", "");
        }

        return (from theProcess in processlist
            where theProcess.ProcessName.Equals(name, StringComparison.CurrentCultureIgnoreCase)
            select theProcess.Id).FirstOrDefault();
    }

    public bool ChangeProtection(nuint address, MemoryProtection newProtection, out MemoryProtection oldProtection)
    {
        if (address != nuint.Zero && MProc.Handle != nint.Zero)
        {
            var size = MProc.Is64Bit ? 8 : 4;
            return VirtualProtectEx(MProc.Handle, address, size, newProtection, out oldProtection);
        }

        oldProtection = default;
        return false;
    }
    
    public nuint FollowMultiLevelPointer(nuint address, IEnumerable<int> offsets)
    {
        var enumerable = offsets as int[] ?? offsets.ToArray();
        if (!enumerable.Any())
        {
            return 0;
        }
        
        var finalAddress = address;
        foreach (var offset in enumerable)
        {
            finalAddress = ReadMemory<nuint>(finalAddress);
            finalAddress += (nuint)(offset >= 0 ? offset : -offset);
        }
        
        return finalAddress;
    }

    public nuint CreateDetour(nuint address, byte[] newBytes, int replaceCount, byte[] varBytes = null!,
        int varOffset = 0, uint size = 0x1000, bool makeDetour = true)
    {
        if (replaceCount < 5)
        {
            throw new ArgumentOutOfRangeException(nameof(replaceCount));
        }
        
        var caveAddress = nuint.Zero;
        var preferred = address;

        const int tryCount = 25;
        for (var i = 0; i < tryCount && caveAddress == nuint.Zero; i++)
        {
            var allocAddress = FindFreeBlockForRegion(preferred, size);
            caveAddress = VirtualAllocEx(MProc.Handle, allocAddress, size, MemCommit | MemReserve, ExecuteReadwrite);

            if (caveAddress != nuint.Zero)
            {
                continue;
            }
            
            const int preferredOffset = 0x10000;
            preferred = nuint.Add(preferred, preferredOffset);
        }

        var nopsNeeded = replaceCount > 5 ? replaceCount - 5 : 0;
        var offset = (int)((long)caveAddress - (long)address - 5);

        var jmpBytes = new byte[5 + nopsNeeded];
        jmpBytes[0] = 0xE9;
        BitConverter.GetBytes(offset).CopyTo(jmpBytes, 1);

        for (var i = 5; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        var caveBytes = new byte[5 + newBytes.Length];
        offset = (int)((long)address + jmpBytes.Length - ((long)caveAddress + newBytes.Length) - 5);
        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xE9;
        BitConverter.GetBytes(offset).CopyTo(caveBytes, newBytes.Length + 1);
        WriteArrayMemory(caveAddress, caveBytes);

        if (makeDetour)
        {
            WriteArrayMemory(address, jmpBytes);
        }

        if (varBytes != null!)
        {
            WriteArrayMemory(caveAddress + (nuint)caveBytes.Length + (nuint)varOffset, varBytes);
        }

        return caveAddress;
    }

    public nuint CreateFarDetour(nuint address, byte[] newBytes, int replaceCount, byte[] varBytes = null!,
        int varOffset = 0, uint size = 0x1000, bool makeDetour = true)
    {
        if (replaceCount < 14)
        {
            throw new ArgumentOutOfRangeException(nameof(replaceCount));
        }

        var caveAddress = VirtualAllocEx(MProc.Handle, nuint.Zero, size, MemCommit | MemReserve, ExecuteReadwrite);
        var nopsNeeded = replaceCount > 14 ? replaceCount - 14 : 0;
        var jmpBytes = new byte[14 + nopsNeeded];
        jmpBytes[0] = 0xFF;
        jmpBytes[1] = 0x25;
        BitConverter.GetBytes((long)caveAddress).CopyTo(jmpBytes, 6);

        for (var i = 14; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        var caveBytes = new byte[newBytes.Length + 14];
        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xFF;
        caveBytes[newBytes.Length + 1] = 0x25;
        BitConverter.GetBytes((long)address + jmpBytes.Length).CopyTo(caveBytes, newBytes.Length + 6);

        WriteArrayMemory(caveAddress, caveBytes);
        if (makeDetour)
        {
            WriteArrayMemory(address, jmpBytes);
        }

        if (varBytes != null!)
        {
            WriteArrayMemory(caveAddress + (nuint)caveBytes.Length + (nuint)varOffset, varBytes);
        }
        
        return caveAddress;
    }

    public nuint CreateCallDetour(nuint address, byte[] newBytes, int replaceCount,
        byte[] varBytes = null!, int varOffset = 0, int size = 0x1000, bool makeDetour = true)
    {
        if (replaceCount < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(replaceCount));
        }
        
        var caveAddress = VirtualAllocEx(MProc.Handle, nuint.Zero, (uint)size, 0x1000 | 0x2000, 0x40);
        var nopsNeeded = replaceCount > 16 ? replaceCount - 16 : 0;
        var jmpBytes = new byte[16 + nopsNeeded];
        jmpBytes[0] = 0xFF;
        jmpBytes[1] = 0x15;
        jmpBytes[2] = 0x02;
        jmpBytes[6] = 0xEB;
        jmpBytes[7] = 0x08;
        BitConverter.GetBytes((long)caveAddress).CopyTo(jmpBytes, 8);

        for (var i = 16; i < jmpBytes.Length; i++)
        {
            jmpBytes[i] = 0x90;
        }

        var caveBytes = new byte[newBytes.Length + 1];
        newBytes.CopyTo(caveBytes, 0);
        caveBytes[newBytes.Length] = 0xC3;

        WriteArrayMemory(caveAddress, caveBytes);
        if (makeDetour)
        {
            WriteArrayMemory(address, jmpBytes);
        }

        if (varBytes != null!)
        {
            WriteArrayMemory(caveAddress + (nuint)caveBytes.Length + (nuint)varOffset, varBytes);
        }

        return caveAddress;
    }

    public enum DetourType
    {
        Jump,
        JumpFar,
        Call,
        Unspecified
    }
    
    public static byte[] CalculateDetour(nuint address, nuint target, DetourType type, int replaceCount)
    {
        if (type == DetourType.Unspecified)
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }
        
        var detourBytes = new byte[replaceCount];
        switch (type)
        {
            case DetourType.Jump:
                detourBytes[0] = 0xE9;
                BitConverter.GetBytes((int)((long)target - (long)address - 5)).CopyTo(detourBytes, 1);
                break;
            case DetourType.JumpFar:
                detourBytes[0] = 0xFF;
                detourBytes[1] = 0x25;
                BitConverter.GetBytes((long)target).CopyTo(detourBytes, 6);
                break;
            case DetourType.Call:
                detourBytes[0] = 0xFF;
                detourBytes[1] = 0x15;
                detourBytes[2] = 0x02;
                detourBytes[6] = 0xEB;
                detourBytes[7] = 0x08;
                BitConverter.GetBytes((long)target).CopyTo(detourBytes, 8);
                break;
            default:
                throw new Exception("Achievement unlocked: How Did We Get Here?");
        }

        var nopsNeeded = type switch
        {
            DetourType.Jump => 5,
            DetourType.JumpFar => 14,
            DetourType.Call => 16,
            _ => throw new Exception("Achievement unlocked: How Did We Get Here?")
        };
        
        for (var i = nopsNeeded; i < detourBytes.Length; i++)
        {
            detourBytes[i] = 0x90;
        }

        return detourBytes;
    }
    
    private nuint FindFreeBlockForRegion(nuint baseAddress, uint size)
    {
        var minAddress = nuint.Subtract(baseAddress, 0x70000000);
        var maxAddress = nuint.Add(baseAddress, 0x70000000);

        var ret = nuint.Zero;

        GetSystemInfo(out var si);
        var min = si.MinimumApplicationAddress;
        var max = si.MaximumApplicationAddress;
        
        if (MProc.Is64Bit)
        {
            minAddress = (UIntPtr)Math.Max((long)min, Math.Min((long)minAddress, (long)max));
            maxAddress = (UIntPtr)Math.Min((long)max, Math.Max((long)maxAddress, (long)min));
        }
        else
        {
            minAddress = si.MinimumApplicationAddress;
            maxAddress = si.MaximumApplicationAddress;
        }

        var current = minAddress;
        var allocSize = si.AllocationGranularity;
        while (VirtualQueryEx(MProc.Handle, current, out var mbi).ToUInt64() != 0)
        {
            if ((long)mbi.BaseAddress > (long)maxAddress)
            {
                return nuint.Zero;
            }

            if (mbi.State == MemFree && mbi.RegionSize > size)
            {
                nuint tmpAddress;
                if ((long)mbi.BaseAddress % allocSize > 0)
                {
                    tmpAddress = mbi.BaseAddress;
                    var offset = (int)(allocSize - (long)tmpAddress % allocSize);

                    if (mbi.RegionSize - offset >= size)
                    {
                        tmpAddress = nuint.Add(tmpAddress, offset);

                        if ((long)tmpAddress < (long)baseAddress)
                        {
                            tmpAddress = nuint.Add(tmpAddress, (int)(mbi.RegionSize - offset - size));

                            if ((long)tmpAddress > (long)baseAddress)
                            {
                                tmpAddress = baseAddress;
                            }

                            tmpAddress = nuint.Subtract(tmpAddress, (int)((long)tmpAddress % allocSize));
                        }

                        if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                        {
                            ret = tmpAddress;
                        }
                    }
                }
                else
                {
                    tmpAddress = mbi.BaseAddress;

                    if ((long)tmpAddress < (long)baseAddress)
                    {
                        tmpAddress = nuint.Add(tmpAddress, (int)(mbi.RegionSize - size));

                        if ((long)tmpAddress > (long)baseAddress)
                        {
                            tmpAddress = baseAddress;
                        }

                        tmpAddress = nuint.Subtract(tmpAddress, (int)((long)tmpAddress % allocSize));
                    }

                    if (Math.Abs((long)tmpAddress - (long)baseAddress) < Math.Abs((long)ret - (long)baseAddress))
                    {
                        ret = tmpAddress;
                    }
                }
            }

            if (mbi.RegionSize % allocSize > 0)
            {
                mbi.RegionSize += allocSize - mbi.RegionSize % allocSize;
            }

            var previous = current;
            current = new UIntPtr(mbi.BaseAddress + (nuint)mbi.RegionSize);

            if ((long)current >= (long)maxAddress || (long)previous >= (long)current)
            {
                return ret;
            }
        }

        return ret;
    }
}