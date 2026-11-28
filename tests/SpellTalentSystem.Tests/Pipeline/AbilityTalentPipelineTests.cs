using System;
using System.Collections.Immutable;
using System.Linq;
using DynamicData;
using SpellTalentSystem.Abilities;
using SpellTalentSystem.Pipeline;
using Xunit;

namespace SpellTalentSystem.Tests.Pipeline;

public sealed class AbilityTalentPipelineTests
{
    private static PrimaryAbility CreateFrostbolt()
    {
        var chillSub = new DependentAbility(
            id: "frostbolt-chill",
            name: "Frostbite",
            nature: new SoftControlNature(new Chilled()),
            targeting: new SingleTarget(StatValue<double>.FromBase(1200.0), new Enemies()),
            delivery: new InstantDelivery(),
            effect: new ApplyStatus(
                DurationSeconds: StatValue<double>.FromBase(3.0),
                Stacks: StatValue<int>.FromBase(1)));

        return new PrimaryAbility(
            id: "frostbolt",
            name: "Frostbolt",
            nature: new ElementalNature(new Frost()),
            targeting: new SingleTarget(StatValue<double>.FromBase(1200.0), new Enemies()),
            delivery: new ProjectileDelivery(
                Speed: StatValue<double>.FromBase(1600.0),
                Pierces: false,
                BounceCount: StatValue<int>.FromBase(0)),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(250.0),
                Coefficient: StatValue<double>.FromBase(0.85)),
            castTimeSeconds: StatValue<double>.FromBase(1.0),
            cooldownSeconds: StatValue<double>.FromBase(6.0),
            subAbilities: [chillSub]);
    }

    [Fact]
    public void NumericTalents_AreCommutative_RegardlessOfPriorityOrOrder()
    {
        var frostbolt = CreateFrostbolt();

        var flatBonus = StatValue<double>.FromFlat(50.0);
        var inc20Bonus = StatValue<double>.FromPercent(0.20);
        var inc10Bonus = StatValue<double>.FromPercent(0.10);

        var flatDamageTalent = new Talent(
            Id: "talent-flat-frost",
            Name: "Biting Cold",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage + flatBonus }
                    : eff)));

        var increasedDamageTalent = new Talent(
            Id: "talent-inc-frost",
            Name: "Glacial Mastery",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage + inc20Bonus }
                    : eff)));

        var moreDamageTalent = new Talent(
            Id: "talent-more-frost",
            Name: "Shatter Focus",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage + inc10Bonus }
                    : eff)));

        var forward = AbilityTalentPipeline.ApplyTalentsAndExpand(
            frostbolt,
            [flatDamageTalent, increasedDamageTalent, moreDamageTalent]);

        var reversed = AbilityTalentPipeline.ApplyTalentsAndExpand(
            frostbolt,
            [moreDamageTalent, increasedDamageTalent, flatDamageTalent]);

        var forwardDmg = Assert.IsType<DirectDamage>(forward.ElementAt(0).Ability.Effect.Value);
        var reversedDmg = Assert.IsType<DirectDamage>(reversed.ElementAt(0).Ability.Effect.Value);

        Assert.Equal(forwardDmg.BaseDamage, reversedDmg.BaseDamage);
        Assert.Equal(250.0, forwardDmg.BaseDamage.Base);
        Assert.Equal(50.0, forwardDmg.BaseDamage.FlatModifier);
        Assert.Equal(0.30, forwardDmg.BaseDamage.IncreasedPercent, precision: 6);
        Assert.Equal(390.0, forwardDmg.BaseDamage.Resolve(), precision: 6);
    }

    [Fact]
    public void TransformativeTalents_UsePriority_SoNatureConversionRunsBeforeElementalEnrichment()
    {
        var frostbolt = CreateFrostbolt();
        var pyroBonus = StatValue<double>.FromPercent(0.25);

        var frostfireConversion = new Talent(
            Id: "transform-frostfire",
            Name: "Frostfire Infusion",
            Priority: 0,
            Apply: root => root is PrimaryAbility p && p.IsElement(new Frost())
                ? p with { Nature = new ElementalNature(new Fire()) }
                : root);

        var pyromancy = new Talent(
            Id: "enrich-pyromancy",
            Name: "Pyromancy",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Fire()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage + pyroBonus }
                    : eff)));

        var resolved = AbilityTalentPipeline.ApplyTalentsAndExpand(
            frostbolt,
            [pyromancy, frostfireConversion]);

        var rootNode = resolved.ElementAt(0);
        Assert.True(rootNode.Ability.IsElement(new Fire()));
        var dmg = Assert.IsType<DirectDamage>(rootNode.Ability.Effect.Value);
        Assert.Equal(0.25, dmg.BaseDamage.IncreasedPercent);
        Assert.Equal(312.5, dmg.BaseDamage.Resolve());
        Assert.Equal<string>(["transform-frostfire", "enrich-pyromancy"], rootNode.AppliedTalentIds);
    }

    [Fact]
    public void TraitTalent_CanAttachPrimaryAbilityChild_WithIndependentInternalCooldown()
    {
        var frostbolt = CreateFrostbolt();

        var iceNovaProc = new PrimaryAbility(
            id: "trait-ice-nova",
            name: "Shattering Nova",
            nature: new ElementalNature(new Frost()),
            targeting: new TargetCenteredAoE(
                Radius: StatValue<double>.FromBase(300.0),
                MaxTargets: StatValue<int>.FromBase(5),
                Allegiance: new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(180.0),
                Coefficient: StatValue<double>.FromBase(0.65)),
            castTimeSeconds: StatValue<double>.FromBase(0.0),
            cooldownSeconds: StatValue<double>.FromBase(12.0));

        var shatteringCritTrait = new Talent(
            Id: "trait-shattering-nova",
            Name: "Shattering Nova",
            Priority: 10,
            Apply: root => root.IsElement(new Frost())
                ? root.AddSubAbility(iceNovaProc)
                : root);

        var resolved = AbilityTalentPipeline.ApplyTalentsAndExpand(frostbolt, [shatteringCritTrait]);

        Assert.Equal(3, resolved.Length);
        var spawnedNode = resolved.ElementAt(2);
        Assert.Equal(1, spawnedNode.Depth);
        Assert.Equal("frostbolt", spawnedNode.ParentAbilityId);

        var spawnedPrimary = Assert.IsType<PrimaryAbility>(spawnedNode.Ability);
        Assert.Equal(12.0, spawnedPrimary.CooldownSeconds.Resolve());
    }
    [Fact]
    public void ReactivePipeline_EmitsUpdatedTree_WhenTalentIsEquippedOrUnequipped()
    {
        using var baseAbilities = new SourceCache<PrimaryAbility, string>(a => a.Id);
        using var activeTalents = new SourceCache<Talent, string>(t => t.Id);

        ImmutableArray<ResolvedAbilityNode>? latestTree = null;

        using var subscription = baseAbilities.Connect()
            .ResolveWithTalents(activeTalents.Connect())
            .Bind(out var trees)
            .Subscribe(_ => latestTree = trees.FirstOrDefault());

        baseAbilities.AddOrUpdate(CreateFrostbolt());
        Assert.NotNull(latestTree);
        Assert.Equal(2, latestTree!.Value.Length);

        var chillBonus = StatValue<double>.FromFlat(2.0);
        var extendChill = new Talent(
            Id: "talent-deep-freeze",
            Name: "Deep Freeze",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.Nature.Value is SoftControlNature(var ctrl) && ctrl.Value is Chilled,
                eff => eff.Value is ApplyStatus status
                    ? status with { DurationSeconds = status.DurationSeconds + chillBonus }
                    : eff)));

        activeTalents.AddOrUpdate(extendChill);

        var chillEffect = Assert.IsType<ApplyStatus>(latestTree!.Value.ElementAt(1).Ability.Effect.Value);
        Assert.Equal(3.0, chillEffect.DurationSeconds.Base);
        Assert.Equal(2.0, chillEffect.DurationSeconds.FlatModifier);
        Assert.Equal(5.0, chillEffect.DurationSeconds.Resolve());

        activeTalents.Remove("talent-deep-freeze");

        var revertedChill = Assert.IsType<ApplyStatus>(latestTree!.Value.ElementAt(1).Ability.Effect.Value);
        Assert.True(revertedChill.DurationSeconds.IsUnmodified);
        Assert.Equal(3.0, revertedChill.DurationSeconds.Resolve());
    }

    [Fact]
    public void ExpandNode_ThrowsDescriptiveException_WhenTraitCreatesCircularSubAbilityLoop()
    {
        var chainLightning = new PrimaryAbility(
            id: "chain-lightning",
            name: "Chain Lightning",
            nature: new ElementalNature(new Lightning()),
            targeting: new SingleTarget(StatValue<double>.FromBase(1200.0), new Enemies()),
            delivery: new ChainDelivery(
                MaxBounces: StatValue<int>.FromBase(3),
                BounceRadius: StatValue<double>.FromBase(400.0)),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(200.0),
                Coefficient: StatValue<double>.FromBase(0.75)),
            castTimeSeconds: StatValue<double>.FromBase(0.75),
            cooldownSeconds: StatValue<double>.FromBase(8.0));

        var staticDischarge = new PrimaryAbility(
            id: "static-discharge",
            name: "Static Discharge",
            nature: new ElementalNature(new Lightning()),
            targeting: new PointBlankAoE(
                Radius: StatValue<double>.FromBase(240.0),
                MaxTargets: StatValue<int>.FromBase(5),
                Allegiance: new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(90.0),
                Coefficient: StatValue<double>.FromBase(0.40)),
            castTimeSeconds: StatValue<double>.FromBase(0.0),
            cooldownSeconds: StatValue<double>.FromBase(4.0),
            subAbilities: [chainLightning]);

        var cyclicTrait = new Talent(
            Id: "trait-recursive-overload",
            Name: "Recursive Overload",
            Priority: 10,
            Apply: root => root.Id == "chain-lightning"
                ? root.AddSubAbility(staticDischarge)
                : root);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            AbilityTalentPipeline.ApplyTalentsAndExpand(chainLightning, [cyclicTrait]));

        Assert.Contains("chain-lightning -> static-discharge -> chain-lightning", ex.Message);
    }

    [Fact]
    public void ExpandNode_AllowsSameSubAbilityId_AcrossDistinctSiblingBranches()
    {
        var iceShard = new DependentAbility(
            id: "ice-shard",
            name: "Ice Shard",
            nature: new ElementalNature(new Frost()),
            targeting: new SingleTarget(StatValue<double>.FromBase(900.0), new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(60.0),
                Coefficient: StatValue<double>.FromBase(0.25)));

        var frostPulse = new DependentAbility(
            id: "frost-pulse",
            name: "Frost Pulse",
            nature: new ElementalNature(new Frost()),
            targeting: new PointBlankAoE(
                Radius: StatValue<double>.FromBase(250.0),
                MaxTargets: StatValue<int>.FromBase(5),
                Allegiance: new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(100.0),
                Coefficient: StatValue<double>.FromBase(0.45)),
            subAbilities: [iceShard]);

        var glacialBarrage = new PrimaryAbility(
            id: "glacial-barrage",
            name: "Glacial Barrage",
            nature: new ElementalNature(new Frost()),
            targeting: new SingleTarget(StatValue<double>.FromBase(1200.0), new Enemies()),
            delivery: new ProjectileDelivery(
                Speed: StatValue<double>.FromBase(1600.0),
                Pierces: false,
                BounceCount: StatValue<int>.FromBase(0)),
            effect: new DirectDamage(
                BaseDamage: StatValue<double>.FromBase(220.0),
                Coefficient: StatValue<double>.FromBase(0.80)),
            castTimeSeconds: StatValue<double>.FromBase(1.0),
            cooldownSeconds: StatValue<double>.FromBase(6.0),
            subAbilities: [iceShard, frostPulse]);

        var resolved = AbilityTalentPipeline.ApplyTalentsAndExpand(glacialBarrage, []);

        Assert.Equal(4, resolved.Length);
        Assert.Equal(2, resolved.Count(n => n.Ability.Id == "ice-shard"));
    }
}
