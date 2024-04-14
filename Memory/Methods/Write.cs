using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public unsafe bool WriteBit(nuint address, bool write, int bit, bool removeWriteProtection = true)
    {
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return false;
        }

        MemoryProtection oldMemProt = 0x00;

        byte buf;
        if (!ReadProcessMemory(MProc.Handle, address, &buf, 1, 0))
        {
            return false;
        }
        
        if (removeWriteProtection)
        {
            ChangeProtection(address, MemoryProtection.ExecuteReadWrite, out oldMemProt);
        }
        
        buf &= (byte)~(1 << bit);
        buf |= (byte)(Unsafe.As<bool, byte>(ref write) << bit);
        
        var ret = WriteProcessMemory(MProc.Handle, address, &buf, 1, nint.Zero);
        if (removeWriteProtection)
        {
            ChangeProtection(address, oldMemProt, out _);
        }
        
        return ret;
    }
        
    public unsafe bool WriteMemory<T>(nuint address, T write, bool removeWriteProtection = true) where T : unmanaged
    {
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return false;
        }
        
        MemoryProtection oldMemProt = 0x00;

        if (removeWriteProtection)
        {
            ChangeProtection(address, MemoryProtection.ExecuteReadWrite, out oldMemProt);
        }
            
        var ret = WriteProcessMemory(MProc.Handle, address, &write, (nuint)sizeof(T), nint.Zero);
        if (removeWriteProtection)
        {
            ChangeProtection(address, oldMemProt, out _);
        }
            
        return ret;
    }

    public bool WriteStringMemory(nuint address, string write, Encoding? stringEncoding = null, bool removeWriteProtection = true)
    {
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return false;
        }
        
        MemoryProtection oldMemProt = 0x00;
            
        if (removeWriteProtection)
        {
            ChangeProtection(address, MemoryProtection.ExecuteReadWrite, out oldMemProt);
        }
            
        var memory = stringEncoding == null ? Encoding.UTF8.GetBytes(write) : stringEncoding.GetBytes(write);
        var ret = WriteProcessMemory(MProc.Handle, address, memory, (nuint)memory.Length, nint.Zero);
        if (removeWriteProtection)
        {
            ChangeProtection(address, oldMemProt, out _);
        }

        return ret;
    }
        
    public unsafe bool WriteArrayMemory<T>(nuint address, T[] write, bool removeWriteProtection = true) where T : unmanaged
    {
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return false;
        }

        MemoryProtection oldMemProt = 0x00;
            
        var buffer = new byte[write.Length * sizeof(T)];
        fixed (T* ptr = write)
        {
            Marshal.Copy((nint)ptr, buffer, 0, buffer.Length);
        }

        if (removeWriteProtection)
        {
            ChangeProtection(address, MemoryProtection.ExecuteReadWrite, out oldMemProt);
        }
            
        var ret = WriteProcessMemory(MProc.Handle, address, buffer, (nuint)buffer.Length, nint.Zero);
        if (removeWriteProtection)
        {
            ChangeProtection(address, oldMemProt, out _);
        }
            
        return ret;
    }
}