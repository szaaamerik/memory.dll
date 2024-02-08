using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public Task<IEnumerable<nuint>> AoBScan(string search, bool writable = false, bool executable = true)
    {
        return AoBScan(0, long.MaxValue, search, writable, executable);
    }

    public Task<IEnumerable<nuint>> AoBScan(string search, bool readable, bool writable, bool executable)
    {
        return AoBScan(0, long.MaxValue, search, readable, writable, executable, false);
    }
    
    public Task<IEnumerable<nuint>> AoBScan(long start, long end, string search, bool writable = false, bool executable = true, bool mapped = false)
    {
        return AoBScan(start, end, search, false, writable, executable, mapped);
    }

    public Task<IEnumerable<nuint>> AoBScan(long start, long end, string search, bool readable, bool writable, bool executable, bool mapped)
    {
        return Task.Run(() =>
        {
            if (!IsProcessRunning(MProc.ProcessId))
            {
                return Array.Empty<nuint>();
            }

            var aobPattern = Utils.ParseSig(search, out var mask);
            var memRegionList = GetMemoryRegions(start, end, mapped, readable, writable, executable);
            var bagResult = new ConcurrentBag<nuint>();

            Parallel.ForEach(memRegionList, item =>
            {
                var compareResults = CompareScan(item, aobPattern, mask);
                foreach (var result in compareResults)
                {
                    bagResult.Add(result);
                }
            });

            return bagResult.OrderBy(c => c).AsEnumerable();
        });
    }

    private IEnumerable<MemoryRegionResult> GetMemoryRegions(long start, long end, bool mapped, bool readable, bool writable, bool executable)
    {
        var memRegionList = new List<MemoryRegionResult>();
        GetSystemInfo(out var sysInfo);
        var procMinAddress = sysInfo.MinimumApplicationAddress;
        var procMaxAddress = sysInfo.MaximumApplicationAddress;

        start = Math.Max((long)procMinAddress.ToUInt64(), start);
        end = Math.Min((long)procMaxAddress.ToUInt64(), end);

        var currentBaseAddress = (nuint)start;

        while (VirtualQueryEx(MProc.Handle, currentBaseAddress, out var memInfo).ToUInt64() != 0 &&
               currentBaseAddress.ToUInt64() < (ulong)end &&
               currentBaseAddress.ToUInt64() + (ulong)memInfo.RegionSize > currentBaseAddress.ToUInt64())
        {
            var isValid = IsValidMemoryRegion(memInfo, procMaxAddress, mapped, readable, writable, executable);

            if (!isValid)
            {
                currentBaseAddress = new UIntPtr(memInfo.BaseAddress.ToUInt64() + (ulong)memInfo.RegionSize);
                continue;
            }

            var memRegion = CreateMemoryRegion(memInfo);
            MergeOverlappingRegions(memRegionList, memRegion);
        }

        return memRegionList;
    }

    private static bool IsValidMemoryRegion(MemoryBasicInformation memInfo, UIntPtr procMaxAddress, bool mapped, bool readable, bool writable, bool executable)
    {
        var isValid = memInfo.State == MemCommit;
        isValid &= memInfo.BaseAddress.ToUInt64() < procMaxAddress.ToUInt64();
        isValid &= (memInfo.Protect & Guard) == 0;
        isValid &= (memInfo.Protect & Noaccess) == 0;
        isValid &= memInfo.Type is MemPrivate or MemImage;

        if (mapped)
        {
            isValid &= memInfo.Type == MemMapped;
        }

        if (!isValid)
        {
            return false;
        }
        
        var isReadable = (memInfo.Protect & Readonly) > 0;
        var isWritable = (memInfo.Protect & Readwrite) > 0 ||
                         (memInfo.Protect & Writecopy) > 0 ||
                         (memInfo.Protect & ExecuteReadwrite) > 0 ||
                         (memInfo.Protect & ExecuteWritecopy) > 0;
        var isExecutable = (memInfo.Protect & Execute) > 0 ||
                           (memInfo.Protect & ExecuteRead) > 0 ||
                           (memInfo.Protect & ExecuteReadwrite) > 0 ||
                           (memInfo.Protect & ExecuteWritecopy) > 0;

        isReadable &= readable;
        isWritable &= writable;
        isExecutable &= executable;

        isValid &= isReadable || isWritable || isExecutable;

        return isValid;
    }

    private static MemoryRegionResult CreateMemoryRegion(MemoryBasicInformation memInfo)
    {
        return new MemoryRegionResult
        {
            CurrentBaseAddress = memInfo.BaseAddress,
            RegionSize = memInfo.RegionSize,
            RegionBase = memInfo.BaseAddress
        };
    }

    private static void MergeOverlappingRegions(List<MemoryRegionResult> memRegionList, MemoryRegionResult memRegion)
    {
        if (memRegionList.Count > 0)
        {
            var previousRegion = memRegionList[^1];

            if ((long)previousRegion.RegionBase + previousRegion.RegionSize == (long)memRegion.CurrentBaseAddress)
            {
                memRegionList[^1] = previousRegion with { RegionSize = previousRegion.RegionSize + memRegion.RegionSize };
                return;
            }
        }

        memRegionList.Add(memRegion);
    }

    private IEnumerable<nuint> CompareScan(MemoryRegionResult item, byte[] aobPattern, byte[] mask)
    {
        if (mask.Length != aobPattern.Length)
        {
            throw new ArgumentException(null, $"{nameof(aobPattern)}.Length != {nameof(mask)}.Length");
        }

        var buffer = Marshal.AllocHGlobal((int)item.RegionSize);

        if (!ReadProcessMemory(MProc.Handle, item.CurrentBaseAddress, buffer, (nuint)item.RegionSize, out var bytesRead))
        {
            return Array.Empty<nuint>();
        }

        var result = 0 - aobPattern.Length;
        var ret = new List<nuint>();

        unsafe
        {
            do
            {
                result = FindPattern((byte*)buffer.ToPointer(), (int)bytesRead, aobPattern, mask, result + aobPattern.Length);

                if (result >= 0)
                {
                    ret.Add(item.CurrentBaseAddress + (uint)result);
                }
            } while (result != -1);
        }

        Marshal.FreeHGlobal(buffer);
        return ret.ToArray();
    }

    private static unsafe int FindPattern(byte* body, int bodyLength, IReadOnlyList<byte> pattern, IReadOnlyList<byte> masks, int start = 0)
    {
        var foundIndex = -1;

        if (bodyLength <= 0 || pattern.Count <= 0 || start > bodyLength - pattern.Count || pattern.Count > bodyLength)
        {
            return foundIndex;
        }

        for (var index = start; index <= bodyLength - pattern.Count; index++)
        {
            if ((body[index] & masks[0]) != (pattern[0] & masks[0])) continue;

            var match = true;

            for (var index2 = pattern.Count - 1; index2 >= 1; index2--)
            {
                if ((body[index + index2] & masks[index2]) == (pattern[index2] & masks[index2])) continue;

                match = false;
                break;
            }

            if (!match)
            {
                continue;
            }

            foundIndex = index;
            break;
        }

        return foundIndex;
    }
}