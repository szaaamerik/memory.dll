using System.Diagnostics;

namespace Memory;

public class Proc
{
    public Process Process { get; set; }
    public nint Handle { get; set; }
    public bool Is64Bit { get; set; }
    public int ProcessId { get; set; }
}