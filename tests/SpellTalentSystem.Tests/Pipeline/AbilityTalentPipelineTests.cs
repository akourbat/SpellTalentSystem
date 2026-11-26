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
    public void Talents_ApplyInPriorityOrder_AndPreserveBaseStats()
    {
        var frostbolt = CreateFrostbolt();

        var flatDamageTalent = new Talent(
            Id: "talent-flat-frost",
            Name: "Biting Cold",
            Priority: 10,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
                eff => eff.Value is DirectDamage dmg
                    ? dmg with { BaseDamage = dmg.BaseDamage.AddFlat(50.0) }
                    : eff)));

        var percentDamageTalent = new Talent(
        Id: "talent-mult-frost",
        Name: "Glacial Mastery",
        Priority: 20,
        Apply: root => root.TransformTree(node => node.MapEffect(
            n => n.IsElement(new Frost()) && n.Effect.Value is DirectDamage,
            eff => eff.Value is DirectDamage dmg
                ? dmg with { BaseDamage = dmg.BaseDamage.ScaleBy(1.20) }
                : eff)));


        var resolved = AbilityTalentPipeline.ApplyTalentsAndExpand(
            frostbolt,
            [percentDamageTalent, flatDamageTalent]);

        Assert.Equal(2, resolved.Length);
        var rootNode = resolved.ElementAt(0);
        var rootDamage = Assert.IsType<DirectDamage>(rootNode.Ability.Effect.Value);
        Assert.Equal(250.0, rootDamage.BaseDamage.Base);
        Assert.Equal(360.0, rootDamage.BaseDamage.Current, precision: 6);
        Assert.Equal<string>(["talent-flat-frost", "talent-mult-frost"], rootNode.AppliedTalentIds);
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
            Priority: 100,
            Apply: root => root.IsElement(new Frost())
                ? root.AddSubAbility(iceNovaProc)
                : root);

        var resolved = AbilityTalentPipeline.ApplyTalentsAndExpand(frostbolt, [shatteringCritTrait]);

        Assert.Equal(3, resolved.Length);
        var spawnedNode = resolved.ElementAt(2);
        Assert.Equal(1, spawnedNode.Depth);
        Assert.Equal("frostbolt", spawnedNode.ParentAbilityId);

        var spawnedPrimary = Assert.IsType<PrimaryAbility>(spawnedNode.Ability);
        Assert.Equal(12.0, spawnedPrimary.CooldownSeconds.Current);
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
            Priority: 10,
            Apply: root => root.TransformTree(node => node.MapEffect(
                n => n.Nature.Value is SoftControlNature(var ctrl) && ctrl.Value is Chilled,
                eff => eff.Value is ApplyStatus status
                    ? status with { DurationSeconds = status.DurationSeconds.AddFlat(2.0) }
                    : eff)));

        pipeline.EquipTalent(extendChill);

        var chillEffect = Assert.IsType<ApplyStatus>(latestTree!.Value.ElementAt(1).Ability.Effect.Value);
        Assert.Equal(3.0, chillEffect.DurationSeconds.Base);
        Assert.Equal(5.0, chillEffect.DurationSeconds.Current);

        pipeline.UnequipTalent("talent-deep-freeze");

        var revertedChill = Assert.IsType<ApplyStatus>(latestTree!.Value.ElementAt(1).Ability.Effect.Value);
        Assert.Equal(3.0, revertedChill.DurationSeconds.Current);
    }
}
