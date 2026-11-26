using System;
using System.Collections.Immutable;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Pipeline;

public static class AbilityExtensions
{
    public static bool IsElement(this Ability ability, ElementType element) =>
        ability.Nature.Value is ElementalNature(var e) && e.Equals(element);

    public static bool IsPhysical(this Ability ability, PhysicalDamageType? physicalType = null) =>
        ability.Nature.Value is PhysicalNature(var p) &&
        (physicalType is null || p.Equals(physicalType.Value));

    public static bool IsAoE(this Ability ability) =>
        ability.Targeting.Value is
            PointBlankAoE or
            TargetCenteredAoE or
            GroundTargetedAoE or
            ConeTarget or
            LineTarget;

    public static bool TargetsEnemies(this Ability ability) =>
        ability.Targeting.Value switch
        {
            SingleTarget(_, var allegiance) => allegiance.Value is Enemies or Both,
            PointBlankAoE(_, _, var allegiance) => allegiance.Value is Enemies or Both,
            TargetCenteredAoE(_, _, var allegiance) => allegiance.Value is Enemies or Both,
            GroundTargetedAoE(_, _, _, var allegiance) => allegiance.Value is Enemies or Both,
            ConeTarget(_, _, _, var allegiance) => allegiance.Value is Enemies or Both,
            LineTarget(_, _, _, var allegiance) => allegiance.Value is Enemies or Both,
            _ => false
        };

    public static Ability WithSubAbilities(this Ability ability, ImmutableArray<Ability> subAbilities) =>
        ability switch
        {
            PrimaryAbility p => p with { SubAbilities = subAbilities },
            DependentAbility d => d with { SubAbilities = subAbilities },
        };

    public static Ability AddSubAbility(this Ability ability, Ability child) =>
        ability.WithSubAbilities(ability.SubAbilities.Add(child));

    /// <summary>
    /// Transforms this ability's <see cref="AbilityEffect"/> if <paramref name="predicate"/> matches.
    /// </summary>
    public static Ability MapEffect(
        this Ability ability,
        Func<Ability, bool> predicate,
        Func<AbilityEffect, AbilityEffect> transform)
    {
        if (!predicate(ability))
            return ability;

        var updatedEffect = transform(ability.Effect);
        if (updatedEffect.Equals(ability.Effect))
            return ability;

        return ability switch
        {
            PrimaryAbility p => p with { Effect = updatedEffect },
            DependentAbility d => d with { Effect = updatedEffect },
            _ => ability
        };
    }

    /// <summary>
    /// Transforms this ability's <see cref="AbilityTargeting"/> if <paramref name="predicate"/> matches.
    /// </summary>
    public static Ability MapTargeting(
        this Ability ability,
        Func<Ability, bool> predicate,
        Func<AbilityTargeting, AbilityTargeting> transform)
    {
        if (!predicate(ability))
            return ability;

        var updatedTargeting = transform(ability.Targeting);
        if (updatedTargeting.Equals(ability.Targeting))
            return ability;

        return ability switch
        {
            PrimaryAbility p => p with { Targeting = updatedTargeting },
            DependentAbility d => d with { Targeting = updatedTargeting },
            _ => ability
        };
    }

    /// <summary>
    /// Transforms a <see cref="PrimaryAbility"/>'s cast time and/or cooldown if <paramref name="predicate"/> matches.
    /// </summary>
    public static Ability MapPrimaryTiming(
        this Ability ability,
        Func<PrimaryAbility, bool> predicate,
        Func<PrimaryAbility, PrimaryAbility> transform)
    {
        if (ability is PrimaryAbility primary && predicate(primary))
            return transform(primary);

        return ability;
    }

    /// <summary>
    /// Recursively applies <paramref name="transform"/> bottom-up across the entire ability tree,
    /// preserving reference equality if nothing changed.
    /// </summary>
    public static Ability TransformTree(this Ability root, Func<Ability, Ability> transform)
    {
        var originalSubs = root.SubAbilities;
        var changed = false;
        ImmutableArray<Ability>.Builder? builder = null;

        for (int i = 0; i < originalSubs.Length; i++)
        {
            var origChild = originalSubs[i];
            var nextChild = origChild.TransformTree(transform);

            if (!ReferenceEquals(origChild, nextChild) && !changed)
            {
                changed = true;
                builder = ImmutableArray.CreateBuilder<Ability>(originalSubs.Length);
                for (int j = 0; j < i; j++)
                    builder.Add(originalSubs[j]);
            }

            builder?.Add(nextChild);
        }

        var withUpdatedSubs = changed
            ? root.WithSubAbilities(builder!.MoveToImmutable())
            : root;

        return transform(withUpdatedSubs);
    }
}
