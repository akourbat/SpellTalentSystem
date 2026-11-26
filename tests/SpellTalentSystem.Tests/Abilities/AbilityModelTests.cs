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
                Radius: 300.0,
                MaxTargets: 5,
                Allegiance: new Allies()),
            delivery: new InstantDelivery(),
            effect: new ApplyStatus(DurationSeconds: 8.0, Stacks: 3));

        var searingStrike = new PrimaryAbility(
            id: "searing-strike",
            name: "Searing Strike",
            nature: new ElementalNature(new Fire()),
            targeting: new PointBlankAoE(
                Radius: 240.0,
                MaxTargets: 5,
                Allegiance: new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(BaseDamage: 320.0, Coefficient: 1.15),
            castTimeSeconds: 0.5,
            cooldownSeconds: 8.0,
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
            delivery: new ChanneledDelivery(DurationSeconds: 3.0, TickIntervalSeconds: 0.5),
            effect: new ModifyIncomingDamage(
                Category: new AllDamageCategory(),
                Multiplier: 0.0,
                DurationSeconds: 3.0),
            castTimeSeconds: 0.0,
            cooldownSeconds: 25.0);

        Assert.IsType<DefensiveNature>(shieldStance.Nature.Value);
        Assert.IsType<SelfTarget>(shieldStance.Targeting.Value);
        var reduction = Assert.IsType<ModifyIncomingDamage>(shieldStance.Effect.Value);
        Assert.Equal(0.0, reduction.Multiplier.Resolve());
    }
}
