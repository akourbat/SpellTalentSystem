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
            targeting: new SingleTarget(1200.0, new Enemies()),
            delivery: new InstantDelivery(),
            effect: new ApplyStatus(DurationSeconds: 3.0, Stacks: 1));

        return new PrimaryAbility(
            id: "frostbolt",
            name: "Frostbolt",
            nature: new ElementalNature(new Frost()),
            targeting: new SingleTarget(1200.0, new Enemies()),
            delivery: new ProjectileDelivery(Speed: 1600.0, Pierces: false, BounceCount: 0),
            effect: new DirectDamage(BaseDamage: 250.0, Coefficient: 0.85),
            castTimeSeconds: 1.0,
            cooldownSeconds: 6.0,
            subAbilities: [chillSub]);
    }

    [Fact]
    public void NumericTalents_AreCommutative_RegardlessOfPriorityOrOrder()
    {
        var frostbolt = CreateFrostbolt();

        var flatDamageTalent = new Talent(
            Id: "talent-flat-frost",
            Name: "Biting Cold",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage.AddFlat(50.0) }
                    : eff)));

        var increasedDamageTalent = new Talent(
            Id: "talent-inc-frost",
            Name: "Glacial Mastery",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage.AddIncreased(0.20) }
                    : eff)));

        var moreDamageTalent = new Talent(
            Id: "talent-more-frost",
            Name: "Shatter Focus",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage.MultiplyMore(1.10) }
                    : eff)));

        var forward = AbilityTalentPipeline.ApplyTalentsAndExpand(
            frostbolt,
            [flatDamageTalent, increasedDamageTalent, moreDamageTalent]);

        var reversed = AbilityTalentPipeline.ApplyTalentsAndExpand(
            frostbolt,
            [moreDamageTalent, increasedDamageTalent, flatDamageTalent]);

        var forwardDmg = Assert.IsType<DirectDamage>(forward.ElementAt(0).Ability.Effect.Value);
        var reversedDmg = Assert.IsType<DirectDamage>(reversed.ElementAt(0).Ability.Effect.Value);

        // Enriched buckets are identical regardless of application order:
        // (250 + 50) * (1 + 0.20) * 1.10 = 396.0
        Assert.Equal(forwardDmg.BaseDamage, reversedDmg.BaseDamage);
        Assert.Equal(250.0, forwardDmg.BaseDamage.Base);
        Assert.Equal(50.0, forwardDmg.BaseDamage.FlatModifier);
        Assert.Equal(0.20, forwardDmg.BaseDamage.IncreasedPercent, precision: 6);
        Assert.Equal(1.10, forwardDmg.BaseDamage.MoreMultiplier, precision: 6);
        Assert.Equal(396.0, forwardDmg.BaseDamage.Resolve(), precision: 6);
    }

    [Fact]
    public void TransformativeTalents_UsePriority_SoNatureConversionRunsBeforeElementalEnrichment()
    {
        var frostbolt = CreateFrostbolt();

        // Priority 0: Transformative talent converts Frost -> Fire
        var frostfireConversion = new Talent(
            Id: "transform-frostfire",
            Name: "Frostfire Infusion",
            Priority: 0,
            Apply: root => root is PrimaryAbility p && p.IsElement(new Frost())
                ? p with { Nature = new ElementalNature(new Fire()) }
                : root);

        // Priority 100: Standard enrichment talent boosts all Fire spells by +25%
        var pyromancy = new Talent(
            Id: "enrich-pyromancy",
            Name: "Pyromancy",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Fire()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage.AddIncreased(0.25) }
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
                Radius: 300.0,
                MaxTargets: 5,
                Allegiance: new Enemies()),
            delivery: new InstantDelivery(),
            effect: new DirectDamage(BaseDamage: 180.0, Coefficient: 0.65),
            castTimeSeconds: 0.0,
            cooldownSeconds: 12.0);

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
        using var pipeline = new AbilityTalentPipeline();
        ImmutableArray<ResolvedAbilityNode>? latestTree = null;

        using var subscription = pipeline.ResolvedAbilityTrees
            .Bind(out var trees)
            .Subscribe(_ => latestTree = trees.FirstOrDefault());

        pipeline.AddOrUpdateAbility(CreateFrostbolt());
        Assert.NotNull(latestTree);
        Assert.Equal(2, latestTree!.Value.Length);

        var extendChill = new Talent(
            Id: "talent-deep-freeze",
            Name: "Deep Freeze",
            Priority: 100,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.Nature.Value is SoftControlNature(var ctrl) && ctrl.Value is Chilled,
                eff => eff.Value is ApplyStatus status
                    ? status with { DurationSeconds = status.DurationSeconds.AddFlat(2.0) }
                    : eff)));

        pipeline.EquipTalent(extendChill);

        var chillEffect = Assert.IsType<ApplyStatus>(latestTree!.Value.ElementAt(1).Ability.Effect.Value);
        Assert.Equal(3.0, chillEffect.DurationSeconds.Base);
        Assert.Equal(2.0, chillEffect.DurationSeconds.FlatModifier);
        Assert.Equal(5.0, chillEffect.DurationSeconds.Resolve());

        pipeline.UnequipTalent("talent-deep-freeze");

        var revertedChill = Assert.IsType<ApplyStatus>(latestTree!.Value.ElementAt(1).Ability.Effect.Value);
        Assert.True(revertedChill.DurationSeconds.IsUnmodified);
        Assert.Equal(3.0, revertedChill.DurationSeconds.Resolve());
    }
}
