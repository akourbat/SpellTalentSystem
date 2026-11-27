namespace SpellTalentSystem.Abilities;

// 1. Primary Attributes (scale with character level + gear/traits/boons)
public record struct Power();
public record struct Precision();
public record struct Toughness();
public record struct Vitality();
public union PrimaryAttribute(Power, Precision, Toughness, Vitality);

// 2. Secondary Attributes (base 0; gained from gear/runes/traits/food)
public record struct Ferocity();
public record struct ConditionDamage();
public record struct Expertise();
public record struct Concentration();
public record struct HealingPower();
public union SecondaryAttribute(Ferocity, ConditionDamage, Expertise, Concentration, HealingPower);

// 3. Derived Attributes (computed via formula from Primary/Secondary + direct trait/boon modifiers)
public record struct Armor();
public record struct MaxHealth();
public record struct CriticalChance();
public record struct CriticalDamage();
public record struct ConditionDuration();
public record struct BoonDuration();
public union DerivedAttribute(
    Armor, MaxHealth, CriticalChance, CriticalDamage, ConditionDuration, BoonDuration);

// 4. Equipment-Intrinsic & Combat Resource Attributes
public record struct WeaponStrength();
public record struct Defense();
public record struct MovementSpeed();
public record struct HealthPercent();
public record struct EndurancePercent();
public record struct ActiveBoonCount();
public record struct ActiveConditionCount();
public union SpecialAttribute(
    WeaponStrength,
    Defense,
    MovementSpeed,
    HealthPercent,
    EndurancePercent,
    ActiveBoonCount,
    ActiveConditionCount);

/// <summary>
/// Subset of attributes allowed directly on gear affixes (Primary + Secondary only).
/// </summary>
public union EquipableAttribute(PrimaryAttribute, SecondaryAttribute);

/// <summary>
/// Top-level discriminated union identifying any character attribute in the system.
/// </summary>
public union CharacterAttributeId(
    PrimaryAttribute,
    SecondaryAttribute,
    DerivedAttribute,
    SpecialAttribute)
{
    public static CharacterAttributeId FromEquipable(EquipableAttribute attr) =>
        attr.Value switch
        {
            PrimaryAttribute p => p,
            SecondaryAttribute s => s,
            _ => throw new System.InvalidOperationException($"Unsupported EquipableAttribute: {attr}")
        };
}
