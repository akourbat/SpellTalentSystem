using System.Linq;
using SpellTalentSystem.Abilities;
using Xunit;

namespace SpellTalentSystem.Tests.Abilities;

public sealed class AbilityModelTests
{
    [Fact]
    public void SearingStrike_ModelsHarmfulAndBeneficialPointBlankAoEs_Separately()
    {
        var selfMightPulse = new DependentAbility(
            id: "searing-strike-might",
            name: "Searing Strike (Empower)",
            nature: new BoonNature(new Might()),
            targeting: new PointBlankAoE(
                Radius: StatValue<double>.FromBase(300.0),
                MaxTargets: StatValue<int>.FromBase(5),
                Allegiance: new Allies()),
            delivery: new InstantDelivery(),
            effect: new ApplyStatus(
                DurationSeconds: StatValue<double>.FromBase(8.0),
                Stacks: StatValue<int>.FromBase(3)));

        var searingStrike = new PrimaryAbility(
            id: "searing-strike",
            name: "Searing Strike",
            nature: new ElementalNature(new Fire()),
            targeting: new PointBlankAoE(
                Radius: StatValue<double>.FromBase(240.0),
                MaxTargets: StatValue<int>.FromBase(5),
                Allegiance: new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(320.0),
                Coefficient: StatValue<double>.FromBase(1.15)),
            castTimeSeconds: StatValue<double>.FromBase(0.5),
            cooldownSeconds: StatValue<double>.FromBase(8.0),
            subAbilities: [selfMightPulse]);

        Assert.Single(searingStrike.SubAbilities);
        Assert.IsType<DirectDamage>(searingStrike.Effect.Value);
        Assert.IsType<ApplyStatus>(searingStrike.SubAbilities.ElementAt(0).Effect.Value);
        Assert.Equal(240.0, ((PointBlankAoE)searingStrike.Targeting.Value!).Radius.Resolve());
        Assert.Equal(300.0, ((PointBlankAoE)selfMightPulse.Targeting.Value!).Radius.Resolve());
    }

    [Fact]
    public void ShieldStance_ModelsDefensiveBlock_WithDamageReductionEffect()
    {
        var shieldStance = new PrimaryAbility(
            id: "shield-stance",
            name: "Shield Stance",
            nature: new DefensiveNature(new Block()),
            targeting: new SelfTarget(),
            delivery: new ChanneledDelivery(
                DurationSeconds: StatValue<double>.FromBase(3.0),
                TickIntervalSeconds: StatValue<double>.FromBase(0.5)),
            effect: new ModifyIncomingDamage(
                Category: new AllDamageCategory(),
                Multiplier: StatValue<double>.FromBase(0.0),
                DurationSeconds: StatValue<double>.FromBase(3.0)),
            castTimeSeconds: StatValue<double>.FromBase(0.0),
            cooldownSeconds: StatValue<double>.FromBase(25.0));

        Assert.IsType<DefensiveNature>(shieldStance.Nature.Value);
        Assert.IsType<SelfTarget>(shieldStance.Targeting.Value);
        var reduction = Assert.IsType<ModifyIncomingDamage>(shieldStance.Effect.Value);
        Assert.Equal(0.0, reduction.Multiplier.Resolve());
    }
}
