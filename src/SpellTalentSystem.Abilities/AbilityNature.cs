namespace SpellTalentSystem.Abilities;

// Physical damage types
public readonly record struct Slashing;
public readonly record struct Piercing;
public readonly record struct Bludgeoning;
public union PhysicalDamageType(Slashing, Piercing, Bludgeoning);

// Elements
public readonly record struct Fire;
public readonly record struct Frost;
public readonly record struct Lightning;
public readonly record struct Earth;
public readonly record struct Water;
public readonly record struct Air;
public readonly record struct Arcane;
public readonly record struct Shadow;
public union ElementType(Fire, Frost, Lightning, Earth, Water, Air, Arcane, Shadow);

// Hard controls
public readonly record struct Stun;
public readonly record struct Daze;
public readonly record struct Knockdown;
public readonly record struct Pull;
public readonly record struct Knockback;
public readonly record struct Launch;
public readonly record struct Float;
public readonly record struct Sink;
public readonly record struct Fear;
public readonly record struct Taunt;
public union HardControlType(Stun, Daze, Knockdown, Pull, Knockback, Launch, Float, Sink, Fear, Taunt);

// Soft controls
public readonly record struct Chilled;
public readonly record struct Crippled;
public readonly record struct Immobilized;
public readonly record struct Slow;
public readonly record struct Weakness;
public readonly record struct Blind;
public union SoftControlType(Chilled, Crippled, Immobilized, Slow, Weakness, Blind);

// Healing kinds
public readonly record struct DirectHealing;
public readonly record struct OverTimeHealing;
public readonly record struct BarrierHealing;
public readonly record struct LifeStealHealing;
public union HealingType(DirectHealing, OverTimeHealing, BarrierHealing, LifeStealHealing);

// Defensive kinds
public readonly record struct Block;
public readonly record struct Evade;
public readonly record struct Invulnerability;
public readonly record struct Reflect;
public readonly record struct Absorb;
public readonly record struct DamageReduction;
public readonly record struct Stunbreak;
public readonly record struct Cleanse;
public readonly record struct BoonStrip;
public union DefensiveType(Block, Evade, Invulnerability, Reflect, Absorb, DamageReduction, Stunbreak, Cleanse, BoonStrip);

// Combo roles
public readonly record struct ComboField;
public readonly record struct ComboFinisher;
public union ComboRole(ComboField, ComboFinisher);

// Conditions
public readonly record struct Bleeding;
public readonly record struct Burning;
public readonly record struct Poison;
public readonly record struct Torment;
public readonly record struct Confusion;
public readonly record struct Vulnerability;
public union ConditionType(Bleeding, Burning, Poison, Torment, Confusion, Vulnerability);

// Boons
public readonly record struct Might;
public readonly record struct Fury;
public readonly record struct Quickness;
public readonly record struct Alacrity;
public readonly record struct Protection;
public readonly record struct Resolution;
public readonly record struct Stability;
public readonly record struct Swiftness;
public readonly record struct Vigor;
public readonly record struct Aegis;
public readonly record struct Regeneration;
public union BoonType(Might, Fury, Quickness, Alacrity, Protection, Resolution, Stability, Swiftness, Vigor, Aegis, Regeneration);

// Top-level AbilityNature cases
public sealed record PhysicalNature(PhysicalDamageType DamageType);
public sealed record ElementalNature(ElementType Element);
public sealed record HardControlNature(HardControlType Control);
public sealed record SoftControlNature(SoftControlType Control);
public sealed record HealingNature(HealingType Kind);
public sealed record DefensiveNature(DefensiveType Kind);
public sealed record ComboNature(ComboRole Role);
public sealed record ConditionNature(ConditionType Kind);
public sealed record BoonNature(BoonType Kind);
public readonly record struct UtilityNature;

/// <summary>
/// Pure semantic classification of an ability node. Contains zero numerical stats,
/// durations, or distances—all execution numbers live exclusively in <see cref="AbilityEffect"/>.
/// </summary>
public union AbilityNature(
    PhysicalNature,
    ElementalNature,
    HardControlNature,
    SoftControlNature,
    HealingNature,
    DefensiveNature,
    ComboNature,
    ConditionNature,
    BoonNature,
    UtilityNature);
