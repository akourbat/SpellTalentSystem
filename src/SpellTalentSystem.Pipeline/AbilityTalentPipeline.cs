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
/// Single-subscription, stateless reactive DynamicData pipeline that combines base
/// <see cref="PrimaryAbility"/> snapshots with sorted <see cref="Talent"/> snapshots
/// and diffs the resulting <see cref="ResolvedAbilityNode"/> trees into an <see cref="IChangeSet{TObject, TKey}"/>.
/// </summary>
public sealed class AbilityTalentPipeline : IDisposable
{
    private readonly SourceCache<PrimaryAbility, string> _baseAbilities = new(a => a.Id);
    private readonly SourceCache<Talent, string> _activeTalents = new(t => t.Id);

    public IObservable<IChangeSet<ImmutableArray<ResolvedAbilityNode>, string>> ResolvedAbilityTrees { get; }

    public AbilityTalentPipeline()
    {
        var sortComparer = Comparer<Talent>.Create((a, b) =>
        {
            int priorityComparison = a.Priority.CompareTo(b.Priority);
            return priorityComparison != 0
                ? priorityComparison
                : StringComparer.Ordinal.Compare(a.Id, b.Id);
        });

        IObservable<IEnumerable<PrimaryAbility>> abilitiesStream = _baseAbilities.Connect()
            .QueryWhenChanged(query => query.Items)
            .StartWith(Array.Empty<PrimaryAbility>());


        IObservable<IReadOnlyList<Talent>> sortedTalentsStream = _activeTalents.Connect()
            .SortAndBind(out var sortedTalents, sortComparer)
            .QueryWhenChanged(_ => (IReadOnlyList<Talent>)sortedTalents)
            .StartWith(Array.Empty<Talent>());

        ResolvedAbilityTrees = abilitiesStream
            .CombineLatest(
                sortedTalentsStream,
                (abilities, talents) => abilities
                    .Select(ability => ApplyPreOrderedTalentsAndExpand(ability, talents))
                    .ToList())
            .ToObservableChangeSet(tree => tree.ElementAt(0).Ability.Id);
    }

    public void AddOrUpdateAbility(PrimaryAbility ability) => _baseAbilities.AddOrUpdate(ability);
    public void RemoveAbility(string abilityId) => _baseAbilities.Remove(abilityId);

    public void EquipTalent(Talent talent) => _activeTalents.AddOrUpdate(talent);
    public void UnequipTalent(string talentId) => _activeTalents.Remove(talentId);

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

    public void Dispose()
    {
        _baseAbilities.Dispose();
        _activeTalents.Dispose();
    }
}
