namespace SpellTalentSystem.Abilities;

// Combo field types
public readonly record struct FireField;
public readonly record struct IceField;
public readonly record struct LightningField;
public readonly record struct WaterField;
public readonly record struct SmokeField;
public readonly record struct DarkField;
public readonly record struct LightField;
public readonly record struct EtherealField;
public readonly record struct PoisonField;
public union ComboFieldType(FireField, IceField, LightningField, WaterField, SmokeField, DarkField, LightField, EtherealField, PoisonField);

// Combo finisher types
public readonly record struct ProjectileFinisher;
public readonly record struct WhirlFinisher;
public readonly record struct LeapFinisher;
public readonly record struct BlastFinisher;
public union ComboFinisherType(ProjectileFinisher, WhirlFinisher, LeapFinisher, BlastFinisher);

// Incoming damage categories
public readonly record struct StrikeDamageCategory;
public readonly record struct ConditionDamageCategory;
public readonly record struct AllDamageCategory;
public union DamageCategory(StrikeDamageCategory, ConditionDamageCategory, AllDamageCategory);

// Control payloads
public sealed record IncapacitateControl(StatValue<double> DurationSeconds, StatValue<double> BreakbarDamage);
public sealed record DisplacementControl(StatValue<double> Distance, StatValue<double> BreakbarDamage);
public union ControlPayload(IncapacitateControl, DisplacementControl);

// Concrete AbilityEffect cases
public sealed record DirectDamage(StatValue<double> BaseDamage, StatValue<double> Coefficient);
public sealed record DamageOverTime(StatValue<double> BaseDamagePerTick, StatValue<double> CoefficientPerTick, StatValue<double> DurationSeconds, StatValue<double> TickIntervalSeconds);
public sealed record DirectHeal(StatValue<double> BaseHeal, StatValue<double> Coefficient);
public sealed record HealOverTime(StatValue<double> BaseHealPerTick, StatValue<double> CoefficientPerTick, StatValue<double> DurationSeconds, StatValue<double> TickIntervalSeconds);
public sealed record BarrierEffect(StatValue<double> BaseBarrier, StatValue<double> Coefficient, StatValue<double> DurationSeconds);
public sealed record ApplyControl(ControlPayload Control);
public sealed record ApplyStatus(StatValue<double> DurationSeconds, StatValue<int> Stacks);
public sealed record RemoveStatus(StatValue<int> Count);
public sealed record GrantDefense(StatValue<double> DurationSeconds, StatValue<int>? MaxHits);
public sealed record ModifyIncomingDamage(DamageCategory Category, StatValue<double> Multiplier, StatValue<double> DurationSeconds);
public readonly record struct BreakStun;
public sealed record CreateComboField(ComboFieldType Field, StatValue<double> DurationSeconds);
public sealed record ExecuteComboFinisher(ComboFinisherType Finisher, StatValue<double> Chance);
public sealed record SummonEffect(string EntityId, StatValue<int> Count, StatValue<double> DurationSeconds);
public sealed record TeleportEffect(StatValue<double> MaxRange);

/// <summary>
/// Concrete state change and scaling numbers executed when an <see cref="Ability"/> node resolves.
/// </summary>
public union AbilityEffect(
    DirectDamage,
    DamageOverTime,
    DirectHeal,
    HealOverTime,
    BarrierEffect,
    ApplyControl,
    ApplyStatus,
    RemoveStatus,
    GrantDefense,
    ModifyIncomingDamage,
    BreakStun,
    CreateComboField,
    ExecuteComboFinisher,
    SummonEffect,
    TeleportEffect);
