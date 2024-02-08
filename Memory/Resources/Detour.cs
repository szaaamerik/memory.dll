#nullable enable
using System;
using System.Linq;

namespace Memory.Resources;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
public class Detour
{
    public bool Setup(nuint address, byte[] originalBytes, byte[] newBytes, int replaceCount, uint varOffset = 0)
    {
        if (replaceCount < 5)
        {
            throw new ArgumentOutOfRangeException(nameof(replaceCount));
        }

        if (originalBytes.Length != replaceCount)
        {
            throw new ArgumentException("The length of original bytes should be equal to the replace count", nameof(originalBytes));
        }
        
        if (!Mem.IsProcessRunning(Mem.DefaultInstance.MProc.ProcessId))
        {
            return false;
        }

        DetourAddr = address;
        _realOriginalBytes = Mem.DefaultInstance.ReadArrayMemory<byte>(DetourAddr, replaceCount);
        if (originalBytes != null && _realOriginalBytes.Where((t, i) => t != originalBytes[i]).Any())
        {
            return false;
        }

        Mem.DefaultInstance.CreateDetour(DetourAddr, newBytes, replaceCount);
        VariableAddress = AllocatedAddress + (UIntPtr)newBytes.Length + varOffset + 5;
        _newBytes = Mem.DefaultInstance.ReadArrayMemory<byte>(DetourAddr, replaceCount);
        return true;
    }
    
    public void Destroy()
    {
        if (AllocatedAddress == UIntPtr.Zero || _realOriginalBytes == null)
        {
            return;
        }
        
        UnHook();
        Imps.VirtualFreeEx(Mem.DefaultInstance.MProc.Process!.Handle, AllocatedAddress, 0, Imps.MemRelease);
    }
    
    public void Toggle()
    {
        if (!IsSetup || _realOriginalBytes == null)
        {
            return;
        }
        
        var currentBytes = Mem.DefaultInstance.ReadArrayMemory<byte>(DetourAddr, _realOriginalBytes.Length);
        if (currentBytes.SequenceEqual(_realOriginalBytes))
        {
            Hook();
        }
        else
        {
            UnHook();
        }

        IsHooked = !IsHooked;
    }

    public void Hook() => Mem.DefaultInstance.WriteArrayMemory(DetourAddr, _newBytes);
    public void UnHook() => Mem.DefaultInstance.WriteArrayMemory(DetourAddr, _realOriginalBytes);
        
    public bool IsHooked { get; private set; }
    public bool IsSetup { get; private set; }
    public UIntPtr VariableAddress { get; private set; }
    public UIntPtr AllocatedAddress { get; private set; }
    public UIntPtr DetourAddr { get; private set; }
    private byte[]? _realOriginalBytes, _newBytes;
}