using System;
using System.Runtime.InteropServices;
using System.Text;
using static Memory.Imps;

namespace Memory;

public partial class Mem
{
    public unsafe bool ReadBit(nuint address, int bit)
    {
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return false;
        }
            
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
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return default;
        }
        
        var size = Marshal.SizeOf<T>();
        T result;
        if (!ReadProcessMemory(MProc.Handle, address, &result, (nuint)size, 0))
        {
            result = default;
        }

        return result;
    }
    
    public string ReadStringMemory(nuint address, int length, Encoding? stringEncoding = null)
    {
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return string.Empty;
        }
        
        stringEncoding ??= Encoding.UTF8;
        var memoryNormal = new byte[length];

        switch (stringEncoding.CodePage)
        {
            case 65001: //UTF8
            case 65000: //UTF7
            case 20127: //ASCII
            case 28591: //Latin1
            {
                for (var i = 1; i < length; i++)
                {
                    byte memory;
                    if ((memory = ReadMemory<byte>(address)) == 0)
                    {
                        break;
                    }
                    if (memoryNormal.Length < i + 1)
                    {
                        Array.Resize(ref memoryNormal, i + 1);
                    }

                    memoryNormal[i - 1] = memory;
                    address += 1;
                }

                return stringEncoding.GetString(memoryNormal);
            }
            case 1200: //Unicode (UTF16)
            case 1201: //BigEndianUnicode (UTF16 big endian)
            {
                for (var i = 2; i < length * 2; i += 2)
                {
                    short memory;
                    if ((memory = ReadMemory<short>(address)) == 0)
                    {
                        break;
                    }

                    if (memoryNormal.Length < i + 2)
                    {
                        Array.Resize(ref memoryNormal, i + 2);
                    }

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
        if (!IsProcessRunning(MProc.ProcessId))
        {
            return Array.Empty<T>();
        }
        
        var size = Marshal.SizeOf<T>();
        var results = new T[length];
        
        fixed (T* result = &results[0])
        {
            return ReadProcessMemory(MProc.Handle, address, result, (nuint)(size * length), 0)
                ? results
                : Array.Empty<T>();
        }
    }
}