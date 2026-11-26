namespace SpellTalentSystem.Abilities;

public readonly record struct InstantDelivery;
public sealed record ProjectileDelivery(StatValue<double> Speed, bool Pierces, StatValue<int> BounceCount);
public readonly record struct BeamDelivery;
public sealed record ChanneledDelivery(StatValue<double> DurationSeconds, StatValue<double> TickIntervalSeconds);
public sealed record DelayedDelivery(StatValue<double> DelaySeconds);
public sealed record PulseDelivery(StatValue<double> DurationSeconds, StatValue<double> TickIntervalSeconds);
public sealed record ChainDelivery(StatValue<int> MaxBounces, StatValue<double> BounceRadius);

/// <summary>
/// Defines how an ability node travels through space or resolves across time
/// once its target/origin is established by <see cref="AbilityTargeting"/>.
/// </summary>
public union AbilityDelivery(
    InstantDelivery,
    ProjectileDelivery,
    BeamDelivery,
    ChanneledDelivery,
    DelayedDelivery,
    PulseDelivery,
    ChainDelivery);
