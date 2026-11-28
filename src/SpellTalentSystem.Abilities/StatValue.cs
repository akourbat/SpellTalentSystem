using System;
using System.Collections.Generic;

namespace SpellTalentSystem.Abilities;

/// <summary>
/// Universal composite value keeping Base, FlatModifier, and IncreasedPercent separate
/// until final evaluation via <see cref="Resolve"/>.
/// Raw numeric values are intentionally not implicitly convertible so every layer
/// explicitly declares whether a value belongs to Base, FlatModifier, or IncreasedPercent.
/// </summary>
public readonly record struct StatValue<T>(
    T Base,
    T FlatModifier = default,
    double IncreasedPercent = 0.0) where T : struct
{
    public bool IsUnmodified =>
        EqualityComparer<T>.Default.Equals(FlatModifier, default) &&
        IncreasedPercent == 0.0;

    public static StatValue<T> FromBase(T value) => new(Base: value);
    public static StatValue<T> FromFlat(T flat) => new(Base: default, FlatModifier: flat);
    public static StatValue<T> FromPercent(double increasedPercent) => new(Base: default, IncreasedPercent: increasedPercent);

    public static StatValue<T> operator +(StatValue<T> left, StatValue<T> right) =>
        new(
            Base: StatValueMath<T>.Add(left.Base, right.Base),
            FlatModifier: StatValueMath<T>.Add(left.FlatModifier, right.FlatModifier),
            IncreasedPercent: left.IncreasedPercent + right.IncreasedPercent);
}

internal static class StatValueMath<T> where T : struct
{
    public static T Add(T a, T b)
    {
        if (typeof(T) == typeof(double))
            return (T)(object)((double)(object)a + (double)(object)b);
        if (typeof(T) == typeof(int))
            return (T)(object)((int)(object)a + (int)(object)b);
        if (typeof(T) == typeof(TimeSpan))
            return (T)(object)((TimeSpan)(object)a + (TimeSpan)(object)b);

        throw new NotSupportedException($"StatValue<{typeof(T).Name}> addition is not supported.");
    }
}

public static class StatValueExtensions
{
    public static double Resolve(this StatValue<double> stat) =>
        (stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent);

    public static int Resolve(this StatValue<int> stat) =>
        (int)Math.Round((stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent));

    public static TimeSpan Resolve(this StatValue<TimeSpan> stat) =>
        TimeSpan.FromTicks((long)Math.Round((stat.Base + stat.FlatModifier).Ticks * (1.0 + stat.IncreasedPercent)));
}
