using SpellTalentSystem.Abilities;
using SpellTalentSystem.Pipeline;

namespace SpellTalentSystem.Items;

public sealed record GrantedTalent(Talent Talent);
public sealed record UsableAbility(Ability Ability, int Charges = 1, bool ConsumeOnUse = true);

/// <summary>
/// Discriminated union of gameplay payloads carried by an item or set bonus tier.
/// </summary>
public union ItemEffect(
    CharacterAttribute,
    GrantedTalent,
    UsableAbility)
{
    public static implicit operator ItemEffect(PrimaryAttribute attr) => new CharacterAttribute(attr);
    public static implicit operator ItemEffect(SecondaryAttribute attr) => new CharacterAttribute(attr);
    public static implicit operator ItemEffect(SpecialAttribute attr) => new CharacterAttribute(attr);
}
