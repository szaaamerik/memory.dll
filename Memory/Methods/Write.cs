using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    private readonly ConcurrentDictionary<nuint, CancellationTokenSource> _freezeTokenSrcs = new();
  
    public bool FreezeValue<T>(nuint address, T value, int speed = 25) where T : unmanaged
    {
        lock (_freezeTokenSrcs)
        {
            if (_freezeTokenSrcs.TryRemove(address, out var valueFound))
            {
                valueFound?.Cancel();
            }

            _freezeTokenSrcs.TryAdd(address, new CancellationTokenSource());
        }

        lock (_freezeTokenSrcs)
        {
            Task.Factory.StartNew(() =>
            {
                lock (_freezeTokenSrcs)
                {
                    while (!_freezeTokenSrcs[address].Token.IsCancellationRequested)
                    {
                        WriteMemory(address, value);
                        Thread.Sleep(speed);
                    }
                }
            }, _freezeTokenSrcs[address].Token);
        }

        return true;
    }

    public void UnfreezeValue(nuint address)
    {
        lock (_freezeTokenSrcs)
        {
            if (_freezeTokenSrcs.TryRemove(address, out var tokenSource))
            {
                tokenSource?.Cancel();
            }
        }
    }
    
    public unsafe bool WriteBit(nuint address, bool write, int bit, bool removeWriteProtection = true)
    {
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

    public bool WriteStringMemory(nuint address, string write, Encoding stringEncoding = null, bool removeWriteProtection = true)
    {
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