using System.Collections.Immutable;

namespace SpellTalentSystem.Abilities;

/// <summary>
/// Closed abstract root of the ability hierarchy. Every node in an ability tree performs
/// exactly one concrete <see cref="Effect"/> and can hold zero or more child
/// <see cref="SubAbilities"/> (which may be either <see cref="DependentAbility"/> nodes
/// or trait-spawned <see cref="PrimaryAbility"/> nodes with their own cooldowns).
/// </summary>
public closed record Ability
{
    private protected Ability(
        string id,
        string name,
        AbilityNature nature,
        AbilityTargeting targeting,
        AbilityDelivery delivery,
        AbilityEffect effect,
        ImmutableArray<Ability> subAbilities = default)
    {
        Id = id;
        Name = name;
        Nature = nature;
        Targeting = targeting;
        Delivery = delivery;
        Effect = effect;
        SubAbilities = subAbilities.IsDefault ? ImmutableArray<Ability>.Empty : subAbilities;
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public AbilityNature Nature { get; init; }
    public AbilityTargeting Targeting { get; init; }
    public AbilityDelivery Delivery { get; init; }
    public AbilityEffect Effect { get; init; }
    public ImmutableArray<Ability> SubAbilities { get; init; }
}

/// <summary>
/// Player-invoked root ability or independent trait-proc'd ability that carries its own
/// cast time and cooldown (or internal cooldown).
/// </summary>
public sealed record PrimaryAbility : Ability
{
    public PrimaryAbility(
        string id,
        string name,
        AbilityNature nature,
        AbilityTargeting targeting,
        AbilityDelivery delivery,
        AbilityEffect effect,
        StatValue<double> castTimeSeconds,
        StatValue<double> cooldownSeconds,
        ImmutableArray<Ability> subAbilities = default)
        : base(id, name, nature, targeting, delivery, effect, subAbilities)
    {
        CastTimeSeconds = castTimeSeconds;
        CooldownSeconds = cooldownSeconds;
    }

    public StatValue<double> CastTimeSeconds { get; init; }
    public StatValue<double> CooldownSeconds { get; init; }
}

/// <summary>
/// Attached or triggered sub-ability that fires as part of a parent ability's resolution
/// without an independent cast time or cooldown.
/// </summary>
public sealed record DependentAbility : Ability
{
    public DependentAbility(
        string id,
        string name,
        AbilityNature nature,
        AbilityTargeting targeting,
        AbilityDelivery delivery,
        AbilityEffect effect,
        ImmutableArray<Ability> subAbilities = default)
        : base(id, name, nature, targeting, delivery, effect, subAbilities)
    { }
}
