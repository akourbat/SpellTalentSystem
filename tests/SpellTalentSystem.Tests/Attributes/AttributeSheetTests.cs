using System;
using SpellTalentSystem.Abilities;
using SpellTalentSystem.Attributes;
using Xunit;

namespace SpellTalentSystem.Tests.Attributes;

public class AttributeSheetTests
{
    [Fact]
    public void CombineWith_KeepsBaseFlatAndPercentSeparateUntilResolved()
    {
        var baseSheet = AttributeSheet.CreateLevel80Base(); // Power Base = 1000

        var combined = baseSheet.CombineWith(
        [
            new PrimaryAttribute(new Power(StatValue<double>.FromFlat(250))),
            new PrimaryAttribute(new Power(StatValue<double>.FromPercent(0.10)))
        ]);

        var bucket = combined.GetBucket(AttributeKind.Power);
        Assert.Equal(1000, bucket.Base);
        Assert.Equal(250, bucket.FlatModifier);
        Assert.Equal(0.10, bucket.IncreasedPercent, precision: 6);

        // (1000 + 250) * 1.10 = 1375
        Assert.Equal(1375, combined.GetPower(), precision: 6);
    }

    [Fact]
    public void CriticalChance_CombinesDirectCritBucketWithPrecisionConversion()
    {
        var sheet = AttributeSheet.CreateLevel80Base().CombineWith(
        [
            new PrimaryAttribute(new Precision(StatValue<double>.FromFlat(1050))), // +1050 Precision -> +50% Crit
            new SpecialAttribute(new CriticalChance(StatValue<double>.FromFlat(0.05))) // +5% direct Crit
        ]);

        // 5% base + 5% direct + 50% from Precision = 60%
        Assert.Equal(0.60, sheet.GetCriticalChance(), precision: 6);
    }

    [Fact]
    public void ResolveBoonDuration_CombinesSkillBaseFlatPercentWithCharacterBoonDuration()
    {
        // Skill: 10s base + 5s flat + 10% skill duration = 16.5s
        var applyMight = new ApplyBoon(
            Boon: new Might(),
            Duration: new StatValue<TimeSpan>(
                Base: TimeSpan.FromSeconds(10),
                FlatModifier: TimeSpan.FromSeconds(5),
                IncreasedPercent: 0.10),
            Stacks: StatValue<int>.FromBase(5));

        // Character: +15% BoonDuration + 150 Concentration (+10%) = +25% total BoonDuration
        var sheet = AttributeSheet.CreateLevel80Base().CombineWith(
        [
            new SpecialAttribute(new BoonDuration(StatValue<double>.FromFlat(0.15))),
            new SecondaryAttribute(new Concentration(StatValue<double>.FromFlat(150)))
        ]);

        // 16.5s * 1.25 = 20.625s
        var resolved = sheet.ResolveBoonDuration(applyMight);
        Assert.Equal(20.625, resolved.TotalSeconds, precision: 3);
    }
}
