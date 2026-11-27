using System;
using System.Collections.Immutable;

namespace SpellTalentSystem.Items;

/// <summary>
/// Closed root of the item hierarchy.
/// </summary>
public closed record Item
{
    private protected Item(
        string id,
        string name,
        ItemCategory category,
        ImmutableArray<ItemEffect> effects = default,
        ItemLifecycle? lifecycle = null)
    {
        Id = id;
        Name = name;
        Category = category;
        Effects = effects.IsDefault ? ImmutableArray<ItemEffect>.Empty : effects;
        Lifecycle = lifecycle ?? ItemLifecycle.Default;
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public ItemCategory Category { get; init; }
    public ImmutableArray<ItemEffect> Effects { get; init; }
    public ItemLifecycle Lifecycle { get; init; }
}

/// <summary>
/// Branch node representing an item that can hold child items (e.g., weapons/armor with socketed affixes, sigils, runes, or infusions).
/// </summary>
public sealed record ContainerItem : Item
{
    public ContainerItem(
        string id,
        string name,
        ItemCategory category,
        int maxChildSlots,
        ImmutableArray<Item> children = default,
        ImmutableArray<ItemEffect> effects = default,
        ItemLifecycle? lifecycle = null)
        : base(id, name, category, effects, lifecycle)
    {
        var resolved = children.IsDefault ? ImmutableArray<Item>.Empty : children;
        if (resolved.Length > maxChildSlots)
            throw new ArgumentException($"'{name}' supports at most {maxChildSlots} child items.", nameof(children));

        MaxChildSlots = maxChildSlots;
        Children = resolved;
    }

    public int MaxChildSlots { get; init; }
    public ImmutableArray<Item> Children { get; init; }
}

/// <summary>
/// Leaf node representing an indivisible item (e.g., stat affix, sigil, rune, infusion, potion, ore, or quest token).
/// </summary>
public sealed record LeafItem : Item
{
    public LeafItem(
        string id,
        string name,
        ItemCategory category,
        ImmutableArray<ItemEffect> effects = default,
        ItemLifecycle? lifecycle = null)
        : base(id, name, category, effects, lifecycle)
    { }
}
