using System;
using System.Linq;
using Memory.Types;

namespace Memory.Types;

public class Trampoline : MemoryObject
{
    private readonly byte[] _originalBytes, _newBytes;
    private string _signature;
    public readonly nuint Allocated;

    public Trampoline(string address, byte[] ogBytes, byte[] newBytes, int replaceCount, byte[] varBytes = null,
        string sig = "", Action<nuint> mutate = null, Mem m = null) : base(address, "", m)
    {
        _originalBytes = ogBytes;
        _signature = sig;

        if (_originalBytes.Length != replaceCount)
            throw new("Original bytes length should be equal to the replace count");

        Allocated = replaceCount switch
        {
            < 5 => throw new("replaceCount must be at least 5"),
            < 14 => M.CreateTrampoline(address, newBytes, replaceCount, varBytes, makeTrampoline: false),
            < 16 => M.CreateFarTrampoline(address, newBytes, replaceCount, varBytes, makeTrampoline: false),
            _ => M.CreateCallTrampoline(address, newBytes, replaceCount, varBytes, 4, makeTrampoline: false)
        };
        _newBytes = M.CalculateTrampoline(address, Allocated, replaceCount switch
        {
            < 5 => throw new("replaceCount must be at least 5"),
            < 14 => Mem.TrampolineType.Jump,
            < 16 => Mem.TrampolineType.JumpFar,
            _ => Mem.TrampolineType.Call
        }, replaceCount);

        mutate?.Invoke(Allocated);
    }

    public Trampoline(string address, byte[] ogBytes, byte[] newBytes, int replaceCount,
        Mem.TrampolineType trampolineType, byte[] varBytes = null,
        string sig = "", Action<nuint> mutate = null, Mem m = null) : base(address, "", m)
    {
        _originalBytes = ogBytes;
        _signature = sig;

        if (_originalBytes.Length != replaceCount)
            throw new("Original bytes length should be equal to the replace count");

        Allocated = trampolineType switch
        {
            Mem.TrampolineType.Jump => M.CreateTrampoline(address, newBytes, replaceCount, varBytes,  makeTrampoline: false),
            Mem.TrampolineType.JumpFar => M.CreateFarTrampoline(address, newBytes, replaceCount, varBytes, makeTrampoline: false),
            Mem.TrampolineType.Call => M.CreateCallTrampoline(address, newBytes, replaceCount, varBytes, 4, makeTrampoline: false),
            _ => throw new("Invalid trampoline type")
        };
        _newBytes = M.CalculateTrampoline(address, Allocated, trampolineType, replaceCount);

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