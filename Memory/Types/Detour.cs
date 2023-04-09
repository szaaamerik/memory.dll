using System;
using System.Diagnostics;
using System.Linq;

namespace Memory.Types;

public class Detour : MemoryObject
{
    private readonly byte[] _originalBytes, _realOriginalBytes, _newBytes;
    private readonly string _signature;
    private readonly int _signatureOffset;
    private nuint _signatureAddress;
    public readonly nuint Allocated;
    
    public bool IsHooked =>
        !M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length)
            .SequenceEqual(_originalBytes);
    
    public Detour(string address, byte[] ogBytes, byte[] newBytes, int replaceCount, Mem.DetourType detourType = Mem.DetourType.Unspecified, byte[] varBytes = null, string signature = "", int signatureOffset = 0, Action<nuint> mutate = null, Mem m = null) : base(address, "", m)
    {
        _originalBytes = ogBytes;
        _signature = signature;
        _signatureOffset = signatureOffset;
        _realOriginalBytes = M.ReadArrayMemory<byte>(AddressPtr, _originalBytes.Length);

        if (_originalBytes.Length != replaceCount)
            throw new("Original bytes length should be equal to the replace count");

        Allocated = detourType switch
        {
            Mem.DetourType.Jump => M.CreateDetour(address, newBytes, replaceCount, varBytes,  makeDetour: false),
            Mem.DetourType.JumpFar => M.CreateFarDetour(address, newBytes, replaceCount, varBytes, makeDetour: false),
            Mem.DetourType.Call => M.CreateCallDetour(address, newBytes, replaceCount, varBytes, 4, makeDetour: false),
            Mem.DetourType.Unspecified => replaceCount switch
            {
                < 5 => throw new("replaceCount must be at least 5"),
                < 14 => M.CreateDetour(address, newBytes, replaceCount, varBytes, makeDetour: false),
                < 16 => M.CreateFarDetour(address, newBytes, replaceCount, varBytes, makeDetour: false),
                _ => M.CreateCallDetour(address, newBytes, replaceCount, varBytes, 4, makeDetour: false)
            },
            _ => throw new("Invalid detour type")
        };
        _newBytes = detourType switch
        {
            < Mem.DetourType.Unspecified => M.CalculateDetour(address, Allocated, detourType, replaceCount),
            _ => replaceCount switch
            {
                < 5 => throw new("replaceCount must be at least 5"),
                < 14 => M.CalculateDetour(address, Allocated, Mem.DetourType.Jump, replaceCount),
                < 16 => M.CalculateDetour(address, Allocated, Mem.DetourType.JumpFar, replaceCount),
                _ => M.CalculateDetour(address, Allocated, Mem.DetourType.Call, replaceCount)
            }
        };

        mutate?.Invoke(Allocated);
        
        
        if (!BytesAtAddressAreCorrect && signature == "")
        {
            Debug.WriteLine($"{address} isn't correct, and no signature was provided to scan for.");
            return;
        }
        if (BytesAtAddressAreCorrect || signature == "") return;
        Debug.WriteLine($"{address} isn't correct, scanning for {signature}");


        _signatureAddress = M.ScanForSig(_signature, resultLimit: 1).FirstOrDefault();
        if (signatureOffset > 0)
            _signatureAddress += (uint) signatureOffset;
        else
            _signatureAddress -= (uint) -signatureOffset; //i think this is necessary because it's unsigned, but i'm not sure and too lazy to test

        Debug.WriteLine($"i found {_signatureAddress:X}!");
        UpdateAddressUsingSignature();
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

    public bool BytesAtAddressAreCorrect => _originalBytes.SequenceEqual(_realOriginalBytes);

    public void UpdateAddressUsingSignature()
    {
        _signatureAddress = M.ScanForSig(_signature, resultLimit: 1).FirstOrDefault();
        if (_signatureOffset > 0)
            _signatureAddress += (uint) _signatureOffset;
        else
            _signatureAddress -= (uint) _signatureOffset; //i think this is necessary because it's unsigned, but i'm not sure and too lazy to test

        AddressPtr = _signatureAddress;
    }
}