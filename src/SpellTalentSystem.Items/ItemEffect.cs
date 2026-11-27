using SpellTalentSystem.Abilities;
using SpellTalentSystem.Pipeline;

namespace SpellTalentSystem.Items;

public sealed record ItemStatBonus(CharacterAttributeId Attribute, double Value);
public sealed record GrantedTalent(Talent Talent);
public sealed record UsableAbility(Ability Ability, int Charges = 1, bool ConsumeOnUse = true);

/// <summary>
/// Discriminated union of gameplay payloads carried by an item or set bonus tier.
/// </summary>
public union ItemEffect(
    ItemStatBonus,
    GrantedTalent,
    UsableAbility);
