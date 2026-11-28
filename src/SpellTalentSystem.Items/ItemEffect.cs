using SpellTalentSystem.Abilities;
using SpellTalentSystem.Pipeline;

namespace SpellTalentSystem.Items;

// ItemEffect.cs — pure 2-case union feeding Attributes and Talents
public sealed record GrantedTalent(Talent Talent);

public union ItemEffect(
    CharacterAttribute,
    GrantedTalent)
{
    public static implicit operator ItemEffect(PrimaryAttribute attr) => new CharacterAttribute(attr);
    public static implicit operator ItemEffect(SecondaryAttribute attr) => new CharacterAttribute(attr);
    public static implicit operator ItemEffect(SpecialAttribute attr) => new CharacterAttribute(attr);
}

