# Attributes & Composite StatValue<T> Resolution Diagram

```text
                    ┌─────────────────────────────────────────┐
                    │         StatValue<T> (Monoid)           │
                    │─────────────────────────────────────────│
                    │ + Base: T                               │
                    │ + FlatModifier: T                       │
                    │ + IncreasedPercent: double              │
                    │                                         │
                    │ Combine(a, b) -> sums Base, Flat, Inc%  │
                    │ Resolve() -> (Base + Flat) * (1 + Inc%) │
                    └────────────────────┬────────────────────┘
                                         │ carried inside
                                         ▼
                    ┌─────────────────────────────────────────┐
                    │        union CharacterAttribute         │
                    │         (keyed by AttributeKind)        │
                    ├────────────────────┬────────────────────┤
                    │ PrimaryAttribute   │ SecondaryAttribute │
                    │ • Power            │ • Ferocity         │
                    │ • Precision        │ • ConditionDamage  │
                    │ • Toughness        │ • HealingPower     │
                    │ • Vitality         │ • Expertise        │
                    ├────────────────────┤ • Concentration    │
                    │ SpecialAttribute   └────────────────────┤
                    │ • CriticalChance   • ConditionDuration  │
                    │ • CriticalDamage   • Defense            │
                    │ • BoonDuration     • WeaponStrength     │
                    └────────────────────┬────────────────────┘
                                         │
        [Base / Leveling] + [Equipment] + [Talents] + [Boons]
                                         │
                                         ▼
                    ┌─────────────────────────────────────────┐
                    │             AttributeSheet              │
                    │─────────────────────────────────────────│
                    │ Stores un-collapsed StatValue<double>   │
                    │ buckets per AttributeKind until called: │
                    │                                         │
                    │ • GetPower(), GetVitality(), ...        │
                    │ • GetCriticalChance() (Crit + Precision)│
                    │ • GetCriticalDamageMultiplier() (+Fero) │
                    │ • GetBoonDurationBonus() (+Conc)        │
                    │ • ResolveBoonDuration(ApplyBoon)        │
                    │ • GetArmor(), GetMaxHealth()            │
                    └─────────────────────────────────────────┘
```
