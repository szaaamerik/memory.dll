using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Memory.Types;

public struct IntVec3 : IEquatable<IntVec3>, IFormattable
{
    public int X;
    public int Y;
    public int Z;

    public IntVec3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public IntVec3(ReadOnlySpan<int> values)
    {
        if (values.Length < 3)
            throw new ArgumentException("values must have a length of at least 3", nameof(values));

        this = Unsafe.ReadUnaligned<IntVec3>(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(values)));
    }

    public static IntVec3 Zero => default;

    public static IntVec3 UnitX => new(1, 0, 0);
    public static IntVec3 UnitY => new(0, 1, 0);
    public static IntVec3 UnitZ => new(0, 0, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntVec3 operator +(IntVec3 left, IntVec3 right) =>
        new(
            left.X + right.X,
            left.Y + right.Y,
            left.Z + right.Z
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntVec3 operator -(IntVec3 left, IntVec3 right) =>
        new(
            left.X - right.X,
            left.Y - right.Y,
            left.Z - right.Z
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntVec3 operator /(IntVec3 left, IntVec3 right) =>
        new(
            left.X / right.X,
            left.Y / right.Y,
            left.Z / right.Z
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntVec3 operator *(IntVec3 left, IntVec3 right) =>
        new(
            left.X * right.X,
            left.Y * right.Y,
            left.Z * right.Z
        );

    public static IntVec3 operator *(IntVec3 left, int right) => left * new IntVec3(right, right, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(IntVec3 left, IntVec3 right) =>
        left.X == right.X && left.Y == right.Y && left.Z == right.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(IntVec3 left, IntVec3 right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is IntVec3 other && Equals(other);

    public readonly bool Equals(IntVec3 other) => this == other;

    public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public readonly override string ToString() => ToString("G", CultureInfo.CurrentCulture);

    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        StringBuilder sb = new();
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        sb.Append('<');
        sb.Append(X.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(Y.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(Z.ToString(format, formatProvider));
        sb.Append('>');
        return sb.ToString();
    }
}