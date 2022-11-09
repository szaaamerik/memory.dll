using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory
{
    public partial class Mem
    {
        /// <summary>
        /// Cut a string that goes on for too long or one that is possibly merged with another string.
        /// </summary>
        /// <param name="str">The string you want to cut.</param>
        /// <returns></returns>
        public string CutString(string str)
        {
            StringBuilder sb = new();
            foreach (char c in str)
            {
                if (c is >= ' ' and <= '~')
                    sb.Append(c);
                else
                    break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reads a byte from memory and splits it into bits
        /// </summary>
        /// <param name="code">address, module + pointer + offset, module + offset</param>
        /// <returns>Array of 8 booleans representing each bit of the byte read</returns>
        public bool[] ReadBits(string code)
        {
            byte[] buf = new byte[1];

            UIntPtr theCode = Get64BitCode(code);

            bool[] ret = new bool[8];

            if (theCode == UIntPtr.Zero)
                return ret;

            if (!ReadProcessMemory(MProc.Handle, theCode, buf, (UIntPtr)1, IntPtr.Zero))
                return ret;


            if (!BitConverter.IsLittleEndian)
                throw new("Should be little endian");

            for (int i = 0; i < 8; i++)
                ret[i] = Convert.ToBoolean(buf[0] & (1 << i));

            return ret;

        }

        public unsafe T ReadMemory<T>(string address) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            T result;
            UIntPtr addy = Get64BitCode(address);
            if (!ReadProcessMemory(MProc.Handle, addy, (long)&result, (UIntPtr)size, 0))
                result = default;

            return result;
        }
        public unsafe T ReadMemory<T>(UIntPtr address) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            T result;
            if (!ReadProcessMemory(MProc.Handle, address, (long)&result, (UIntPtr)size, 0))
                result = default;

            return result;
        }

        public T ReadVectorMemory<T>(string address) where T : struct
        {
            Type type = typeof(T);
            UIntPtr addy = Get64BitCode(address);
            switch (true)
            {
                case true when type == typeof(Vector2):
                {
                    byte[] memory = new byte[8];

                    if (!ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)8, IntPtr.Zero))
                        return new();

                    return (T)(object)new Vector2(BitConverter.ToSingle(memory, 0), BitConverter.ToSingle(memory, 4));
                }
                case true when type == typeof(Vector3):
                {
                    byte[] memory = new byte[12];

                    if (!ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)12, IntPtr.Zero))
                        return new();

                    return (T)(object)new Vector3(BitConverter.ToSingle(memory, 0), BitConverter.ToSingle(memory, 4),
                        BitConverter.ToSingle(memory, 8));
                }
                case true when type == typeof(Vector4):
                {
                    byte[] memory = new byte[16];

                    if (!ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)16, IntPtr.Zero))
                        return new();

                    return (T)(object)new Vector4(BitConverter.ToSingle(memory, 0), BitConverter.ToSingle(memory, 4),
                        BitConverter.ToSingle(memory, 8), BitConverter.ToSingle(memory, 12));
                }
                default:
                    throw new ArgumentException("Invalid type (must be Vector2, Vector3, or Vector4)");
            }
        }
        public T ReadVectorMemory<T>(UIntPtr address) where T : struct
        {
            Type type = typeof(T);
            switch (true)
            {
                case true when type == typeof(Vector2):
                {
                    byte[] memory = new byte[8];

                    if (!ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)8, IntPtr.Zero))
                        return new();

                    return (T)(object)new Vector2(BitConverter.ToSingle(memory, 0), BitConverter.ToSingle(memory, 4));
                }
                case true when type == typeof(Vector3):
                {
                    byte[] memory = new byte[12];

                    if (!ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)12, IntPtr.Zero))
                        return new();

                    return (T)(object)new Vector3(BitConverter.ToSingle(memory, 0), BitConverter.ToSingle(memory, 4),
                        BitConverter.ToSingle(memory, 8));
                }
                case true when type == typeof(Vector4):
                {
                    byte[] memory = new byte[16];

                    if (!ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)16, IntPtr.Zero))
                        return new();

                    return (T)(object)new Vector4(BitConverter.ToSingle(memory, 0), BitConverter.ToSingle(memory, 4),
                        BitConverter.ToSingle(memory, 8), BitConverter.ToSingle(memory, 12));
                }
                default:
                    throw new ArgumentException("Invalid type (must be Vector2, Vector3, or Vector4)");
            }
        }

        public string ReadStringMemory(string address, Encoding stringEncoding = null)
        {
            stringEncoding ??= Encoding.UTF8;
            byte[] memoryNormal = new byte[0];
            UIntPtr addy = Get64BitCode(address);

            switch (stringEncoding.CodePage)
            {
                case 65001: //UTF8
                case 65000: //UTF7
                case 20127: //ASCII
                case 28591: //Latin1
                {
                    byte memory = 0;
                    while (ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)1, IntPtr.Zero))
                    {
                        if (memory == 0)
                            break;
                        Array.Resize(ref memoryNormal, memoryNormal.Length + 1);
                        memoryNormal[memoryNormal.Length - 1] = memory;
                        addy += 1;
                    }

                    return stringEncoding.GetString(memoryNormal);
                }
                case 1200: //Unicode (UTF16)
                case 1201: //BigEndianUnicode (UTF16 big endian)
                {
                    short memory = 0;
                    while (ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)2, IntPtr.Zero))
                    {
                        if (memory == 0)
                            break;
                        Array.Resize(ref memoryNormal, memoryNormal.Length + 2);
                        unchecked
                        {
                            memoryNormal[memoryNormal.Length - 2] = (byte)memory;
                            memoryNormal[memoryNormal.Length - 1] = (byte)(memory >> 8);
                        }
                    }
                    
                    return stringEncoding.GetString(memoryNormal);
                }
                default:
                    throw new ArgumentException("Invalid encoding (must be UTF8, UTF7, ASCII, Latin1, Unicode, or BigEndianUnicode)");
            }
        }
        public string ReadStringMemory(UIntPtr address, Encoding stringEncoding = null)
        {
            stringEncoding ??= Encoding.UTF8;
            byte[] memoryNormal = new byte[0];

            switch (stringEncoding.CodePage)
            {
                case 65001: //UTF8
                case 65000: //UTF7
                case 20127: //ASCII
                case 28591: //Latin1
                {
                    byte memory = 0;
                    while (ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)1, IntPtr.Zero))
                    {
                        if (memory == 0)
                            break;
                        Array.Resize(ref memoryNormal, memoryNormal.Length + 1);
                        memoryNormal[memoryNormal.Length - 1] = memory;
                        address += 1;
                    }

                    return stringEncoding.GetString(memoryNormal);
                }
                case 1200: //Unicode (UTF16)
                case 1201: //BigEndianUnicode (UTF16 big endian)
                {
                    short memory = 0;
                    while (ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)2, IntPtr.Zero))
                    {
                        if (memory == 0)
                            break;
                        Array.Resize(ref memoryNormal, memoryNormal.Length + 2);
                        unchecked
                        {
                            memoryNormal[memoryNormal.Length - 2] = (byte)memory;
                            memoryNormal[memoryNormal.Length - 1] = (byte)(memory >> 8);
                        }
                    }
                    
                    return stringEncoding.GetString(memoryNormal);
                }
                default:
                    throw new ArgumentException("Invalid encoding (must be UTF8, UTF7, ASCII, Latin1, Unicode, or BigEndianUnicode)");
            }
        }

        public string ReadStringMemory(string address, int length, Encoding stringEncoding = null)
        {
            stringEncoding ??= Encoding.UTF8;
            byte[] memoryNormal = new byte[length];
            UIntPtr addy = Get64BitCode(address);

            switch (stringEncoding.CodePage)
            {
                case 65001: //UTF8
                case 65000: //UTF7
                case 20127: //ASCII
                case 28591: //Latin1
                {
                    byte memory = 0;
                    for (int i = 0; i < length; i++)
                    {
                        if (!ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)1, IntPtr.Zero))
                            break;
                        
                        Array.Resize(ref memoryNormal, i + 1);
                        memoryNormal[i - 1] = memory;
                        addy += 1;
                    }

                    return stringEncoding.GetString(memoryNormal);
                }
                case 1200: //Unicode (UTF16)
                case 1201: //BigEndianUnicode (UTF16 big endian)
                {
                    short memory = 0;
                    for (int i = 0; i < length * 2; i += 2)
                    {
                        if (!ReadProcessMemory(MProc.Handle, addy, memory, (UIntPtr)1, IntPtr.Zero))
                            break;
                        
                        Array.Resize(ref memoryNormal, i + 2);
                        unchecked
                        {
                            memoryNormal[i - 2] = (byte)memory;
                            memoryNormal[i - 1] = (byte)(memory >> 8);
                        }
                    }
                    
                    return stringEncoding.GetString(memoryNormal);
                }
                default:
                    throw new ArgumentException("Invalid encoding (must be UTF8, UTF7, ASCII, Latin1, Unicode, or BigEndianUnicode)");
            }
        }
        public string ReadStringMemory(UIntPtr address, int length, Encoding stringEncoding = null)
        {
            stringEncoding ??= Encoding.UTF8;
            byte[] memoryNormal = new byte[length];

            switch (stringEncoding.CodePage)
            {
                case 65001: //UTF8
                case 65000: //UTF7
                case 20127: //ASCII
                case 28591: //Latin1
                {
                    byte memory = 0;
                    for (int i = 0; i < length; i++)
                    {
                        if (!ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)1, IntPtr.Zero))
                            break;
                        
                        Array.Resize(ref memoryNormal, i + 1);
                        memoryNormal[i - 1] = memory;
                        address += 1;
                    }

                    return stringEncoding.GetString(memoryNormal);
                }
                case 1200: //Unicode (UTF16)
                case 1201: //BigEndianUnicode (UTF16 big endian)
                {
                    short memory = 0;
                    for (int i = 0; i < length * 2; i += 2)
                    {
                        if (!ReadProcessMemory(MProc.Handle, address, memory, (UIntPtr)1, IntPtr.Zero))
                            break;
                        
                        Array.Resize(ref memoryNormal, i + 2);
                        unchecked
                        {
                            memoryNormal[i - 2] = (byte)memory;
                            memoryNormal[i - 1] = (byte)(memory >> 8);
                        }
                    }
                    
                    return stringEncoding.GetString(memoryNormal);
                }
                default:
                    throw new ArgumentException("Invalid encoding (must be UTF8, UTF7, ASCII, Latin1, Unicode, or BigEndianUnicode)");
            }
        }

        public unsafe T[] ReadArrayMemory<T>(string address, int length) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            UIntPtr addy = Get64BitCode(address);
            T[] results = new T[length];
            for (int i = 0; i < length; i++)
            {
                T result;
                if (!ReadProcessMemory(MProc.Handle, addy, (long)&result, (UIntPtr)size, 0))
                    result = new();
                results[i] = result;
            }

            return results;
        }
        public unsafe T[] ReadArrayMemory<T>(UIntPtr address, int length) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            T[] results = new T[length];
            for (int i = 0; i < length; i++)
            {
                T result;
                if (!ReadProcessMemory(MProc.Handle, address, (long)&result, (UIntPtr)size, 0))
                    result = new();
                results[i] = result;
            }

            return results;
        }


        private readonly ConcurrentDictionary<string, CancellationTokenSource> _readTokenSrcs = new();
        /// <summary>
        /// Reads a memory address, keeps value in UI object. Ex: BindToUI("0x12345678,0x02,0x05", v => ObjName.Invoke((MethodInvoker)delegate { if (String.Compare(v, ObjName.Text) != 0) { ObjName.Text = v; } }));
        /// </summary>
        /// <param name="address">Your offsets or INI file variable name</param>
        /// <param name="uiObject">Returning variable to bind to UI object. See example in summary.</param>
        public void BindToUi(string address, Action<string> uiObject)
        {
            CancellationTokenSource cts = new();
            if (_readTokenSrcs.ContainsKey(address))
            {
                try
                {
                    _readTokenSrcs[address].Cancel();
                    _readTokenSrcs.TryRemove(address, out _);
                }
                catch
                {
                    Debug.WriteLine("ERROR: Avoided a crash. Address " + address + " was not bound.");
                }
            }
            else
            {
                Debug.WriteLine("Adding Bound Address " + address);
            }

            _readTokenSrcs.TryAdd(address, cts);

            Task.Factory.StartNew(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    uiObject(ReadStringMemory(address));
                    Thread.Sleep(100);
                }
            },
            cts.Token);
        }
    }
}
