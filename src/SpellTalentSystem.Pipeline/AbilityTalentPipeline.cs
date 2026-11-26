using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Pipeline;

/// <summary>
/// Stateless, purely functional DynamicData pipeline extensions that combine
/// base <see cref="PrimaryAbility"/> change sets with <see cref="Talent"/> change sets
/// and emit flattened <see cref="ResolvedAbilityNode"/> trees.
/// </summary>
public static class AbilityTalentPipeline
{
    private static readonly IComparer<Talent> TalentSortComparer = Comparer<Talent>.Create((a, b) =>
    {
        int priorityComparison = a.Priority.CompareTo(b.Priority);
        return priorityComparison != 0
            ? priorityComparison
            : StringComparer.Ordinal.Compare(a.Id, b.Id);
    });

    /// <summary>
    /// Reactively enriches <paramref name="abilities"/> using the active <paramref name="talents"/> stream
    /// and emits an <see cref="IChangeSet{TObject, TKey}"/> of flattened <see cref="ResolvedAbilityNode"/> trees.
    /// Owns no caches and requires no disposal.
    /// </summary>
    public static IObservable<IChangeSet<ImmutableArray<ResolvedAbilityNode>, string>> ResolveWithTalents(
        this IObservable<IChangeSet<PrimaryAbility, string>> abilities,
        IObservable<IChangeSet<Talent, string>> talents)
    {
        var abilitiesStream = abilities
            .QueryWhenChanged(query => query.Items)
            .StartWith(Array.Empty<PrimaryAbility>());

        var sortedTalentsStream = talents
            .SortAndBind(out var sortedTalents, TalentSortComparer)
            .QueryWhenChanged(_ => (IReadOnlyList<Talent>)sortedTalents)
            .StartWith(Array.Empty<Talent>());

        return abilitiesStream
            .CombineLatest(
                sortedTalentsStream,
                (currentAbilities, currentTalents) => currentAbilities
                    .Select(ability => ApplyPreOrderedTalentsAndExpand(ability, currentTalents))
                    .ToList())
            .ToObservableChangeSet(tree => tree.ElementAt(0).Ability.Id);
    }

    public static ImmutableArray<Talent> OrderTalents(IEnumerable<Talent> talents) =>
        talents
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Id, StringComparer.Ordinal)
            .ToImmutableArray();

    public static ImmutableArray<ResolvedAbilityNode> ApplyTalentsAndExpand(
        PrimaryAbility baseAbility,
        IEnumerable<Talent> talents) =>
        ApplyPreOrderedTalentsAndExpand(baseAbility, OrderTalents(talents));

    private static ImmutableArray<ResolvedAbilityNode> ApplyPreOrderedTalentsAndExpand(
        PrimaryAbility baseAbility,
        IEnumerable<Talent> orderedTalents)
    {
        Ability current = baseAbility;
        var appliedIds = ImmutableArray.CreateBuilder<string>();

        foreach (var talent in orderedTalents)
        {
            var next = talent.Apply(current);
            if (!ReferenceEquals(current, next))
            {
                appliedIds.Add(talent.Id);
                current = next;
            }
        }

        var talentIds = appliedIds.ToImmutable();
        var nodes = ImmutableArray.CreateBuilder<ResolvedAbilityNode>();
        var ancestorPath = ImmutableList<string>.Empty;
        ExpandNode(current, depth: 0, parentId: null, talentIds, ancestorPath, nodes);
        return nodes.ToImmutable();
    }

    private static void ExpandNode(
        Ability node,
        int depth,
        string? parentId,
        ImmutableArray<string> appliedTalentIds,
        ImmutableList<string> ancestorPath,
        ImmutableArray<ResolvedAbilityNode>.Builder output)
    {
        if (ancestorPath.Contains(node.Id))
        {
            var cycleTrace = string.Join(" -> ", ancestorPath.Add(node.Id));
            throw new InvalidOperationException(
                $"Circular sub-ability reference detected while expanding '{ancestorPath.ElementAt(0)}': {cycleTrace}");
        }

        var currentPath = ancestorPath.Add(node.Id);
        output.Add(new ResolvedAbilityNode(node, depth, parentId, appliedTalentIds));

        foreach (var child in node.SubAbilities)
        {
            ExpandNode(child, depth + 1, node.Id, appliedTalentIds, currentPath, output);
        }
    }
}
