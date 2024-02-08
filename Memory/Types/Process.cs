using System.Diagnostics;

namespace Memory;

/// <summary>
/// Information about the opened process.
/// </summary>
public class Proc
{
    public Process Process { get; set; }
    public nint Handle { get; set; }
    public bool Is64Bit { get; set; }
}