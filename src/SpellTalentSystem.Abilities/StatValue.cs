using System;

namespace SpellTalentSystem.Abilities;

/// <summary>
/// Represents a modifiable numerical ability stat, tracking both its authored
/// <see cref="Base"/> value and its talent-modified <see cref="Current"/> value.
/// </summary>
public readonly record struct StatValue<T>(T Base, T Current)
    where T : struct, IEquatable<T>
{
    public static StatValue<T> FromBase(T value) => new(value, value);

    public StatValue<T> WithCurrent(T newValue) => this with { Current = newValue };

    public StatValue<T> Map(Func<T, T> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return this with { Current = transform(Current) };
    }

    public static implicit operator StatValue<T>(T baseValue) => FromBase(baseValue);

    public override string ToString() =>
        Base.Equals(Current) ? $"{Current}" : $"{Current} (Base: {Base})";
}

public static class StatValueExtensions
{
    public static StatValue<double> AddFlat(this StatValue<double> stat, double flat) =>
        stat.Map(v => v + flat);

    public static StatValue<double> ScaleBy(this StatValue<double> stat, double multiplier) =>
        stat.Map(v => v * multiplier);

    public static StatValue<int> AddFlat(this StatValue<int> stat, int flat) =>
        stat.Map(v => v + flat);
}
