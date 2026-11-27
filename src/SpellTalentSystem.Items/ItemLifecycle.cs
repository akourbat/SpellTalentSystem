using System;

namespace SpellTalentSystem.Items;

public enum BindingType { Unbound, BindOnEquip, BindOnPickup, AccountBound }
public enum ItemRarity { Basic, Fine, Masterwork, Rare, Exotic, Ascended, Legendary }

/// <summary>
/// Progression, economy, binding, spoilage, and set-membership metadata for an item.
/// </summary>
public sealed record ItemLifecycle(
    int RequiredLevel = 1,
    int ItemLevel = 1,
    ItemRarity Rarity = ItemRarity.Basic,
    BindingType Binding = BindingType.Unbound,
    bool IsBound = false,
    long? VendorCopper = null,
    bool IsQuestItem = false,
    TimeSpan? SpoilDuration = null,
    string? SetId = null)
{
    public static ItemLifecycle Default { get; } = new();
}
