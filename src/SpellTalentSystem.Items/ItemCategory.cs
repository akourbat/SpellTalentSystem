namespace SpellTalentSystem.Items;

// ==================== 1. GEAR CATEGORIES ====================

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

public readonly record struct Amulet;
public readonly record struct Ring;
public readonly record struct Accessory;
public readonly record struct Backpiece;
public readonly record struct Relic;

public union TrinketCategory(Amulet, Ring, Accessory, Backpiece, Relic);

/// <summary>
/// Subset of item classifications representing equippable <see cref="Gear"/> (weapons, armor, trinkets, and relics).
/// </summary>
public union GearCategory(
    WeaponCategory,
    ArmorCategory,
    TrinketCategory);

// ==================== 2. GEAR UPGRADE CATEGORIES ====================

public enum UpgradeKind
{
    StatAffix,
    Sigil,
    Rune,
    Infusion,
    Gem,
    Enchantment
}

public readonly record struct StatAffix;
public readonly record struct Sigil;
public readonly record struct Rune;
public readonly record struct Infusion;
public readonly record struct Gem;
public readonly record struct Enchantment;

/// <summary>
/// Classifications for socketable <see cref="GearUpgrade"/> items.
/// </summary>
public union UpgradeCategory(StatAffix, Sigil, Rune, Infusion, Gem, Enchantment)
{
    public UpgradeKind Kind => Value switch
    {
        StatAffix => UpgradeKind.StatAffix,
        Sigil => UpgradeKind.Sigil,
        Rune => UpgradeKind.Rune,
        Infusion => UpgradeKind.Infusion,
        Gem => UpgradeKind.Gem,
        Enchantment => UpgradeKind.Enchantment,
        _ => throw new InvalidOperationException($"Unsupported UpgradeCategory: {Value}")
    };
}

// ==================== 3. CONSUMABLE CATEGORIES ====================

public readonly record struct Potion;
public readonly record struct Food;
public readonly record struct UtilityOil;
public readonly record struct Scroll;

/// <summary>
/// Classifications for materialized combat/buff <see cref="ConsumableItem"/> items.
/// </summary>
public union ConsumableCategory(Potion, Food, UtilityOil, Scroll);

// ==================== 4. SIMPLE ITEM CATEGORIES ====================

public readonly record struct CraftingMaterial;
public readonly record struct Trophy;
public readonly record struct QuestItem;
public readonly record struct SalvageKit;
public readonly record struct CurrencyToken;

/// <summary>
/// Simplified top-level classifications for non-equippable inventory items (<see cref="SimpleItem"/>).
/// </summary>
public union SimpleCategory(CraftingMaterial, Trophy, QuestItem, SalvageKit, CurrencyToken);
