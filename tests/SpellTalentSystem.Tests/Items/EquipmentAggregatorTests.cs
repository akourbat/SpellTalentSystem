using System;
using System.Linq;
using SpellTalentSystem.Abilities;
using SpellTalentSystem.Items;
using SpellTalentSystem.Pipeline;
using Xunit;

namespace SpellTalentSystem.Items.Tests;

public class EquipmentAggregatorTests
{
    private static Talent CreateDummyTalent(string id, string name) =>
        new(Id: id, Name: name, Priority: 100, Apply: root => root);

    [Fact]
    public void UpgradeSlot_Throws_WhenMismatchedUpgradeKindIsSlotted()
    {
        var rune = new GearUpgrade("rune.test", "Test Rune", new UpgradeCategory(new Rune()));
        var ring = new Gear(
            id: "ring.test",
            name: "Test Ring",
            category: new TrinketCategory(new Ring()),
            upgradeSlots: [new UpgradeSlot(UpgradeKind.Infusion)]);

        Assert.Throws<ArgumentException>(() => ring.WithSlot(0, rune));
        Assert.Throws<InvalidOperationException>(() => ring.WithSlotted(rune));
    }

    [Fact]
    public void GearUpgrade_Throws_WhenNonStatAffixSlotIsDeclared()
    {
        Assert.Throws<ArgumentException>(() =>
            new GearUpgrade(
                id: "ench.bad",
                name: "Invalid Enchantment",
                category: new UpgradeCategory(new Enchantment()),
                upgradeSlots: [new UpgradeSlot(UpgradeKind.Sigil)]));
    }

    [Fact]
    public void Aggregate_SumsStatsAndTalentsAcrossWeaponRelicAndNestedAffixes()
    {
        var sigilTalent = CreateDummyTalent("talent.sigil_force", "Superior Sigil of Force");
        var relicTalent = CreateDummyTalent("talent.relic_fireworks", "Relic of Fireworks");

        var berserkerAffix = new GearUpgrade(
            id: "affix.berserker",
            name: "Berserker's Inscription",
            category: new UpgradeCategory(new StatAffix()),
            effects:
            [
                new PrimaryAttribute(new Power(StatValue<double>.FromFlat(251))),
                new PrimaryAttribute(new Precision(StatValue<double>.FromFlat(179))),
                new SecondaryAttribute(new Ferocity(StatValue<double>.FromFlat(179)))
            ]);

        var sigilOfForce = new GearUpgrade(
            id: "sigil.force",
            name: "Superior Sigil of Force",
            category: new UpgradeCategory(new Sigil()),
            effects: [new GrantedTalent(sigilTalent)]);

        var mightyInfusion = new GearUpgrade(
            id: "infusion.mighty",
            name: "Mighty +9 Infusion",
            category: new UpgradeCategory(new Infusion()),
            effects:
            [
                new PrimaryAttribute(new Power(StatValue<double>.FromFlat(5)))
            ]);

        var greatsword = new Gear(
            id: "wpn.claymore",
            name: "Zojja's Claymore",
            category: new WeaponCategory(new Greatsword()),
            requiredLevel: 80,
            baseEffects:
            [
                new SpecialAttribute(new WeaponStrength(StatValue<double>.FromBase(1100)))
            ],
            upgradeSlots:
            [
                new UpgradeSlot(UpgradeKind.StatAffix, berserkerAffix),
                new UpgradeSlot(UpgradeKind.Sigil, sigilOfForce),
                new UpgradeSlot(UpgradeKind.Infusion, mightyInfusion)
            ],
            lifecycle: new ItemLifecycle(Rarity: ItemRarity.Ascended));

        var relicAffix = new GearUpgrade(
            id: "affix.relic_power",
            name: "Relic Power Affix",
            category: new UpgradeCategory(new StatAffix()),
            effects: [new PrimaryAttribute(new Power(StatValue<double>.FromFlat(44)))]);

        var relicOfFireworks = new Gear(
            id: "relic.fireworks",
            name: "Relic of Fireworks",
            category: new TrinketCategory(new Relic()),
            requiredLevel: 80,
            baseEffects: [new GrantedTalent(relicTalent)],
            upgradeSlots: [new UpgradeSlot(UpgradeKind.StatAffix)],
            lifecycle: new ItemLifecycle(Rarity: ItemRarity.Ascended))
            .WithSlotted(relicAffix);

        var result = EquipmentAggregator.Aggregate([greatsword, relicOfFireworks]);

        Assert.Equal(300, result.GetAttributeBucket(AttributeKind.Power).Resolve());
        Assert.Equal(179, result.GetAttributeBucket(AttributeKind.Precision).Resolve());
        Assert.Equal(179, result.GetAttributeBucket(AttributeKind.Ferocity).Resolve());
        Assert.Equal(1100, result.GetAttributeBucket(AttributeKind.WeaponStrength).Resolve());
        Assert.Equal(2, result.GrantedTalents.Length);
        Assert.Contains(result.GrantedTalents, t => t.Id == "talent.sigil_force");
        Assert.Contains(result.GrantedTalents, t => t.Id == "talent.relic_fireworks");
    }
    [Fact]
    public void Aggregate_SupportsNestedStatAffixInsideGearUpgrade()
    {
        var enchTalent = CreateDummyTalent("talent.arcane_echo", "Arcane Echo");
        var nestedAffix = new GearUpgrade(
            id: "affix.nested_prec",
            name: "Precision Etching",
            category: new UpgradeCategory(new StatAffix()),
            effects: [new PrimaryAttribute(new Precision(StatValue<double>.FromFlat(40)))]);

        var enchantment = new GearUpgrade(
            id: "ench.arcane",
            name: "Arcane Conduit",
            category: new UpgradeCategory(new Enchantment()),
            effects: [new GrantedTalent(enchTalent)],
            upgradeSlots: [new UpgradeSlot(UpgradeKind.StatAffix)])
            .WithSlotted(nestedAffix);

        var amulet = new Gear(
            id: "trinket.amulet",
            name: "Runed Amulet",
            category: new TrinketCategory(new Amulet()),
            upgradeSlots: [new UpgradeSlot(UpgradeKind.Enchantment)])
            .WithSlotted(enchantment);

        var result = EquipmentAggregator.Aggregate([amulet]);

        Assert.Equal(40, result.GetAttributeBucket(AttributeKind.Precision).Resolve());
        Assert.Single(result.GrantedTalents, t => t.Id == "talent.arcane_echo");
    }

