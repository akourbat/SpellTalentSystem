namespace SpellTalentSystem.Items;

public enum BindingType { Unbound, BindOnEquip, BindOnPickup, AccountBound }
public enum ItemRarity { Basic, Fine, Masterwork, Rare, Exotic, Ascended, Legendary }

/// <summary>
/// Shared economy, rarity, and binding metadata across all <see cref="GameItem"/> types.
/// </summary>
public sealed record ItemLifecycle(
    ItemRarity Rarity = ItemRarity.Basic,
    BindingType Binding = BindingType.Unbound,
    bool IsBound = false,
    long? VendorCopper = null)
{
    public static ItemLifecycle Default { get; } = new();
}
