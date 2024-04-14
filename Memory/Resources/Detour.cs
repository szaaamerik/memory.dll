using System;
using System.Linq;
using System.Text;

namespace Memory.Resources;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
public class Detour
{
    public bool Setup(nuint address, Mem.DetourType type, byte[] originalBytes, byte[] newBytes, int replaceCount, uint varOffset = 0)
    {
        if (IsSetup)
        {
            return true;
        }
        
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

        AllocatedAddress = type switch
        {
            Mem.DetourType.Jump => Mem.DefaultInstance.CreateDetour(address, newBytes, replaceCount),
            Mem.DetourType.JumpFar => Mem.DefaultInstance.CreateFarDetour(address, newBytes, replaceCount),
            Mem.DetourType.Call => Mem.DefaultInstance.CreateCallDetour(address, newBytes, replaceCount),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        varOffset += type switch
        {
            Mem.DetourType.Jump => 5,
            Mem.DetourType.JumpFar => 14,
            Mem.DetourType.Call => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        VariableAddress = AllocatedAddress + (UIntPtr)newBytes.Length + varOffset;
        _newBytes = Mem.DefaultInstance.ReadArrayMemory<byte>(DetourAddr, replaceCount);
        return IsSetup = true;
    }
    
    public void Destroy()
    {
        if (!IsSetup)
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

    public void Hook() => Mem.DefaultInstance.WriteArrayMemory(DetourAddr, _newBytes ?? Array.Empty<byte>());
    public void UnHook() => Mem.DefaultInstance.WriteArrayMemory(DetourAddr, _realOriginalBytes ?? Array.Empty<byte>());
        
    public void UpdateVariable<T>(T value, uint varOffset = 0) where T : unmanaged
    {
        if (VariableAddress == UIntPtr.Zero || !IsSetup)
        {
            return;
        }
        
        Mem.DefaultInstance.WriteMemory(VariableAddress + varOffset, value);
    }
    
    public void UpdateVariable<T>(T[] value, uint varOffset = 0) where T : unmanaged
    {
        if (VariableAddress == UIntPtr.Zero || !IsSetup)
        {
            return;
        }
        
        Mem.DefaultInstance.WriteArrayMemory(VariableAddress + varOffset, value);
    }
    
    public T ReadVariable<T>(uint varOffset = 0) where T : unmanaged
    {
        return VariableAddress == UIntPtr.Zero || !IsSetup
            ? new T()
            : Mem.DefaultInstance.ReadMemory<T>(VariableAddress + varOffset);
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder(512);
        sb.Append("IsHooked: ").AppendLine(IsHooked.ToString());
        sb.Append("IsSetup: ").AppendLine(IsSetup.ToString());
        sb.Append("Detour Addr: ").AppendLine(DetourAddr.ToString("X"));
        sb.Append("Allocated Addr: ").AppendLine(AllocatedAddress.ToString("X"));
        sb.Append("Variable Addr: ").AppendLine(VariableAddress.ToString("X"));

        if (_realOriginalBytes != null)
        {
            sb.Append("Original Bytes: ").AppendLine(BitConverter.ToString(_realOriginalBytes).Replace("-", " "));
        }

        if (_newBytes != null)
        {
            sb.Append("New Bytes: ").AppendLine(BitConverter.ToString(_newBytes).Replace("-", " "));
        }
        
        return sb.ToString();
    }

    
    public bool IsHooked { get; private set; }
    public bool IsSetup { get; private set; }
    public UIntPtr VariableAddress { get; private set; }
    public UIntPtr AllocatedAddress { get; private set; }
    public UIntPtr DetourAddr { get; private set; }
    private byte[]? _realOriginalBytes, _newBytes;
}