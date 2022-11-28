using System;

namespace Memory
{
    /// <summary>
    /// AoB scan information.
    /// </summary>
    struct MemoryRegionResult
    {
        public nuint CurrentBaseAddress { get; set; }
        public long RegionSize { get; set; }
        public nuint RegionBase { get; set; }
    }
}
