using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Items;


/// <summary>
/// Closed root of the item hierarchy carrying only metadata shared across all game items.
/// </summary>
public closed record GameItem
{
    private protected GameItem(
        string id,
        string name,
        string description = "",
        ItemLifecycle? lifecycle = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Lifecycle = lifecycle ?? ItemLifecycle.Default;
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public ItemLifecycle Lifecycle { get; init; }
}

/// <summary>
/// Equippable weapons, armor, trinkets, and relics with explicit typed <see cref="UpgradeSlots"/>.
/// </summary>
public sealed record Gear : GameItem
{
    public Gear(
        string id,
        string name,
        GearCategory category,
        string description = "",
        int requiredLevel = 1,
        string? setId = null,
        ImmutableArray<ItemEffect> baseEffects = default,
        ImmutableArray<UpgradeSlot> upgradeSlots = default,
        ItemLifecycle? lifecycle = null)
        : base(id, name, description, lifecycle)
    {
        Category = category;
        RequiredLevel = requiredLevel;
        SetId = setId;
        BaseEffects = baseEffects.IsDefault ? ImmutableArray<ItemEffect>.Empty : baseEffects;
        UpgradeSlots = upgradeSlots.IsDefault ? ImmutableArray<UpgradeSlot>.Empty : upgradeSlots;
    }

    public GearCategory Category { get; init; }
    public int RequiredLevel { get; init; }
    public string? SetId { get; init; }
    public ImmutableArray<ItemEffect> BaseEffects { get; init; }
    public ImmutableArray<UpgradeSlot> UpgradeSlots { get; init; }

    public Gear WithSlot(int slotIndex, GearUpgrade? upgrade) =>
        this with { UpgradeSlots = UpgradeSlot.SlotAt(UpgradeSlots, slotIndex, upgrade, Name) };

    public Gear WithSlotted(GearUpgrade upgrade) =>
        this with { UpgradeSlots = UpgradeSlot.SlotFirstAvailable(UpgradeSlots, upgrade, Name) };

    public IEnumerable<ItemEffect> CollectEffects()
    {
        foreach (var effect in BaseEffects)
            yield return effect;

        foreach (var slot in UpgradeSlots)
        {
            if (slot.Slotted is null)
                continue;

            foreach (var upgradeEffect in slot.Slotted.CollectEffects())
                yield return upgradeEffect;
        }
    }
}

/// <summary>
/// Socketable enhancements (StatAffix, Sigil, Rune, Infusion, Gem, Enchantment) that can carry
/// attributes, granted talents, and optional nested <see cref="UpgradeKind.StatAffix"/> slots.
/// </summary>
public sealed record GearUpgrade : GameItem
{
    public GearUpgrade(
        string id,
        string name,
        UpgradeCategory category,
        string description = "",
        int requiredLevel = 1,
        string? setId = null,
        ImmutableArray<ItemEffect> effects = default,
        ImmutableArray<UpgradeSlot> upgradeSlots = default,
        ItemLifecycle? lifecycle = null)
        : base(id, name, description, lifecycle)
    {
        var resolvedSlots = upgradeSlots.IsDefault ? ImmutableArray<UpgradeSlot>.Empty : upgradeSlots;
        if (resolvedSlots.Any(s => s.AcceptedKind != UpgradeKind.StatAffix))
        {
            throw new ArgumentException(
                $"GearUpgrade '{name}' can only declare UpgradeSlots of kind {UpgradeKind.StatAffix}.",
                nameof(upgradeSlots));
        }

        Category = category;
        RequiredLevel = requiredLevel;
        SetId = setId;
        Effects = effects.IsDefault ? ImmutableArray<ItemEffect>.Empty : effects;
        UpgradeSlots = resolvedSlots;
    }

    public UpgradeCategory Category { get; init; }
    public int RequiredLevel { get; init; }
    public string? SetId { get; init; }
    public ImmutableArray<ItemEffect> Effects { get; init; }
    public ImmutableArray<UpgradeSlot> UpgradeSlots { get; init; }

    public GearUpgrade WithSlot(int slotIndex, GearUpgrade? affix) =>
        this with { UpgradeSlots = UpgradeSlot.SlotAt(UpgradeSlots, slotIndex, affix, Name) };

    public GearUpgrade WithSlotted(GearUpgrade affix) =>
        this with { UpgradeSlots = UpgradeSlot.SlotFirstAvailable(UpgradeSlots, affix, Name) };

    public IEnumerable<ItemEffect> CollectEffects()
    {
        foreach (var effect in Effects)
            yield return effect;

        foreach (var slot in UpgradeSlots)
        {
            if (slot.Slotted is null)
                continue;

            foreach (var nestedEffect in slot.Slotted.CollectEffects())
                yield return nestedEffect;
        }
    }
}

/// <summary>
/// Materialized combat or buff items (potions, food, utility oils, combat scrolls).
/// </summary>
// ConsumableItem in GameItem.cs
public sealed record ConsumableItem : GameItem
{
    public ConsumableItem(
        string id,
        string name,
        ConsumableCategory category,
        string description = "",
        int requiredLevel = 1,
        PrimaryAbility? ability = null,
        ImmutableArray<ItemEffect> effects = default,
        TimeSpan? spoilDuration = null,
        int charges = 1,
        ItemLifecycle? lifecycle = null)
        : base(id, name, description, lifecycle)
    {
        Category = category;
        RequiredLevel = requiredLevel;
        Ability = ability;
        Effects = effects.IsDefault ? ImmutableArray<ItemEffect>.Empty : effects;
        SpoilDuration = spoilDuration;
        Charges = charges;
    }

    public ConsumableCategory Category { get; init; }
    public int RequiredLevel { get; init; }
    public PrimaryAbility? Ability { get; init; }
    public ImmutableArray<ItemEffect> Effects { get; init; }
    public TimeSpan? SpoilDuration { get; init; }
    public int Charges { get; init; }
}


/// <summary>
/// Non-equippable inventory items (crafting materials, trophies, quest items, salvage kits, currency tokens).
/// </summary>
public sealed record SimpleItem : GameItem
{
    public SimpleItem(
        string id,
        string name,
        SimpleCategory category,
        string description = "",
        int maxStackSize = 250,
        ItemLifecycle? lifecycle = null)
        : base(id, name, description, lifecycle)
    {
        Category = category;
        MaxStackSize = maxStackSize;
    }

    public SimpleCategory Category { get; init; }
    public int MaxStackSize { get; init; }
}
