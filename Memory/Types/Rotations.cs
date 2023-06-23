using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Memory.Types;

public struct Rotations : IEquatable<Rotations>, IFormattable
{
    public float Pitch;
    public float Yaw;

    public Rotations(float pitch, float yaw)
    {
        Pitch = pitch;
        Yaw = yaw;
    }

    public static Rotations Zero => default;

    public Vector2 ToVector2() => new(Pitch, Yaw);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotations operator +(Rotations left, Rotations right) =>
        new(
            left.Pitch + right.Pitch,
            left.Yaw + right.Yaw
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotations operator -(Rotations left, Rotations right) =>
        new(
            left.Pitch - right.Pitch,
            left.Yaw - right.Yaw
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotations operator /(Rotations left, Rotations right) =>
        new(
            left.Pitch / right.Pitch,
            left.Yaw / right.Yaw
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotations operator *(Rotations left, Rotations right) =>
        new(
            left.Pitch * right.Pitch,
            left.Yaw * right.Yaw
        );

    public static Rotations operator *(Rotations left, float right) => left * new Rotations(right, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Rotations left, Rotations right) =>
        left.Pitch == right.Pitch && left.Yaw == right.Yaw;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Rotations left, Rotations right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Rotations Lerp(Rotations value1, Rotations value2, float amount) =>
        value1 * (1.0f - amount) + value2 * amount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is Rotations other && Equals(other);

    public readonly bool Equals(Rotations other) => this == other;

    public readonly override string ToString() => ToString("G", CultureInfo.CurrentCulture);

    public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        StringBuilder sb = new();
        string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
        sb.Append('<');
        sb.Append(Pitch.ToString(format, formatProvider));
        sb.Append(separator);
        sb.Append(' ');
        sb.Append(Yaw.ToString(format, formatProvider));
        sb.Append('>');
        return sb.ToString();
    }

    public override int GetHashCode() => HashCode.Combine(Pitch, Yaw);

    //cast to Vector2
    public static explicit operator Vector2(Rotations rotations) => rotations.ToVector2();

    //can i cast from Vector2 to Rotations? the answer is yes
    public static implicit operator Rotations(Vector2 vector2) => new(vector2.X, vector2.Y);

    //cast object of type Vector2 to Rotations

    public static Rotations FromObject(object obj) => (Vector2)obj;
}