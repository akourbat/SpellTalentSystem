using System;
using System.Numerics;

namespace SpellTalentSystem.Abilities;

/// <summary>
/// Represents an enrichable ability stat using commutative modifier buckets:
/// <list type="bullet">
///   <item><description><see cref="Base"/>: Authored baseline value.</description></item>
///   <item><description><see cref="FlatModifier"/>: Commutative sum of flat additions.</description></item>
///   <item><description><see cref="IncreasedPercent"/>: Commutative sum of additive percentage increases (e.g. 0.20 = +20%).</description></item>
///   <item><description><see cref="MoreMultiplier"/>: Commutative product of multiplicative factors (1.0 = neutral).</description></item>
/// </list>
/// </summary>
public readonly record struct StatValue<T>(
    T Base,
    T FlatModifier,
    double IncreasedPercent,
    double MoreMultiplier)
    where T : struct, INumber<T>
{
    public static StatValue<T> FromBase(T baseValue) =>
        new(baseValue, T.Zero, IncreasedPercent: 0.0, MoreMultiplier: 1.0);

    public bool IsUnmodified =>
        FlatModifier == T.Zero &&
        IncreasedPercent == 0.0 &&
        MoreMultiplier == 1.0;

    public StatValue<T> AddFlat(T flat) =>
        this with { FlatModifier = FlatModifier + flat };

    public StatValue<T> AddIncreased(double percent) =>
        this with { IncreasedPercent = IncreasedPercent + percent };

    public StatValue<T> MultiplyMore(double factor) =>
        this with { MoreMultiplier = MoreMultiplier * factor };

    public static implicit operator StatValue<T>(T baseValue) => FromBase(baseValue);

    public override string ToString() =>
        IsUnmodified
            ? $"{Base}"
            : $"{Base} (Flat: {FlatModifier:+#;-#;0}, Inc: {IncreasedPercent:+0.##%;-0.##%;0%}, More: x{MoreMultiplier:0.##})";
}

public static class StatValueExtensions
{
    /// <summary>
    /// Evaluates the enriched floating-point value:
    /// <c>(Base + FlatModifier) * (1.0 + IncreasedPercent) * MoreMultiplier</c>.
    /// </summary>
    public static double Resolve(this StatValue<double> stat) =>
        (stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent) * stat.MoreMultiplier;

    /// <summary>
    /// Evaluates the enriched integer stat (e.g., MaxTargets, BounceCount, Stacks),
    /// rounding toward zero after applying percentage/multiplicative modifiers.
    /// </summary>
    public static int Resolve(this StatValue<int> stat) =>
        (int)((stat.Base + stat.FlatModifier) * (1.0 + stat.IncreasedPercent) * stat.MoreMultiplier);
}
