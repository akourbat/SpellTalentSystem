using System;

namespace SpellTalentSystem.Abilities;

/// <summary>
/// Universal composite value keeping Base, FlatModifier, and IncreasedPercent separate
/// until final evaluation via <see cref="Resolve"/>.
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
}

public static class StatValueExtensions
{
    // --- double ---
    public static StatValue<double> AddFlat(this StatValue<double> stat, double flat) =>
        stat with { FlatModifier = stat.FlatModifier + flat };

    public static StatValue<double> AddIncreased(this StatValue<double> stat, double increasedPercent) =>
        stat with { IncreasedPercent = stat.IncreasedPercent + increasedPercent };

    public static StatValue<double> Combine(this StatValue<double> a, StatValue<double> b) =>
        new(
            Base: a.Base + b.Base,
            FlatModifier: a.FlatModifier + b.FlatModifier,
            IncreasedPercent: a.IncreasedPercent + b.IncreasedPercent);

    public static double Resolve(this StatValue<double> stat) =>
        (stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent);

    // --- int ---
    public static StatValue<int> AddFlat(this StatValue<int> stat, int flat) =>
        stat with { FlatModifier = stat.FlatModifier + flat };

    public static StatValue<int> AddIncreased(this StatValue<int> stat, double increasedPercent) =>
        stat with { IncreasedPercent = stat.IncreasedPercent + increasedPercent };

    public static StatValue<int> Combine(this StatValue<int> a, StatValue<int> b) =>
        new(
            Base: a.Base + b.Base,
            FlatModifier: a.FlatModifier + b.FlatModifier,
            IncreasedPercent: a.IncreasedPercent + b.IncreasedPercent);

    public static int Resolve(this StatValue<int> stat) =>
        (int)Math.Round((stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent));

    // --- TimeSpan ---
    public static StatValue<TimeSpan> AddFlat(this StatValue<TimeSpan> stat, TimeSpan flat) =>
        stat with { FlatModifier = stat.FlatModifier + flat };

    public static StatValue<TimeSpan> AddIncreased(this StatValue<TimeSpan> stat, double increasedPercent) =>
        stat with { IncreasedPercent = stat.IncreasedPercent + increasedPercent };

    public static StatValue<TimeSpan> Combine(this StatValue<TimeSpan> a, StatValue<TimeSpan> b) =>
        new(
            Base: a.Base + b.Base,
            FlatModifier: a.FlatModifier + b.FlatModifier,
            IncreasedPercent: a.IncreasedPercent + b.IncreasedPercent);

    public static TimeSpan Resolve(this StatValue<TimeSpan> stat) =>
        TimeSpan.FromTicks((long)Math.Round((stat.Base + stat.FlatModifier).Ticks * (1.0 + stat.IncreasedPercent)));
}
