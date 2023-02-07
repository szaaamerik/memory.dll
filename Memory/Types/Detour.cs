using System;
using System.Linq;

namespace Memory.Types;

public class Detour : MemoryObject
{
    private readonly byte[] _originalBytes, _newBytes;
    private string _signature;
    public readonly nuint Allocated;

    public Detour(string address, byte[] ogBytes, byte[] newBytes, int replaceCount, byte[] varBytes = null,
        string sig = "", Action<nuint> mutate = null, Mem m = null) : base(address, "", m)
    {
        _originalBytes = ogBytes;
        _signature = sig;

        if (_originalBytes.Length != replaceCount)
            throw new("Original bytes length should be equal to the replace count");

        Allocated = replaceCount switch
        {
            < 5 => throw new("replaceCount must be at least 5"),
            < 14 => M.CreateDetour(address, newBytes, replaceCount, varBytes, makeDetour: false),
            < 16 => M.CreateFarDetour(address, newBytes, replaceCount, varBytes, makeDetour: false),
            _ => M.CreateCallDetour(address, newBytes, replaceCount, varBytes, 4, makeDetour: false)
        };
        _newBytes = M.CalculateDetour(address, Allocated, replaceCount switch
        {
            < 5 => throw new("replaceCount must be at least 5"),
            < 14 => Mem.DetourType.Jump,
            < 16 => Mem.DetourType.JumpFar,
            _ => Mem.DetourType.Call
        }, replaceCount);

        mutate?.Invoke(Allocated);
    }

    public Detour(string address, byte[] ogBytes, byte[] newBytes, int replaceCount,
        Mem.DetourType detourType, byte[] varBytes = null,
        string sig = "", Action<nuint> mutate = null, Mem m = null) : base(address, "", m)
    {
        _originalBytes = ogBytes;
        _signature = sig;

        if (_originalBytes.Length != replaceCount)
            throw new("Original bytes length should be equal to the replace count");

        Allocated = detourType switch
        {
            Mem.DetourType.Jump => M.CreateDetour(address, newBytes, replaceCount, varBytes,  makeDetour: false),
            Mem.DetourType.JumpFar => M.CreateFarDetour(address, newBytes, replaceCount, varBytes, makeDetour: false),
            Mem.DetourType.Call => M.CreateCallDetour(address, newBytes, replaceCount, varBytes, 4, makeDetour: false),
            _ => throw new("Invalid detour type")
        };
        _newBytes = M.CalculateDetour(address, Allocated, detourType, replaceCount);

        mutate?.Invoke(Allocated);
    }

    public void Hook() => M.WriteArrayMemory(AddressPtr, _newBytes);

    public void Unhook() => M.WriteArrayMemory(AddressPtr, _originalBytes);

    public void Toggle()
    {
        byte[] currentBytes = M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length);

        if (currentBytes.SequenceEqual(_originalBytes))
            Hook();
        else
            Unhook();
    }
}