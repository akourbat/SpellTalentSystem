using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Pipeline;

/// <summary>
/// Reactive DynamicData pipeline that combines base <see cref="PrimaryAbility"/> definitions
/// with active <see cref="Talent"/> modifiers and emits flattened <see cref="ResolvedAbilityNode"/> trees.
/// </summary>
public sealed class AbilityTalentPipeline : IDisposable
{
    private readonly SourceCache<PrimaryAbility, string> _baseAbilities = new(a => a.Id);
    private readonly SourceCache<Talent, string> _activeTalents = new(t => t.Id);
    private readonly BehaviorSubject<ImmutableArray<Talent>> _orderedTalents = new(ImmutableArray<Talent>.Empty);
    private readonly IDisposable _talentSubscription;

    public IObservable<IChangeSet<ImmutableArray<ResolvedAbilityNode>, string>> ResolvedAbilityTrees { get; }

    public AbilityTalentPipeline()
    {
        _talentSubscription = _activeTalents.Connect()
            .ToCollection()
            .Select(OrderTalents)
            .Subscribe(_orderedTalents);

        ResolvedAbilityTrees = _baseAbilities.Connect()
            .AutoRefreshOnObservable(_ => _orderedTalents)
            .Transform(
                ability => ApplyPreOrderedTalentsAndExpand(ability, _orderedTalents.Value),
                transformOnRefresh: true)
            .DistinctUntilChanged();
    }

    public void AddOrUpdateAbility(PrimaryAbility ability) => _baseAbilities.AddOrUpdate(ability);
    public void RemoveAbility(string abilityId) => _baseAbilities.Remove(abilityId);

    public void EquipTalent(Talent talent) => _activeTalents.AddOrUpdate(talent);
    public void UnequipTalent(string talentId) => _activeTalents.Remove(talentId);

    /// <summary>
    /// Orders talents deterministically by <see cref="Talent.Priority"/> ascending,
    /// using ordinal <see cref="Talent.Id"/> comparison as a tie-breaker.
    /// </summary>
    public static ImmutableArray<Talent> OrderTalents(IEnumerable<Talent> talents) =>
        talents
            .OrderBy(t => t.Priority)
            .ThenBy(t => t.Id, StringComparer.Ordinal)
            .ToImmutableArray();

    /// <summary>
    /// Sorts <paramref name="talents"/> by priority, applies them to <paramref name="baseAbility"/>,
    /// and flattens the resulting tree into <see cref="ResolvedAbilityNode"/> entries.
    /// </summary>
    public static ImmutableArray<ResolvedAbilityNode> ApplyTalentsAndExpand(
        PrimaryAbility baseAbility,
        IEnumerable<Talent> talents) =>
        ApplyPreOrderedTalentsAndExpand(baseAbility, OrderTalents(talents));

    private static ImmutableArray<ResolvedAbilityNode> ApplyPreOrderedTalentsAndExpand(
        PrimaryAbility baseAbility,
        ImmutableArray<Talent> orderedTalents)
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
        ExpandNode(current, depth: 0, parentId: null, talentIds, nodes);
        return nodes.ToImmutable();
    }

    private static void ExpandNode(
        Ability node,
        int depth,
        string? parentId,
        ImmutableArray<string> appliedTalentIds,
        ImmutableArray<ResolvedAbilityNode>.Builder output)
    {
        output.Add(new ResolvedAbilityNode(node, depth, parentId, appliedTalentIds));

        foreach (var child in node.SubAbilities)
        {
            ExpandNode(child, depth + 1, node.Id, appliedTalentIds, output);
        }
    }

    public void Dispose()
    {
        _talentSubscription.Dispose();
        _orderedTalents.Dispose();
        _baseAbilities.Dispose();
        _activeTalents.Dispose();
    }
}
