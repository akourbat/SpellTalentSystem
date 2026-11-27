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
    public void ContainerItem_Throws_WhenChildrenExceedMaxChildSlots()
    {
        var leaf1 = new LeafItem("l1", "Leaf 1", new UpgradeCategory(new Gem()));
        var leaf2 = new LeafItem("l2", "Leaf 2", new UpgradeCategory(new Gem()));

        Assert.Throws<ArgumentException>(() =>
            new ContainerItem(
                id: "ring.test",
                name: "Test Ring",
                category: new TrinketCategory(new Ring()),
                maxChildSlots: 1,
                children: [leaf1, leaf2]));
    }

    [Fact]
    public void Aggregate_SumsStatsAcrossWeaponAndNestedEnhancements()
    {
        var sigilTalent = CreateDummyTalent("talent.sigil_force", "Superior Sigil of Force");

        var berserkerAffix = new LeafItem(
            id: "affix.berserker",
            name: "Berserker's Inscription",
            category: new UpgradeCategory(new StatAffix()),
            effects:
            [
                new ItemStatBonus(new PrimaryAttribute(new Power()), 251),
                new ItemStatBonus(new PrimaryAttribute(new Precision()), 179),
                new ItemStatBonus(new SecondaryAttribute(new Ferocity()), 179)
            ]);

        var sigilOfForce = new LeafItem(
            id: "sigil.force",
            name: "Superior Sigil of Force",
            category: new UpgradeCategory(new Sigil()),
            effects: [new GrantedTalent(sigilTalent)]);

        var mightyInfusion = new LeafItem(
            id: "infusion.mighty",
            name: "Mighty +9 Infusion",
            category: new UpgradeCategory(new Infusion()),
            effects:
            [
                new ItemStatBonus(new PrimaryAttribute(new Power()), 5)
            ]);

        var greatsword = new ContainerItem(
            id: "wpn.claymore",
            name: "Zojja's Claymore",
            category: new WeaponCategory(new Greatsword()),
            maxChildSlots: 3,
            children: [berserkerAffix, sigilOfForce, mightyInfusion],
            effects:
            [
                new ItemStatBonus(new SpecialAttribute(new WeaponStrength()), 1100)
            ],
            lifecycle: new ItemLifecycle(RequiredLevel: 80, ItemLevel: 80, Rarity: ItemRarity.Ascended));

        var result = EquipmentAggregator.Aggregate([greatsword]);

        Assert.Equal(256, result.GetStatTotal(new PrimaryAttribute(new Power())));
        Assert.Equal(179, result.GetStatTotal(new PrimaryAttribute(new Precision())));
        Assert.Equal(179, result.GetStatTotal(new SecondaryAttribute(new Ferocity())));
        Assert.Equal(1100, result.GetStatTotal(new SpecialAttribute(new WeaponStrength())));
        Assert.Single(result.GrantedTalents, t => t.Id == "talent.sigil_force");
    }

    [Fact]
    public void Aggregate_CountsRuneSetPiecesAcrossArmorTree_AndUnlocksMatchingTiers()
    {
        var sixPieceTalent = CreateDummyTalent("talent.scholar_6pc", "Scholar 90% HP Bonus");

        CharacterAttributeId power = new PrimaryAttribute(new Power());
        CharacterAttributeId ferocity = new SecondaryAttribute(new Ferocity());
        CharacterAttributeId defense = new SpecialAttribute(new Defense());

        var scholarSet = new ItemSetDefinition(
            SetId: "set.scholar",
            Name: "Superior Rune of the Scholar",
            Tiers:
            [
                new SetBonusTier(1, [new ItemStatBonus(power, 25)]),
                new SetBonusTier(2, [new ItemStatBonus(ferocity, 35)]),
                new SetBonusTier(3, [new ItemStatBonus(power, 50)]),
                new SetBonusTier(4, [new ItemStatBonus(ferocity, 65)]),
                new SetBonusTier(5, [new ItemStatBonus(power, 100)]),
                new SetBonusTier(6, [new ItemStatBonus(ferocity, 125), new GrantedTalent(sixPieceTalent)])
            ]);

        ContainerItem MakeArmorPiece(ArmorPiece piece, double baseDefense, double piecePower)
        {
            var affix = new LeafItem(
                id: $"affix.{piece}",
                name: "Berserker Insignia",
                category: new UpgradeCategory(new StatAffix()),
                effects: [new ItemStatBonus(power, piecePower)]);

            var rune = new LeafItem(
                id: $"rune.scholar.{piece}",
                name: "Superior Rune of the Scholar",
                category: new UpgradeCategory(new Rune()),
                lifecycle: new ItemLifecycle(RequiredLevel: 80, ItemLevel: 80, Rarity: ItemRarity.Exotic, SetId: "set.scholar"));

            return new ContainerItem(
                id: $"armor.{piece}",
                name: $"Ascended {piece}",
                category: new ArmorCategory(new HeavyArmor(), piece),
                maxChildSlots: 2,
                children: [affix, rune],
                effects: [new ItemStatBonus(defense, baseDefense)],
                lifecycle: new ItemLifecycle(RequiredLevel: 80, ItemLevel: 80, Rarity: ItemRarity.Ascended));
        }

        // Equip 4 pieces -> unlocks 1pc, 2pc, 3pc, 4pc (75 Power, 100 Ferocity)
        var fourPieces = new Item[]
        {
            MakeArmorPiece(new Helm(), 100, 60),
            MakeArmorPiece(new Shoulders(), 80, 45),
            MakeArmorPiece(new Chestpiece(), 380, 140),
            MakeArmorPiece(new Gloves(), 80, 45)
        };

        var partialResult = EquipmentAggregator.Aggregate(fourPieces, [scholarSet]);
        Assert.Equal(4, partialResult.ActiveSetPieceCounts["set.scholar"]);
        Assert.Equal(290 + 75, partialResult.GetStatTotal(power));
        Assert.Equal(100, partialResult.GetStatTotal(ferocity));
        Assert.Empty(partialResult.GrantedTalents);

        // Equip all 6 pieces -> unlocks all 6 tiers (175 Power, 225 Ferocity + 6pc Talent)
        var sixPieces = new Item[]
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
        Assert.Equal(425 + 175, fullResult.GetStatTotal(power));
        Assert.Equal(225, fullResult.GetStatTotal(ferocity));
        Assert.Equal(920, fullResult.GetStatTotal(defense));
        Assert.Single(fullResult.GrantedTalents, t => t.Id == "talent.scholar_6pc");
    }

    [Fact]
    public void ConsumableLeafItem_WrapsAbilityAndSpoilLifecycle()
    {
        var healAbility = new PrimaryAbility(
            id: "ability.minor_heal",
            name: "Drink Healing Potion",
            nature: new HealingNature(new DirectHealing()),
            targeting: new SelfTarget(),
            delivery: new InstantDelivery(),
            effect: new DirectHeal(BaseHeal: 250.0, Coefficient: 0.0),
            castTimeSeconds: 0.0,
            cooldownSeconds: 30.0);

        var potion = new LeafItem(
            id: "con.healing_potion",
            name: "Minor Healing Potion",
            category: new ConsumableCategory(new Potion()),
            effects: [new UsableAbility(healAbility, Charges: 1, ConsumeOnUse: true)],
            lifecycle: new ItemLifecycle(
                RequiredLevel: 5,
                VendorCopper: 15,
                SpoilDuration: TimeSpan.FromHours(24)));

        var usable = Assert.IsType<UsableAbility>(potion.Effects.Single().Value);
        Assert.Equal("ability.minor_heal", usable.Ability.Id);
        Assert.Equal(TimeSpan.FromHours(24), potion.Lifecycle.SpoilDuration);
    }
}
