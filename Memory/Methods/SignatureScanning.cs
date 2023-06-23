using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Memory;

public partial class Mem
{
    private Dictionary<string, byte[]> _memoryCache = new();
    private Dictionary<string, IEnumerable<long>> _signatureResultCache = new();
    /// <summary>
    /// The default amount of tasks to use for signature scanning with the ScanForSig method.
    /// 
    /// </summary>
    public int SigScanTasks = 16;
    /// <summary>
    /// Scans for a signature in the process' memory.
    /// </summary>
    /// <param name="sig">The signature to scan for.</param>
    /// <param name="resultLimit">The amount of results to limit the scan to.
    /// This can speed up scan times in some cases.</param>
    /// <param name="numberOfTasks">The amount of tasks to use for the scan.
    /// If the number of tasks is 0 or lower, the amount of tasks will be pulled from the SigScanTasks variable</param>
    /// <param name="module">The module to scan in. If "default", the main module of the attached process will be used.</param>
    /// <returns>An enumerable of addresses that match the signature.</returns>
    /// <exception cref="ArgumentException">Thrown when the signature is blank.</exception>
    public IEnumerable<nuint> ScanForSig(string sig, int resultLimit = 0, int numberOfTasks = -1, string module = "default")
    {
        if (numberOfTasks <= 0)
            numberOfTasks = SigScanTasks;
        if (string.IsNullOrWhiteSpace(sig))
            throw new ArgumentException("A blank signature was provided!");
        try
        {
            if(_signatureResultCache.TryGetValue(sig, out var value))
            {
                var results = value.Select(x => (nuint)x);
                if (resultLimit > 0) results = results.Take(resultLimit);
                return results; 
            }
            // Get the start and end address of the module
            Process proc = MProc.Process;
            proc.Refresh();
            sig = sig.Replace('*', '?');
            sig = sig.Trim();
            while (sig.EndsWith(" ?") || sig.EndsWith(" ??"))
            {
                if (sig.EndsWith(" ??")) sig = sig.Substring(0, sig.Length - 3);
                if (sig.EndsWith(" ?")) sig = sig.Substring(0, sig.Length - 2);
            }


            long startAddress = 0;
            long endAddress = 0x7fffffffffff;
            if (module == "default")
            {
                startAddress = proc.MainModule!.BaseAddress.ToInt64();
                endAddress = startAddress + proc.MainModule!.ModuleMemorySize;
            }
            else
            {
                foreach (ProcessModule module1 in proc!.Modules)
                {
                    if (Path.GetFileName(module1.FileName) != module) continue;
                    startAddress = module1.BaseAddress.ToInt64();
                    endAddress = startAddress + module1.ModuleMemorySize;
                }
            }

            // Parse the signature
            byte[] sigBytes = ParseSig(sig, out byte[] maskBytes);
            List<long> addresses = new();

            byte[] buffer;
            if (_memoryCache.All(x => x.Key != module))
            {
                buffer = new byte[endAddress - startAddress];
                Imps.ReadProcessMemory(MProc.Handle, (nuint)startAddress, buffer, buffer.Length);
                _memoryCache.Add(module, buffer);
            }

            buffer = _memoryCache[module];
            
            int bytesPerTask = buffer.Length / numberOfTasks;

            List<Task> tasks = new();
            nint foundCountPtr = Marshal.AllocHGlobal(16);
            Marshal.WriteInt32(foundCountPtr, 0);
            for (int i = 0; i < buffer.Length; i += bytesPerTask)
            {
                int i1 = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < bytesPerTask; j++)
                    {
                        bool found = true;
                        bool reachedEndOfBuffer = false;
                        for (int k = 0; k < sigBytes.Length; k++)
                        {
                            if (i1 + j + k >= buffer.Length)
                            {
                                reachedEndOfBuffer = true;
                                found = false;
                                break;
                            }
                            if ((buffer[i1 + j + k] & maskBytes[k]) != (sigBytes[k] & maskBytes[k]))
                            {
                                found = false;
                                break;
                            }
                        }
                        if (reachedEndOfBuffer) break;

                        if (resultLimit != 0 && Marshal.ReadInt32(foundCountPtr) >= resultLimit) return;
                        if (!found) continue;
                        // increment the found count and verify the value
                        int c = Marshal.ReadInt32(foundCountPtr);
                        if (resultLimit != 0 && c >= resultLimit) break;
                        Marshal.WriteInt32(foundCountPtr, c + 1);
                        addresses.Add(startAddress + i1 + j);
                    }
                }));
            }

            while (tasks.Any(x => !x.IsCompleted))
            {
                Task.Delay(1).Wait();
            }
            Marshal.FreeHGlobal(foundCountPtr);
            _signatureResultCache.TryAdd(sig, addresses);
            return addresses.Select(x => (nuint)x);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Memory", $"Error while scanning for {sig} in {module}: {e}");
            return new List<nuint>();
        }
    }
    
    private byte[] ParseSig(string sig, out byte[] mask)
    {
        
        string[] stringByteArray = sig.Split(' ');

        byte[] sigPattern = new byte[stringByteArray.Length];
        mask = new byte[stringByteArray.Length];

        for (int i = 0; i < stringByteArray.Length; i++)
        {
            string ba = stringByteArray[i];

            if (ba == "??" || (ba.Length == 1 && ba == "?"))
            {
                mask[i] = 0x00;
                stringByteArray[i] = "0x00";
            }
            else if (char.IsLetterOrDigit(ba[0]) && ba[1] == '?')
            {
                mask[i] = 0xF0;
                stringByteArray[i] = ba[0] + "0";
            }
            else if (char.IsLetterOrDigit(ba[1]) && ba[0] == '?')
            {
                mask[i] = 0x0F;
                stringByteArray[i] = "0" + ba[1];
            }
            else
                mask[i] = 0xFF;
        }

        for (int i = 0; i < stringByteArray.Length; i++)
            sigPattern[i] = (byte)(Convert.ToByte(stringByteArray[i], 16) & mask[i]);
        return sigPattern;
    }
}