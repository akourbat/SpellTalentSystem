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
        IEnumerable<Item> equippedItems,
        IEnumerable<ItemSetDefinition>? setDefinitions = null)
    {
        var rawAttributes = ImmutableArray.CreateBuilder<CharacterAttribute>();
        var talents = ImmutableArray.CreateBuilder<Talent>();
        var setCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var root in equippedItems)
        {
            CollectFromNode(root, rawAttributes, talents, setCounts);
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

    private static void CollectFromNode(
        Item node,
        ImmutableArray<CharacterAttribute>.Builder rawAttributes,
        ImmutableArray<Talent>.Builder talents,
        Dictionary<string, int> setCounts)
    {
        if (!string.IsNullOrWhiteSpace(node.Lifecycle.SetId))
        {
            setCounts[node.Lifecycle.SetId] = setCounts.GetValueOrDefault(node.Lifecycle.SetId) + 1;
        }

        ApplyEffects(node.Effects, rawAttributes, talents);

        if (node is ContainerItem container)
        {
            foreach (var child in container.Children)
            {
                CollectFromNode(child, rawAttributes, talents, setCounts);
            }
        }
    }

    private static void ApplyEffects(
        ImmutableArray<ItemEffect> effects,
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
