using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory
{
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
        public bool FreezeValue<T>(string address, T value, int speed = 25, string file = "") where T : unmanaged
        {
            CancellationTokenSource cts = new();
            UIntPtr addr = GetCode(address, file);

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
            UIntPtr addy = GetCode(address);
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

            UIntPtr theCode = GetCode(code, file);

            for (var i = 0; i < 8; i++)
            {
                if (bits[i])
                    buf[0] |= (byte)(1 << i);
            }

            WriteProcessMemory(MProc.Handle, theCode, buf, (UIntPtr)1, IntPtr.Zero);
        }
        
        public unsafe bool WriteMemory<T>(string address, T write, bool removeWriteProtection = true) where T : unmanaged
        {
            UIntPtr addy = Get64BitCode(address);
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
            UIntPtr addy = Get64BitCode(address);
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
            UIntPtr addy = Get64BitCode(address);
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
    }
}