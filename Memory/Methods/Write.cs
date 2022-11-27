using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    ConcurrentDictionary<UIntPtr, CancellationTokenSource> _freezeTokenSrcs =
        new();

    /// <summary>
    /// Freeze a value to an address.
    /// </summary>
    /// <param name="address">Your address</param>
    /// <param name="value">Value to freeze</param>
    /// <param name="speed">The number of milliseconds to wait before setting the value again</param>
    /// <param name="file">ini file to read address from (OPTIONAL)</param>
    public bool FreezeValue<T>(string address, T value, int speed = 25) where T : unmanaged
    {
        CancellationTokenSource cts = new();
        UIntPtr addr = FollowMultiLevelPointer(address);

        lock (_freezeTokenSrcs)
        {
            if (_freezeTokenSrcs.ContainsKey(addr))
            {
                Debug.WriteLine("Changing Freezing Address " + address + " Value " + value.ToString());
                try
                {
                    _freezeTokenSrcs[addr].Cancel();
                    _freezeTokenSrcs.TryRemove(addr, out _);
                }
                catch
                {
                    Debug.WriteLine("ERROR: Avoided a crash. Address " + address + " was not frozen.");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Adding Freezing Address " + address + " Value " + value);
            }

            _freezeTokenSrcs.TryAdd(addr, cts);
        }

        Task.Factory.StartNew(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    WriteMemory(addr, value);
                    Thread.Sleep(speed);
                }
            },
            cts.Token);

        return true;
    }
        
    public bool FreezeValue<T>(UIntPtr address, T value, int speed = 25, string file = "") where T : unmanaged
    {
        CancellationTokenSource cts = new();

        lock (_freezeTokenSrcs)
        {
            if (_freezeTokenSrcs.ContainsKey(address))
            {
                Debug.WriteLine("Changing Freezing Address " + address + " Value " + value);
                try
                {
                    _freezeTokenSrcs[address].Cancel();
                    _freezeTokenSrcs.TryRemove(address, out _);
                }
                catch
                {
                    Debug.WriteLine("ERROR: Avoided a crash. Address " + address + " was not frozen.");
                    return false;
                }
            }
            else
            {
                Debug.WriteLine("Adding Freezing Address " + address + " Value " + value);
            }

            _freezeTokenSrcs.TryAdd(address, cts);
        }

        Task.Factory.StartNew(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    WriteMemory(address, value);
                    Thread.Sleep(speed);
                }
            },
            cts.Token);

        return true;
    }

    /// <summary>
    /// Unfreeze a frozen value at an address
    /// </summary>
    /// <param name="address">address where frozen value is stored</param>
    public void UnfreezeValue(string address)
    {
        UIntPtr addy = FollowMultiLevelPointer(address);
        Debug.WriteLine("Un-Freezing Address " + address);
        try
        {
            lock (_freezeTokenSrcs)
            {
                _freezeTokenSrcs[addy].Cancel();
                _freezeTokenSrcs.TryRemove(addy, out _);
            }
        }
        catch
        {
            Debug.WriteLine("ERROR: Address " + address + " was not frozen.");
        }
    }

    /// <summary>
    /// Unfreeze a frozen value at an address
    /// </summary>
    /// <param name="address">address where frozen value is stored</param>
    public void UnfreezeValue(UIntPtr address)
    {
        Debug.WriteLine("Un-Freezing Address " + address);
        try
        {
            lock (_freezeTokenSrcs)
            {
                _freezeTokenSrcs[address].Cancel();
                _freezeTokenSrcs.TryRemove(address, out _);
            }
        }
        catch
        {
            Debug.WriteLine("ERROR: Address " + address + " was not frozen.");
        }
    }


    /// <summary>
    /// Takes an array of 8 booleans and writes to a single byte
    /// </summary>
    /// <param name="code">address to write to</param>
    /// <param name="bits">Array of 8 booleans to write</param>
    /// <param name="file">path and name of ini file. (OPTIONAL)</param>
    public void WriteBits(string code, bool[] bits, string file = "")
    {
        if (bits.Length != 8)
            throw new ArgumentException("Not enough bits for a whole byte", nameof(bits));

        byte[] buf = new byte[1];

        UIntPtr theCode = FollowMultiLevelPointer(code);

        for (var i = 0; i < 8; i++)
        {
            if (bits[i])
                buf[0] |= (byte)(1 << i);
        }

        WriteProcessMemory(MProc.Handle, theCode, buf, (UIntPtr)1, IntPtr.Zero);
    }
        
    public unsafe bool WriteMemory<T>(string address, T write, bool removeWriteProtection = true) where T : unmanaged
    {
        UIntPtr addy = FollowMultiLevelPointer(address);
        MemoryProtection oldMemProt = 0x00;
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, addy, (IntPtr)8, MemoryProtection.ExecuteReadWrite, out oldMemProt);
            
        bool ret = WriteProcessMemory(MProc.Handle, addy, (long)&write, (UIntPtr)sizeof(T), IntPtr.Zero);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, addy, (IntPtr)8, oldMemProt, out _);
            
        return ret;
    }
    public unsafe bool WriteMemory<T>(UIntPtr address, T write, bool removeWriteProtection = true) where T : unmanaged
    {
        MemoryProtection oldMemProt = 0x00;
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, address, (IntPtr)8, MemoryProtection.ExecuteReadWrite, out oldMemProt);
            
        bool ret = WriteProcessMemory(MProc.Handle, address, (long)&write, (UIntPtr)sizeof(T), IntPtr.Zero);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, address, (IntPtr)8, oldMemProt, out _);
            
        return ret;
    }

    public bool WriteStringMemory(string address, string write, Encoding stringEncoding = null, bool removeWriteProtection = true)
    {
        UIntPtr addy = FollowMultiLevelPointer(address);
        MemoryProtection oldMemProt = 0x00;
            
        byte[] memory = stringEncoding == null
            ? Encoding.UTF8.GetBytes(write)
            : stringEncoding.GetBytes(write);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, addy, (IntPtr)8, MemoryProtection.ExecuteReadWrite, out oldMemProt);
            
        bool ret = WriteProcessMemory(MProc.Handle, addy, memory, (UIntPtr)memory.Length, IntPtr.Zero);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, addy, (IntPtr)8, oldMemProt, out _);

        return ret;
    }
    public bool WriteStringMemory(UIntPtr address, string write, Encoding stringEncoding = null, bool removeWriteProtection = true)
    {
        MemoryProtection oldMemProt = 0x00;
            
        byte[] memory = stringEncoding == null
            ? Encoding.UTF8.GetBytes(write)
            : stringEncoding.GetBytes(write);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, address, (IntPtr)8, MemoryProtection.ExecuteReadWrite, out oldMemProt);
            
        bool ret = WriteProcessMemory(MProc.Handle, address, memory, (UIntPtr)memory.Length, IntPtr.Zero);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, address, (IntPtr)8, oldMemProt, out _);

        return ret;
    }
        
    public unsafe bool WriteArrayMemory<T>(string address, T[] write, bool removeWriteProtection = true) where T : unmanaged
    {
        UIntPtr addy = FollowMultiLevelPointer(address);
        MemoryProtection oldMemProt = 0x00;
            
        byte[] buffer = new byte[write.Length * sizeof(T)];

        fixed (T* ptr = write)
        {
            Marshal.Copy((IntPtr)ptr, buffer, 0, buffer.Length);
        }
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, addy, (IntPtr)8, MemoryProtection.ExecuteReadWrite, out oldMemProt);
            
        bool ret = WriteProcessMemory(MProc.Handle, addy, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, addy, (IntPtr)8, oldMemProt, out _);
            
        return ret;
    }
    public unsafe bool WriteArrayMemory<T>(UIntPtr address, T[] write, bool removeWriteProtection = true) where T : unmanaged
    {
        MemoryProtection oldMemProt = 0x00;
            
        byte[] buffer = new byte[write.Length * sizeof(T)];

        fixed (T* ptr = write)
        {
            Marshal.Copy((IntPtr)ptr, buffer, 0, buffer.Length);
        }
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, address, (IntPtr)8, MemoryProtection.ExecuteReadWrite, out oldMemProt);
            
        bool ret = WriteProcessMemory(MProc.Handle, address, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
            
        if (removeWriteProtection)
            VirtualProtectEx(MProc.Handle, address, (IntPtr)8, oldMemProt, out _);
            
        return ret;
    }

    public bool WriteAnyMemory<T>(string address, T write, bool removeWriteProtection = true)
    {
        UIntPtr addy = FollowMultiLevelPointer(address);
        Type t = typeof(T);

        return true switch
        {
            true when t == typeof(float) => WriteMemory(addy, (float)(object)write, removeWriteProtection),
            true when t == typeof(Vector3) => WriteMemory(addy, (Vector3)(object)write, removeWriteProtection),
            true when t == typeof(Vector2) => WriteMemory(addy, (Vector2)(object)write, removeWriteProtection),
            true when t == typeof(int) => WriteMemory(addy, (int)(object)write, removeWriteProtection),
            true when t == typeof(byte) => WriteMemory(addy, (byte)(object)write, removeWriteProtection),
            true when t == typeof(bool) => WriteMemory(addy, (bool)(object)write, removeWriteProtection),
            true when t == typeof(string) => WriteStringMemory(addy, (string)(object)write, null, removeWriteProtection),
            true when t == typeof(Vector4) => WriteMemory(addy, (Vector4)(object)write, removeWriteProtection),
            true when t == typeof(Matrix4x4) => WriteMemory(addy, (Matrix4x4)(object)write, removeWriteProtection),
            true when t == typeof(Matrix3x2) => WriteMemory(addy, (Matrix3x2)(object)write, removeWriteProtection),
            true when t == typeof(short) => WriteMemory(addy, (short)(object)write, removeWriteProtection),
            true when t == typeof(long) => WriteMemory(addy, (long)(object)write, removeWriteProtection),
            true when t == typeof(double) => WriteMemory(addy, (double)(object)write, removeWriteProtection),
            true when t == typeof(ulong) => WriteMemory(addy, (ulong)(object)write, removeWriteProtection),
            true when t == typeof(uint) => WriteMemory(addy, (uint)(object)write, removeWriteProtection),
            true when t == typeof(ushort) => WriteMemory(addy, (ushort)(object)write, removeWriteProtection),
            true when t == typeof(sbyte) => WriteMemory(addy, (sbyte)(object)write, removeWriteProtection),
            true when t == typeof(char) => WriteMemory(addy, (char)(object)write, removeWriteProtection),
            true when t == typeof(decimal) => WriteMemory(addy, (decimal)(object)write, removeWriteProtection),
            true when t == typeof(IntPtr) => WriteMemory(addy, (IntPtr)(object)write, removeWriteProtection),
            true when t == typeof(UIntPtr) => WriteMemory(addy, (UIntPtr)(object)write, removeWriteProtection),
            _ => throw new("FUCK YOU!!!")
        };
    }

    public bool WriteAnyMemory<T>(UIntPtr address, T write, bool removeWriteProtection = true)
    {
        Type t = typeof(T);

        return true switch
        {
            true when t == typeof(float) => WriteMemory(address, (float)(object)write, removeWriteProtection),
            true when t == typeof(Vector3) => WriteMemory(address, (Vector3)(object)write, removeWriteProtection),
            true when t == typeof(Vector2) => WriteMemory(address, (Vector2)(object)write, removeWriteProtection),
            true when t == typeof(int) => WriteMemory(address, (int)(object)write, removeWriteProtection),
            true when t == typeof(byte) => WriteMemory(address, (byte)(object)write, removeWriteProtection),
            true when t == typeof(bool) => WriteMemory(address, (bool)(object)write, removeWriteProtection),
            true when t == typeof(string) => WriteStringMemory(address, (string)(object)write, null,
                removeWriteProtection),
            true when t == typeof(Vector4) => WriteMemory(address, (Vector4)(object)write, removeWriteProtection),
            true when t == typeof(Matrix4x4) => WriteMemory(address, (Matrix4x4)(object)write, removeWriteProtection),
            true when t == typeof(Matrix3x2) => WriteMemory(address, (Matrix3x2)(object)write, removeWriteProtection),
            true when t == typeof(short) => WriteMemory(address, (short)(object)write, removeWriteProtection),
            true when t == typeof(long) => WriteMemory(address, (long)(object)write, removeWriteProtection),
            true when t == typeof(double) => WriteMemory(address, (double)(object)write, removeWriteProtection),
            true when t == typeof(ulong) => WriteMemory(address, (ulong)(object)write, removeWriteProtection),
            true when t == typeof(uint) => WriteMemory(address, (uint)(object)write, removeWriteProtection),
            true when t == typeof(ushort) => WriteMemory(address, (ushort)(object)write, removeWriteProtection),
            true when t == typeof(sbyte) => WriteMemory(address, (sbyte)(object)write, removeWriteProtection),
            true when t == typeof(char) => WriteMemory(address, (char)(object)write, removeWriteProtection),
            true when t == typeof(decimal) => WriteMemory(address, (decimal)(object)write, removeWriteProtection),
            true when t == typeof(IntPtr) => WriteMemory(address, (IntPtr)(object)write, removeWriteProtection),
            true when t == typeof(UIntPtr) => WriteMemory(address, (UIntPtr)(object)write, removeWriteProtection),
            _ => throw new("FUCK YOU!!!")
        };
    }
}