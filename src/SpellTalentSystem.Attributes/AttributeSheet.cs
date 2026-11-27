using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Attributes;

/// <summary>
/// Holds composite <see cref="StatValue{Double}"/> buckets aggregated from Base/Leveling,
/// Equipment, Talents, and Boons, resolving final numbers only when queried.
/// </summary>
public sealed record AttributeSheet
{
    public AttributeSheet(IEnumerable<CharacterAttribute>? attributes = null)
    {
        var aggregated = attributes is null
            ? ImmutableArray<CharacterAttribute>.Empty
            : CharacterAttribute.Aggregate(attributes);

        Attributes = aggregated.ToImmutableDictionary(a => a.Kind, a => a.Stat);
    }

    public ImmutableDictionary<AttributeKind, StatValue<double>> Attributes { get; init; }

    public StatValue<double> GetBucket(AttributeKind kind) =>
        Attributes.TryGetValue(kind, out var stat) ? stat : default;

    public double GetResolved(AttributeKind kind) =>
        GetBucket(kind).Resolve();

    /// <summary>
    /// Combines this sheet with additional attribute deltas (e.g., from Equipment, Talents, or Boons),
    /// keeping Base, Flat, and % buckets separate.
    /// </summary>
    public AttributeSheet CombineWith(IEnumerable<CharacterAttribute> deltas)
    {
        var current = Attributes.Select(kvp => CharacterAttribute.FromKind(kvp.Key, kvp.Value));
        return new AttributeSheet(current.Concat(deltas));
    }

    // --- Final Derived Combat Resolutions ---

    public double GetPower() => GetResolved(AttributeKind.Power);
    public double GetPrecision() => GetResolved(AttributeKind.Precision);
    public double GetToughness() => GetResolved(AttributeKind.Toughness);
    public double GetVitality() => GetResolved(AttributeKind.Vitality);
    public double GetFerocity() => GetResolved(AttributeKind.Ferocity);
    public double GetConditionDamage() => GetResolved(AttributeKind.ConditionDamage);
    public double GetHealingPower() => GetResolved(AttributeKind.HealingPower);
    public double GetExpertise() => GetResolved(AttributeKind.Expertise);
    public double GetConcentration() => GetResolved(AttributeKind.Concentration);

    /// <summary>
    /// Resolves total Critical Chance: CriticalChance bucket (base + flat% + inc%)
    /// plus Precision conversion ((Precision - 1000) / 2100.0).
    /// </summary>
    public double GetCriticalChance()
    {
        var baseCrit = GetResolved(AttributeKind.CriticalChance);
        var precisionBonus = Math.Max(0.0, GetPrecision() - 1000.0) / 2100.0;
        return Math.Clamp(baseCrit + precisionBonus, 0.0, 1.0);
    }

    /// <summary>
    /// Resolves total Critical Damage multiplier: CriticalDamage bucket (default 1.50)
    /// plus Ferocity conversion (Ferocity / 1500.0).
    /// </summary>
    public double GetCriticalDamageMultiplier()
    {
        var baseCritDmg = GetResolved(AttributeKind.CriticalDamage);
        var ferocityBonus = GetFerocity() / 1500.0;
        return baseCritDmg + ferocityBonus;
    }

    /// <summary>
    /// Resolves total Boon Duration % bonus: BoonDuration bucket + Concentration conversion (Concentration / 1500.0).
    /// </summary>
    public double GetBoonDurationBonus()
    {
        var directBonus = GetResolved(AttributeKind.BoonDuration);
        var concentrationBonus = GetConcentration() / 1500.0;
        return directBonus + concentrationBonus;
    }

    /// <summary>
    /// Resolves total Condition Duration % bonus: ConditionDuration bucket + Expertise conversion (Expertise / 1500.0).
    /// </summary>
    public double GetConditionDurationBonus()
    {
        var directBonus = GetResolved(AttributeKind.ConditionDuration);
        var expertiseBonus = GetExpertise() / 1500.0;
        return directBonus + expertiseBonus;
    }

    public double GetArmor() => GetToughness() + GetResolved(AttributeKind.Defense);

    public double GetMaxHealth(double baseClassHealth = 1000.0) =>
        baseClassHealth + (GetVitality() * 10.0);

    /// <summary>
    /// Resolves the final duration of an <see cref="ApplyBoon"/> effect by combining the ability's
    /// composite <see cref="ApplyBoon.Duration"/> with the caster's resolved <see cref="GetBoonDurationBonus"/>.
    /// </summary>
    public TimeSpan ResolveBoonDuration(ApplyBoon boonEffect)
    {
        var skillDuration = boonEffect.Duration.Resolve();
        var multiplier = 1.0 + GetBoonDurationBonus();
        return TimeSpan.FromTicks((long)Math.Round(skillDuration.Ticks * multiplier));
    }

    public static AttributeSheet CreateLevel80Base() => new AttributeSheet(
    [
        new PrimaryAttribute(new Power(StatValue<double>.FromBase(1000))),
        new PrimaryAttribute(new Precision(StatValue<double>.FromBase(1000))),
        new PrimaryAttribute(new Toughness(StatValue<double>.FromBase(1000))),
        new PrimaryAttribute(new Vitality(StatValue<double>.FromBase(1000))),
        new SpecialAttribute(new CriticalChance(StatValue<double>.FromBase(0.05))),
        new SpecialAttribute(new CriticalDamage(StatValue<double>.FromBase(1.50)))
    ]);
}
