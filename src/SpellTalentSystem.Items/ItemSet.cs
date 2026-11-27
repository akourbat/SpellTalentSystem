using System.Collections.Immutable;

namespace SpellTalentSystem.Items;

/// <summary>
/// A single threshold tier within an item or rune set (e.g., 2-piece, 4-piece, 6-piece).
/// </summary>
public sealed record SetBonusTier(
    int RequiredPieces,
    ImmutableArray<ItemEffect> Effects);

/// <summary>
/// Defines tiered bonuses unlocked by equipping multiple items or enhancements sharing <see cref="SetId"/>.
/// </summary>
public sealed record ItemSetDefinition(
    string SetId,
    string Name,
    ImmutableArray<SetBonusTier> Tiers);
