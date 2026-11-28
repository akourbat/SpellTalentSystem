using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpellTalentSystem.Abilities;

public enum AttributeKind
{
    Power, Precision, Toughness, Vitality,
    Ferocity, ConditionDamage, HealingPower, Expertise, Concentration,
    CriticalChance, CriticalDamage, BoonDuration, ConditionDuration, Defense, WeaponStrength
}

// 1. Primary Attributes
public sealed record Power(StatValue<double> Value);
public sealed record Precision(StatValue<double> Value);
public sealed record Toughness(StatValue<double> Value);
public sealed record Vitality(StatValue<double> Value);

public union PrimaryAttribute(Power, Precision, Toughness, Vitality);

// 2. Secondary Attributes
public sealed record Ferocity(StatValue<double> Value);
public sealed record ConditionDamage(StatValue<double> Value);
public sealed record HealingPower(StatValue<double> Value);
public sealed record Expertise(StatValue<double> Value);
public sealed record Concentration(StatValue<double> Value);

public union SecondaryAttribute(Ferocity, ConditionDamage, HealingPower, Expertise, Concentration);

// 3. Derived / Percentage / Special Attributes
public sealed record CriticalChance(StatValue<double> Value);
public sealed record CriticalDamage(StatValue<double> Value);
public sealed record BoonDuration(StatValue<double> Value);
public sealed record ConditionDuration(StatValue<double> Value);
public sealed record Defense(StatValue<double> Value);
public sealed record WeaponStrength(StatValue<double> Value);

public union SpecialAttribute(
    CriticalChance, CriticalDamage, BoonDuration, ConditionDuration, Defense, WeaponStrength);

/// <summary>
/// Discriminated union of all character attributes carrying composite <see cref="StatValue{Double}"/> buckets.
/// </summary>
public union CharacterAttribute(
    PrimaryAttribute,
    SecondaryAttribute,
    SpecialAttribute)
{
    public object LeafValue => Value switch
    {
        PrimaryAttribute p => p.Value!,
        SecondaryAttribute s => s.Value!,
        SpecialAttribute sp => sp.Value!,
        _ => throw new InvalidOperationException($"Unsupported CharacterAttribute case: {Value}")
    };

    public (AttributeKind Kind, StatValue<double> Stat) Deconstruct() => LeafValue switch
    {
        Power(var v) => (AttributeKind.Power, v),
        Precision(var v) => (AttributeKind.Precision, v),
        Toughness(var v) => (AttributeKind.Toughness, v),
        Vitality(var v) => (AttributeKind.Vitality, v),
        Ferocity(var v) => (AttributeKind.Ferocity, v),
        ConditionDamage(var v) => (AttributeKind.ConditionDamage, v),
        HealingPower(var v) => (AttributeKind.HealingPower, v),
        Expertise(var v) => (AttributeKind.Expertise, v),
        Concentration(var v) => (AttributeKind.Concentration, v),
        CriticalChance(var v) => (AttributeKind.CriticalChance, v),
        CriticalDamage(var v) => (AttributeKind.CriticalDamage, v),
        BoonDuration(var v) => (AttributeKind.BoonDuration, v),
        ConditionDuration(var v) => (AttributeKind.ConditionDuration, v),
        Defense(var v) => (AttributeKind.Defense, v),
        WeaponStrength(var v) => (AttributeKind.WeaponStrength, v),
        _ => throw new InvalidOperationException($"Unsupported attribute leaf: {LeafValue}")
    };

    public AttributeKind Kind => Deconstruct().Kind;
    public StatValue<double> Stat => Deconstruct().Stat;

    public static CharacterAttribute FromKind(AttributeKind kind, StatValue<double> value) => kind switch
    {
        AttributeKind.Power => new PrimaryAttribute(new Power(value)),
        AttributeKind.Precision => new PrimaryAttribute(new Precision(value)),
        AttributeKind.Toughness => new PrimaryAttribute(new Toughness(value)),
        AttributeKind.Vitality => new PrimaryAttribute(new Vitality(value)),
        AttributeKind.Ferocity => new SecondaryAttribute(new Ferocity(value)),
        AttributeKind.ConditionDamage => new SecondaryAttribute(new ConditionDamage(value)),
        AttributeKind.HealingPower => new SecondaryAttribute(new HealingPower(value)),
        AttributeKind.Expertise => new SecondaryAttribute(new Expertise(value)),
        AttributeKind.Concentration => new SecondaryAttribute(new Concentration(value)),
        AttributeKind.CriticalChance => new SpecialAttribute(new CriticalChance(value)),
        AttributeKind.CriticalDamage => new SpecialAttribute(new CriticalDamage(value)),
        AttributeKind.BoonDuration => new SpecialAttribute(new BoonDuration(value)),
        AttributeKind.ConditionDuration => new SpecialAttribute(new ConditionDuration(value)),
        AttributeKind.Defense => new SpecialAttribute(new Defense(value)),
        AttributeKind.WeaponStrength => new SpecialAttribute(new WeaponStrength(value)),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// Combines multiple attribute deltas (from Base, Equipment, Talents, Boons) per <see cref="AttributeKind"/>
    /// using <see cref="StatValue{Double}"/> addition, preserving separate Base, FlatModifier, and IncreasedPercent buckets.
    /// </summary>
    public static ImmutableArray<CharacterAttribute> Aggregate(IEnumerable<CharacterAttribute> attributes)
    {
        var totals = new Dictionary<AttributeKind, StatValue<double>>();

        foreach (var attr in attributes)
        {
            var (kind, stat) = attr.Deconstruct();
            totals[kind] = totals.TryGetValue(kind, out var existing)
                ? existing + stat
                : stat;
        }

        return [.. totals.Select(kvp => FromKind(kvp.Key, kvp.Value))];
    }
}
