using System;
using System.Runtime.CompilerServices;

namespace Memory.Types;

public class MemoryObject : IEquatable<MemoryObject>
{
    protected readonly Mem M;
    public string Address;
    public readonly nuint AddressPtr;

    protected MemoryObject(string address, string offsets = "", Mem m = null)
    {
        M = m ?? Mem.DefaultInstance;
        Address = (address + offsets).TrimEnd(',').TrimEnd('+');
        AddressPtr = M.Get64BitCode(Address);
    }

    public override int GetHashCode() => HashCode.Combine(AddressPtr);

    public bool Equals(MemoryObject other) => AddressPtr == other!.AddressPtr;

    public override bool Equals(object obj) => Equals(obj as MemoryObject);

    #region Operator overloads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(MemoryObject left, MemoryObject right) => left!.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(MemoryObject left, MemoryObject right) => !left!.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(MemoryObject left, nuint right) => left!.AddressPtr == right;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(MemoryObject left, nuint right) => left!.AddressPtr != right;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(nuint left, MemoryObject right) => left == right!.AddressPtr;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(nuint left, MemoryObject right) => left != right!.AddressPtr;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator nuint(MemoryObject memoryObject) => memoryObject.AddressPtr;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, string offset) =>
        new(memoryObject.Address, offset, memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, byte offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, short offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, int offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, long offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, nint offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, ushort offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, uint offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, ulong offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator +(MemoryObject memoryObject, nuint offset) =>
        new(memoryObject.Address, $"+{offset}", memoryObject.M);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryObject operator -(MemoryObject memoryObject, string offset) =>
        new(memoryObject.Address, $"-{offset}", memoryObject.M);
    #endregion
}