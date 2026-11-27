using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Attributes;

/// <summary>
/// A commutative modifier targeting any <see cref="CharacterAttributeId"/>
/// (from equipment affixes, runes, boons, or standard stat traits).
/// </summary>
public sealed record AttributeModifier(
    string SourceId,
    CharacterAttributeId Attribute,
    double FlatBonus = 0.0,
    double IncreasedPercent = 0.0,
    double MoreMultiplier = 1.0)
{
    public StatValue<double> ApplyTo(StatValue<double> current)
    {
        var updated = current;
        if (FlatBonus != 0.0)
            updated = updated.AddFlat(FlatBonus);
        if (IncreasedPercent != 0.0)
            updated = updated.AddIncreased(IncreasedPercent);
        if (MoreMultiplier != 1.0)
            updated = updated.MultiplyMore(MoreMultiplier);
        return updated;
    }
}

/// <summary>
/// A GW2-style conversion trait (e.g., "Gain Power equal to 10% of your Toughness").
/// Evaluated once after Primary/Secondary attributes resolve so conversions never chain infinitely.
/// </summary>
public sealed record AttributeConversion(
    string SourceId,
    CharacterAttributeId SourceAttribute,
    CharacterAttributeId TargetAttribute,
    double Ratio);

/// <summary>
/// Baseline character configuration (level-scaled base stats + profession base health).
/// </summary>
public sealed record CharacterBaseline(
    int Level,
    double BasePrimaryStat,
    double ProfessionBaseHealth)
{
    /// <summary>
    /// Standard GW2 Level 80 baseline (1000 Primary stats, 11,645 medium profession base health).
    /// </summary>
    public static CharacterBaseline Level80Standard(double professionBaseHealth = 11_645.0) =>
        new(Level: 80, BasePrimaryStat: 1000.0, ProfessionBaseHealth: professionBaseHealth);
}
