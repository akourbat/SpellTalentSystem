using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpellTalentSystem.Abilities;
using SpellTalentSystem.Pipeline;

namespace SpellTalentSystem.Items;

public sealed record AggregatedEquipment(
    ImmutableArray<CharacterAttribute> AggregatedAttributes,
    ImmutableArray<Talent> GrantedTalents,
    ImmutableDictionary<string, int> ActiveSetPieceCounts)
{
    public StatValue<double> GetAttributeBucket(AttributeKind kind)
    {
        var match = AggregatedAttributes.FirstOrDefault(a => a.Value is not null && a.Kind == kind);
        return match.Value is null ? default : match.Stat;
    }
}

public static class EquipmentAggregator
{
    public static AggregatedEquipment Aggregate(
        IEnumerable<Gear> equippedGear,
        IEnumerable<ItemSetDefinition>? setDefinitions = null)
    {
        var rawAttributes = ImmutableArray.CreateBuilder<CharacterAttribute>();
        var talents = ImmutableArray.CreateBuilder<Talent>();
        var setCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var gear in equippedGear)
        {
            CollectFromGear(gear, rawAttributes, talents, setCounts);
        }

        if (setDefinitions is not null)
        {
            foreach (var set in setDefinitions)
            {
                if (!setCounts.TryGetValue(set.SetId, out var count) || count <= 0)
                    continue;

                foreach (var tier in set.Tiers.Where(t => count >= t.RequiredPieces).OrderBy(t => t.RequiredPieces))
                {
                    ApplyEffects(tier.Effects, rawAttributes, talents);
                }
            }
        }

        return new AggregatedEquipment(
            AggregatedAttributes: CharacterAttribute.Aggregate(rawAttributes),
            GrantedTalents: talents.ToImmutable(),
            ActiveSetPieceCounts: setCounts.ToImmutableDictionary());
    }

    private static void CollectFromGear(
        Gear gear,
        ImmutableArray<CharacterAttribute>.Builder rawAttributes,
        ImmutableArray<Talent>.Builder talents,
        Dictionary<string, int> setCounts)
    {
        IncrementSetCount(gear.SetId, setCounts);
        ApplyEffects(gear.BaseEffects, rawAttributes, talents);

        foreach (var slot in gear.UpgradeSlots)
        {
            if (slot.Slotted is not null)
            {
                CollectFromUpgrade(slot.Slotted, rawAttributes, talents, setCounts);
            }
        }
    }

    private static void CollectFromUpgrade(
        GearUpgrade upgrade,
        ImmutableArray<CharacterAttribute>.Builder rawAttributes,
        ImmutableArray<Talent>.Builder talents,
        Dictionary<string, int> setCounts)
    {
        IncrementSetCount(upgrade.SetId, setCounts);
        ApplyEffects(upgrade.Effects, rawAttributes, talents);

        foreach (var slot in upgrade.UpgradeSlots)
        {
            if (slot.Slotted is not null)
            {
                CollectFromUpgrade(slot.Slotted, rawAttributes, talents, setCounts);
            }
        }
    }

    private static void IncrementSetCount(string? setId, Dictionary<string, int> setCounts)
    {
        if (!string.IsNullOrWhiteSpace(setId))
        {
            setCounts[setId] = setCounts.GetValueOrDefault(setId) + 1;
        }
    }

    private static void ApplyEffects(
        IEnumerable<ItemEffect> effects,
        ImmutableArray<CharacterAttribute>.Builder rawAttributes,
        ImmutableArray<Talent>.Builder talents)
    {
        foreach (var effect in effects)
        {
            switch (effect.Value)
            {
                case CharacterAttribute attr:
                    rawAttributes.Add(attr);
                    break;

                case GrantedTalent granted:
                    talents.Add(granted.Talent);
                    break;
            }
        }
    }
}