    [Fact]
    public void Aggregate_CountsRuneSetPiecesAcrossArmorTree_AndUnlocksMatchingTiers()
    {
        var sixPieceTalent = CreateDummyTalent("talent.scholar_6pc", "Scholar 90% HP Bonus");

        var scholarSet = new ItemSetDefinition(
            SetId: "set.scholar",
            Name: "Superior Rune of the Scholar",
            Tiers:
            [
                new SetBonusTier(1, [new PrimaryAttribute(new Power(StatValue<double>.FromFlat(25)))]),
                new SetBonusTier(2, [new SecondaryAttribute(new Ferocity(StatValue<double>.FromFlat(35)))]),
                new SetBonusTier(3, [new PrimaryAttribute(new Power(StatValue<double>.FromFlat(50)))]),
                new SetBonusTier(4, [new SecondaryAttribute(new Ferocity(StatValue<double>.FromFlat(65)))]),
                new SetBonusTier(5, [new PrimaryAttribute(new Power(StatValue<double>.FromFlat(100)))]),
                new SetBonusTier(6, [new SecondaryAttribute(new Ferocity(StatValue<double>.FromFlat(125))), new GrantedTalent(sixPieceTalent)])
            ]);

        Gear MakeArmorPiece(ArmorPiece piece, double baseDefense, double piecePower)
        {
            var affix = new GearUpgrade(
                id: $"affix.{piece}",
                name: "Berserker Insignia",
                category: new UpgradeCategory(new StatAffix()),
                effects: [new PrimaryAttribute(new Power(StatValue<double>.FromFlat(piecePower)))]);

            var rune = new GearUpgrade(
                id: $"rune.scholar.{piece}",
                name: "Superior Rune of the Scholar",
                category: new UpgradeCategory(new Rune()),
                requiredLevel: 80,
                setId: "set.scholar",
                lifecycle: new ItemLifecycle(Rarity: ItemRarity.Exotic));

            return new Gear(
                id: $"armor.{piece}",
                name: $"Ascended {piece}",
                category: new ArmorCategory(new HeavyArmor(), piece),
                requiredLevel: 80,
                baseEffects: [new SpecialAttribute(new Defense(StatValue<double>.FromBase(baseDefense)))],
                upgradeSlots:
                [
                    new UpgradeSlot(UpgradeKind.StatAffix, affix),
                    new UpgradeSlot(UpgradeKind.Rune, rune)
                ],
                lifecycle: new ItemLifecycle(Rarity: ItemRarity.Ascended));
        }

        var fourPieces = new[]
        {
            MakeArmorPiece(new Helm(), 100, 60),
            MakeArmorPiece(new Shoulders(), 80, 45),
            MakeArmorPiece(new Chestpiece(), 380, 140),
            MakeArmorPiece(new Gloves(), 80, 45)
        };

        var partialResult = EquipmentAggregator.Aggregate(fourPieces, [scholarSet]);
        Assert.Equal(4, partialResult.ActiveSetPieceCounts["set.scholar"]);
        Assert.Equal(290 + 75, partialResult.GetAttributeBucket(AttributeKind.Power).Resolve());
        Assert.Equal(100, partialResult.GetAttributeBucket(AttributeKind.Ferocity).Resolve());
        Assert.Empty(partialResult.GrantedTalents);

        var sixPieces = new[]
        {
            MakeArmorPiece(new Helm(), 100, 60),
            MakeArmorPiece(new Shoulders(), 80, 45),
            MakeArmorPiece(new Chestpiece(), 380, 140),
            MakeArmorPiece(new Gloves(), 80, 45),
            MakeArmorPiece(new Leggings(), 200, 90),
            MakeArmorPiece(new Boots(), 80, 45)
        };

        var fullResult = EquipmentAggregator.Aggregate(sixPieces, [scholarSet]);
        Assert.Equal(6, fullResult.ActiveSetPieceCounts["set.scholar"]);
        Assert.Equal(425 + 175, fullResult.GetAttributeBucket(AttributeKind.Power).Resolve());
        Assert.Equal(225, fullResult.GetAttributeBucket(AttributeKind.Ferocity).Resolve());
        Assert.Equal(920, fullResult.GetAttributeBucket(AttributeKind.Defense).Resolve());
        Assert.Single(fullResult.GrantedTalents, t => t.Id == "talent.scholar_6pc");
    }

