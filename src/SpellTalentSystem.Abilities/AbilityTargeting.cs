namespace SpellTalentSystem.Abilities;

public readonly record struct Enemies;
public readonly record struct Allies;
public readonly record struct SelfOnly;
public readonly record struct Both;
public union TargetAllegiance(Enemies, Allies, SelfOnly, Both);

public readonly record struct SelfTarget;
public sealed record SingleTarget(StatValue<double> Range, TargetAllegiance Allegiance);
public sealed record PointBlankAoE(StatValue<double> Radius, StatValue<int> MaxTargets, TargetAllegiance Allegiance);
public sealed record TargetCenteredAoE(StatValue<double> Radius, StatValue<int> MaxTargets, TargetAllegiance Allegiance);
public sealed record GroundTargetedAoE(StatValue<double> Range, StatValue<double> Radius, StatValue<int> MaxTargets, TargetAllegiance Allegiance);
public sealed record ConeTarget(StatValue<double> Range, StatValue<double> AngleDegrees, StatValue<int> MaxTargets, TargetAllegiance Allegiance);
public sealed record LineTarget(StatValue<double> Range, StatValue<double> Width, StatValue<int> MaxTargets, TargetAllegiance Allegiance);

/// <summary>
/// Defines where an ability node originates, its spatial geometry, target cap,
/// and which faction(s) it affects.
/// </summary>
public union AbilityTargeting(
    SelfTarget,
    SingleTarget,
    PointBlankAoE,
    TargetCenteredAoE,
    GroundTargetedAoE,
    ConeTarget,
    LineTarget);
