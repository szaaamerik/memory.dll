using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public unsafe bool ReadBit(nuint address, int bit)
    {
        if (bit is < 0 or > 7)
        {
            throw new ArgumentException("Bit must be between 0 and 7");
        }

        byte result;
        if (!ReadProcessMemory(MProc.Handle, address, &result, 1, 0))
        {
            result = default;
        }

        return (result & (1 << bit)) != 0;
    }

    public unsafe T ReadMemory<T>(nuint address) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        T result;
        if (!ReadProcessMemory(MProc.Handle, address, &result, (nuint)size, 0))
        {
            result = default;
        }

        return result;
    }
    
    public string ReadStringMemory(nuint address, int length, Encoding stringEncoding = null)
    {
        stringEncoding ??= Encoding.UTF8;
        var memoryNormal = new byte[length];

        switch (stringEncoding.CodePage)
        {
            case 65001: //UTF8
            case 65000: //UTF7
            case 20127: //ASCII
            case 28591: //Latin1
            {
                for (var i = 0; i < length; i++)
                {
                    byte memory;
                    if ((memory = ReadMemory<byte>(address)) == 0)
                    {
                        break;
                    }

                    Array.Resize(ref memoryNormal, i + 1);
                    memoryNormal[i - 1] = memory;
                    address += 1;
                }

                return stringEncoding.GetString(memoryNormal);
            }
            case 1200: //Unicode (UTF16)
            case 1201: //BigEndianUnicode (UTF16 big endian)
            {
                for (var i = 0; i < length * 2; i += 2)
                {
                    short memory;
                    if ((memory = ReadMemory<short>(address)) == 0)
                    {
                        break;
                    }

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
            {
                throw new ArgumentException("Invalid encoding (must be UTF8, UTF7, ASCII, Latin1, Unicode, or BigEndianUnicode)");
            }
        }
    }

    public unsafe T[] ReadArrayMemory<T>(nuint address, int length) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();
        var results = new T[length];
        
        fixed (T* resultsp = &results[0])
        {
            if (ReadProcessMemory(MProc.Handle, address, resultsp, (nuint)(size * length), 0))
            {
                return results;
            }
            
            var error = Marshal.GetLastWin32Error();
            throw new Exception($"ReadProcessMemory threw error code 0x{error:X}");
        }
    }
}