    [Fact]
    public void ConsumableAndSimpleItems_RepresentNonGearEntitiesCleanly()
    {
        var healAbility = new PrimaryAbility(
            id: "ability.minor_heal",
            name: "Drink Healing Potion",
            nature: new HealingNature(new DirectHealing()),
            targeting: new SelfTarget(),
            delivery: new InstantDelivery(),
            effect: new DirectHeal(
                BaseHeal: StatValue<double>.FromBase(250.0),
                Coefficient: StatValue<double>.FromBase(0.0)),
            castTimeSeconds: StatValue<double>.FromBase(0.0),
            cooldownSeconds: StatValue<double>.FromBase(30.0));

        var potion = new ConsumableItem(
            id: "con.healing_potion",
            name: "Minor Healing Potion",
            category: new ConsumableCategory(new Potion()),
            requiredLevel: 5,
            usableAbility: new UsableAbility(healAbility, Charges: 1, ConsumeOnUse: true),
            spoilDuration: TimeSpan.FromHours(24),
            lifecycle: new ItemLifecycle(VendorCopper: 15));

        var salvageKit = new SimpleItem(
            id: "kit.mystic",
            name: "Mystic Salvage Kit",
            category: new SimpleCategory(new SalvageKit()),
            maxStackSize: 250,
            lifecycle: new ItemLifecycle(Rarity: ItemRarity.Rare, VendorCopper: 250));

        Assert.Equal("ability.minor_heal", potion.UsableAbility!.Ability.Id);
        Assert.Equal(TimeSpan.FromHours(24), potion.SpoilDuration);
        Assert.IsType<SalvageKit>(salvageKit.Category.Value);
    }
}
