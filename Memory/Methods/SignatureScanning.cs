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
    private readonly Dictionary<string, byte[]> _memoryCache = new();
    private readonly Dictionary<string, IEnumerable<long>> _signatureResultCache = new();
    
    public readonly int SigScanTasks = 16;
    public IEnumerable<nuint> ScanForSig(string sig, int resultLimit = 0, int numberOfTasks = -1, string module = "default")
    {
        if (numberOfTasks <= 0)
        {
            numberOfTasks = SigScanTasks;
        }
        
        if (string.IsNullOrWhiteSpace(sig))
        {
            throw new ArgumentException("A blank signature was provided!");
        }
        
        if(_signatureResultCache.TryGetValue(sig, out var value))
        {
            var results = value.Select(x => (nuint)x);
            if (resultLimit > 0) results = results.Take(resultLimit);
            return results; 
        }
        
        var proc = MProc.Process;
        proc.Refresh();
        sig = sig.Replace('*', '?');
        sig = sig.Trim();
        while (sig.EndsWith(" ?") || sig.EndsWith(" ??"))
        {
            if (sig.EndsWith(" ??"))
            {
                sig = sig[..^3];
            }
            if (sig.EndsWith(" ?"))
            {
                sig = sig[..^2];
            }
        }
        
        var startAddress = 0L;
        var endAddress = 0x7fffffffffffL;
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
        
        var sigBytes = Utils.ParseSig(sig, out var maskBytes);
        List<long> addresses = new();
        
        byte[] buffer;
        if (_memoryCache.All(x => x.Key != module))
        {
            buffer = new byte[endAddress - startAddress];
            Imps.ReadProcessMemory(MProc.Handle, (nuint)startAddress, buffer, buffer.Length);
            _memoryCache.Add(module, buffer);
        }
        
        buffer = _memoryCache[module];
        var bytesPerTask = buffer.Length / numberOfTasks;
        List<Task> tasks = new();
        
        var foundCountPtr = Marshal.AllocHGlobal(16);
        Marshal.WriteInt32(foundCountPtr, 0);
        for (var i = 0; i < buffer.Length; i += bytesPerTask)
        {
            var i1 = i;
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < bytesPerTask; j++)
                {
                    var found = true;
                    var reachedEndOfBuffer = false;
                    for (var k = 0; k < sigBytes.Length; k++)
                    {
                        if (i1 + j + k >= buffer.Length)
                        {
                            reachedEndOfBuffer = true;
                            found = false;
                            break;
                        }
                        if ((buffer[i1 + j + k] & maskBytes[k]) == (sigBytes[k] & maskBytes[k])) continue;
                        found = false;
                        break;
                    }
                    if (reachedEndOfBuffer) break;
                    if (resultLimit != 0 && Marshal.ReadInt32(foundCountPtr) >= resultLimit) return;
                    if (!found) continue;
                    var c = Marshal.ReadInt32(foundCountPtr);
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
}