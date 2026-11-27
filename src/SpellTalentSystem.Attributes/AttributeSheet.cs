using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Attributes;

/// <summary>
/// Immutable, resolved character attribute sheet storing enriched <see cref="StatValue{T}"/>
/// buckets for every Primary, Secondary, Derived, and Special attribute.
/// </summary>
public sealed record AttributeSheet(ImmutableDictionary<CharacterAttributeId, StatValue<double>> Stats)
{
    public StatValue<double> GetStat(CharacterAttributeId id) =>
        Stats.TryGetValue(id, out var stat) ? stat : StatValue<double>.FromBase(0.0);

    public double GetResolved(CharacterAttributeId id) =>
        GetStat(id).Resolve();

    /// <summary>
    /// Evaluates a complete <see cref="AttributeSheet"/> in three deterministic passes:
    /// 1. Baseline + direct commutative modifiers on Primary, Secondary, Special, and Derived overrides.
    /// 2. Non-chaining GW2 stat conversions (<see cref="AttributeConversion"/>) added as flat bonuses.
    /// 3. GW2 Derived Attribute formulas (Armor, MaxHealth, CritChance, CritDamage, Condition/Boon Duration).
    /// </summary>
    public static AttributeSheet Resolve(
        CharacterBaseline baseline,
        IEnumerable<AttributeModifier> modifiers,
        IEnumerable<AttributeConversion>? conversions = null)
    {
        var builder = ImmutableDictionary.CreateBuilder<CharacterAttributeId, StatValue<double>>();

        // Seed Primary attributes (level-scaled)
        CharacterAttributeId powerId = new PrimaryAttribute(new Power());
        CharacterAttributeId precisionId = new PrimaryAttribute(new Precision());
        CharacterAttributeId toughnessId = new PrimaryAttribute(new Toughness());
        CharacterAttributeId vitalityId = new PrimaryAttribute(new Vitality());

        builder[powerId] = StatValue<double>.FromBase(baseline.BasePrimaryStat);
        builder[precisionId] = StatValue<double>.FromBase(baseline.BasePrimaryStat);
        builder[toughnessId] = StatValue<double>.FromBase(baseline.BasePrimaryStat);
        builder[vitalityId] = StatValue<double>.FromBase(baseline.BasePrimaryStat);

        // Seed Secondary attributes (base 0)
        CharacterAttributeId ferocityId = new SecondaryAttribute(new Ferocity());
        CharacterAttributeId condDmgId = new SecondaryAttribute(new ConditionDamage());
        CharacterAttributeId expertiseId = new SecondaryAttribute(new Expertise());
        CharacterAttributeId concentrationId = new SecondaryAttribute(new Concentration());
        CharacterAttributeId healingPowerId = new SecondaryAttribute(new HealingPower());

        builder[ferocityId] = StatValue<double>.FromBase(0.0);
        builder[condDmgId] = StatValue<double>.FromBase(0.0);
        builder[expertiseId] = StatValue<double>.FromBase(0.0);
        builder[concentrationId] = StatValue<double>.FromBase(0.0);
        builder[healingPowerId] = StatValue<double>.FromBase(0.0);

        // Seed Special defaults
        CharacterAttributeId defenseId = new SpecialAttribute(new Defense());
        CharacterAttributeId moveSpeedId = new SpecialAttribute(new MovementSpeed());
        CharacterAttributeId healthPctId = new SpecialAttribute(new HealthPercent());
        CharacterAttributeId endurancePctId = new SpecialAttribute(new EndurancePercent());

        builder[defenseId] = StatValue<double>.FromBase(0.0);
        builder[moveSpeedId] = StatValue<double>.FromBase(1.0);
        builder[healthPctId] = StatValue<double>.FromBase(1.0);
        builder[endurancePctId] = StatValue<double>.FromBase(1.0);

        // Pass 1: Apply all direct commutative modifiers
        foreach (var mod in modifiers)
        {
            var current = builder.TryGetValue(mod.Attribute, out var existing)
                ? existing
                : StatValue<double>.FromBase(0.0);
            builder[mod.Attribute] = mod.ApplyTo(current);
        }

        // Pass 2: Apply GW2 conversions from Pass 1 resolved snapshots (non-chaining)
        if (conversions is not null)
        {
            var preConversionSnapshot = builder.ToImmutable();
            foreach (var conv in conversions)
            {
                double sourceResolved = preConversionSnapshot.TryGetValue(conv.SourceAttribute, out var srcStat)
                    ? srcStat.Resolve()
                    : 0.0;

                double grantedFlat = sourceResolved * conv.Ratio;
                if (grantedFlat == 0.0)
                    continue;

                var targetStat = builder.TryGetValue(conv.TargetAttribute, out var existingTarget)
                    ? existingTarget
                    : StatValue<double>.FromBase(0.0);
                builder[conv.TargetAttribute] = targetStat.AddFlat(grantedFlat);
            }
        }

        // Pass 3: Compute GW2 Derived Attributes from resolved Primary/Secondary stats
        // (preserving any direct flat/%/more modifiers placed on Derived attributes by traits/boons)
        double toughness = builder[toughnessId].Resolve();
        double defense = builder[defenseId].Resolve();
        double vitality = builder[vitalityId].Resolve();
        double precision = builder[precisionId].Resolve();
        double ferocity = builder[ferocityId].Resolve();
        double expertise = builder[expertiseId].Resolve();
        double concentration = builder[concentrationId].Resolve();

        SetDerivedBase(builder, new DerivedAttribute(new Armor()), toughness + defense);
        SetDerivedBase(builder, new DerivedAttribute(new MaxHealth()), baseline.ProfessionBaseHealth + (vitality * 10.0));
        SetDerivedBase(builder, new DerivedAttribute(new CriticalChance()), Math.Clamp(0.05 + ((precision - 1000.0) / 2100.0), 0.0, 1.0));
        SetDerivedBase(builder, new DerivedAttribute(new CriticalDamage()), 1.50 + (ferocity / 1500.0));
        SetDerivedBase(builder, new DerivedAttribute(new ConditionDuration()), Math.Min(1.0, expertise / 1500.0));
        SetDerivedBase(builder, new DerivedAttribute(new BoonDuration()), Math.Min(1.0, concentration / 1500.0));

        return new AttributeSheet(builder.ToImmutable());
    }

    private static void SetDerivedBase(
        ImmutableDictionary<CharacterAttributeId, StatValue<double>>.Builder builder,
        CharacterAttributeId id,
        double computedBase)
    {
        builder[id] = builder.TryGetValue(id, out var existingWithTraitBonuses)
            ? existingWithTraitBonuses with { Base = computedBase }
            : StatValue<double>.FromBase(computedBase);
    }
}
