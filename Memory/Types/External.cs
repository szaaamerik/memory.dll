using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Memory.Types;

public readonly struct External<T>
{
    private readonly Mem _m;
    public override bool Equals(object obj) => obj is External<T> other && Equals(other);

    public bool Equals(External<T> other) => Address.Equals(other.Address);

    public bool Equals(T other) => Value.Equals(other);

    public override int GetHashCode() => HashCode.Combine(Address);

    public T Value
    {
        get
        {
            if (typeof(T) != typeof(Rotations))
                return _m.ReadAnyMemory<T>(Address);
            if(typeof(T) == typeof(IntVec3))
                return (T)(object)new IntVec3(
                    _m.ReadMemory<int>(Address),
                    _m.ReadMemory<int>(Address+4),
                    _m.ReadMemory<int>(Address+8));
            Vector2 vec = _m.ReadMemory<Vector2>(Address);
            return (T)(object)new Rotations(vec.X, vec.Y);
        }
        set => _ = typeof(T) != typeof(Rotations)
            ?
            typeof(T) == typeof(IntVec3)
            ? _m.WriteMemory(Address, (IntVec3)(object)value)
            :
            _m.WriteAnyMemory(Address, value)
            : _m.WriteMemory(Address, (Rotations)(object)value);
    }

    public readonly nuint Address;

    public External(string address, string offsets = "", Mem m = null)
    {
        _m = m ?? Mem.DefaultInstance;
        try
        {
            Address = _m.Get64BitCode(address + offsets);
        }
        catch
        {
            Debug.WriteLine($"[External<{typeof(T).Name}>] ADDRESS INVALID: " + address + offsets);
        }
    }

    public External(nuint address, string offsets = "", Mem m = null)
    {
        _m = m ?? Mem.DefaultInstance;
        Address = offsets == "" ? address : _m.Get64BitCode(address + offsets);
    }


    #region Operator overloads
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(External<T> left, External<T> right) => (dynamic)left.Value == (dynamic)right.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(External<T> left, External<T> right) => (dynamic)left.Value != (dynamic)right.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(External<T> left, T right) => (dynamic)left.Value == (dynamic)right;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(External<T> left, T right) => (dynamic)left.Value != (dynamic)right;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(T left, External<T> right) => (dynamic)left == (dynamic)right.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(T left, External<T> right) => (dynamic)left != (dynamic)right.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(External<T> external) => external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator +(T value, External<T> external) => value + (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator -(T value, External<T> external) => value - (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator *(T value, External<T> external) => value * (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator /(T value, External<T> external) => value / (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator %(T value, External<T> external) => value % (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator &(T value, External<T> external) => value & (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator |(T value, External<T> external) => value | (dynamic)external.Value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T operator ^(T value, External<T> external) => value ^ (dynamic)external.Value;
    #endregion
}