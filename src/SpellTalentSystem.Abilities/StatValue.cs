using System;
using System.Collections.Generic;

namespace SpellTalentSystem.Abilities;

/// <summary>
/// Universal composite value keeping Base, FlatModifier, and IncreasedPercent separate
/// until final evaluation via <see cref="Resolve"/>.
/// Raw values implicitly lift into the <see cref="Base"/> bucket at authoring/serialization boundaries.
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

    public static implicit operator StatValue<T>(T baseValue) => FromBase(baseValue);

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
    public static StatValue<double> AddFlat(this StatValue<double> stat, double flat) =>
        stat with { FlatModifier = stat.FlatModifier + flat };

    public static StatValue<int> AddFlat(this StatValue<int> stat, int flat) =>
        stat with { FlatModifier = stat.FlatModifier + flat };

    public static StatValue<TimeSpan> AddFlat(this StatValue<TimeSpan> stat, TimeSpan flat) =>
        stat with { FlatModifier = stat.FlatModifier + flat };

    public static StatValue<T> AddIncreased<T>(this StatValue<T> stat, double increasedPercent) where T : struct =>
        stat with { IncreasedPercent = stat.IncreasedPercent + increasedPercent };

    public static double Resolve(this StatValue<double> stat) =>
        (stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent);

    public static int Resolve(this StatValue<int> stat) =>
        (int)Math.Round((stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent));

    public static TimeSpan Resolve(this StatValue<TimeSpan> stat) =>
        TimeSpan.FromTicks((long)Math.Round((stat.Base + stat.FlatModifier).Ticks * (1.0 + stat.IncreasedPercent)));
}
