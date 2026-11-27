using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpellTalentSystem.Abilities;
using SpellTalentSystem.Pipeline;

namespace SpellTalentSystem.Items;

public sealed record AggregatedEquipment(
    ImmutableArray<ItemStatBonus> AggregatedStats,
    ImmutableArray<Talent> GrantedTalents,
    ImmutableDictionary<string, int> ActiveSetPieceCounts)
{
    public double GetStatTotal(CharacterAttributeId attribute) =>
        AggregatedStats.FirstOrDefault(s => s.Attribute.Equals(attribute))?.Value ?? 0.0;
}

public static class EquipmentAggregator
{
    public static AggregatedEquipment Aggregate(
        IEnumerable<Item> equippedItems,
        IEnumerable<ItemSetDefinition>? setDefinitions = null)
    {
        var statTotals = new Dictionary<CharacterAttributeId, double>();
        var talents = ImmutableArray.CreateBuilder<Talent>();
        var setCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var root in equippedItems)
        {
            CollectFromNode(root, statTotals, talents, setCounts);
        }

        if (setDefinitions is not null)
        {
            foreach (var set in setDefinitions)
            {
                if (!setCounts.TryGetValue(set.SetId, out var count) || count <= 0)
                    continue;

                foreach (var tier in set.Tiers.Where(t => count >= t.RequiredPieces).OrderBy(t => t.RequiredPieces))
                {
                    ApplyEffects(tier.Effects, statTotals, talents);
                }
            }
        }

        return new AggregatedEquipment(
            AggregatedStats: [.. statTotals.Select(kvp => new ItemStatBonus(kvp.Key, kvp.Value))],
            GrantedTalents: talents.ToImmutable(),
            ActiveSetPieceCounts: setCounts.ToImmutableDictionary());
    }

    private static void CollectFromNode(
        Item node,
        Dictionary<CharacterAttributeId, double> statTotals,
        ImmutableArray<Talent>.Builder talents,
        Dictionary<string, int> setCounts)
    {
        if (!string.IsNullOrWhiteSpace(node.Lifecycle.SetId))
        {
            setCounts[node.Lifecycle.SetId] = setCounts.GetValueOrDefault(node.Lifecycle.SetId) + 1;
        }

        ApplyEffects(node.Effects, statTotals, talents);

        if (node is ContainerItem container)
        {
            foreach (var child in container.Children)
            {
                CollectFromNode(child, statTotals, talents, setCounts);
            }
        }
    }

    private static void ApplyEffects(
        ImmutableArray<ItemEffect> effects,
        Dictionary<CharacterAttributeId, double> statTotals,
        ImmutableArray<Talent>.Builder talents)
    {
        foreach (var effect in effects)
        {
            switch (effect.Value)
            {
                case ItemStatBonus stat:
                    statTotals[stat.Attribute] = statTotals.GetValueOrDefault(stat.Attribute) + stat.Value;
                    break;

                case GrantedTalent granted:
                    talents.Add(granted.Talent);
                    break;
            }
        }
    }
}
