namespace SpellTalentSystem.Items;

// 1. Weapon
public readonly record struct Sword;
public readonly record struct Greatsword;
public readonly record struct Dagger;
public readonly record struct Axe;
public readonly record struct Mace;
public readonly record struct Hammer;
public readonly record struct Staff;
public readonly record struct Scepter;
public readonly record struct Focus;
public readonly record struct Shield;
public readonly record struct Longbow;
public readonly record struct Shortbow;
public readonly record struct Pistol;
public readonly record struct Rifle;

public union WeaponCategory(
    Sword, Greatsword, Dagger, Axe, Mace, Hammer,
    Staff, Scepter, Focus, Shield, Longbow, Shortbow, Pistol, Rifle);

// 2. Armor (Weight + Piece paired in a 2-property record at level 2)
public readonly record struct LightArmor;
public readonly record struct MediumArmor;
public readonly record struct HeavyArmor;

public union ArmorWeight(LightArmor, MediumArmor, HeavyArmor);

public readonly record struct Helm;
public readonly record struct Shoulders;
public readonly record struct Chestpiece;
public readonly record struct Gloves;
public readonly record struct Leggings;
public readonly record struct Boots;

public union ArmorPiece(Helm, Shoulders, Chestpiece, Gloves, Leggings, Boots);

public sealed record ArmorCategory(ArmorWeight Weight, ArmorPiece Piece);

// 3. Trinket
public readonly record struct Amulet;
public readonly record struct Ring;
public readonly record struct Accessory;
public readonly record struct Backpiece;

public union TrinketCategory(Amulet, Ring, Accessory, Backpiece);

// 4. Upgrade (socketable / enhancement leaves)
public readonly record struct StatAffix;
public readonly record struct Sigil;
public readonly record struct Rune;
public readonly record struct Infusion;
public readonly record struct Gem;
public readonly record struct Enchantment;

public union UpgradeCategory(StatAffix, Sigil, Rune, Infusion, Gem, Enchantment);

// 5. Consumable
public readonly record struct Potion;
public readonly record struct Food;
public readonly record struct UtilityOil;
public readonly record struct Scroll;

public union ConsumableCategory(Potion, Food, UtilityOil, Scroll);

// 6. Material (crafting resources)
public readonly record struct Ore;
public readonly record struct Ingot;
public readonly record struct Wood;
public readonly record struct Plank;
public readonly record struct Leather;
public readonly record struct Cloth;
public readonly record struct Herb;
public readonly record struct Reagent;

public union MaterialCategory(Ore, Ingot, Wood, Plank, Leather, Cloth, Herb, Reagent);

// 7. Misc
public readonly record struct QuestToken;
public readonly record struct SalvageKit;
public readonly record struct Trophy;
public readonly record struct ContainerBag;

public union MiscCategory(QuestToken, SalvageKit, Trophy, ContainerBag);

/// <summary>
/// Pure semantic classification answering "What is this item?".
/// Carries zero numerical stats or restrictions.
/// </summary>
public union ItemCategory(
    WeaponCategory,
    ArmorCategory,
    TrinketCategory,
    UpgradeCategory,
    ConsumableCategory,
    MaterialCategory,
    MiscCategory);
