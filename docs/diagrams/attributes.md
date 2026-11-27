# Attributes Resolution Diagram

```text
[Base Character Attributes] + [Aggregated Stat Bonuses (Items, Boons, Traits)]
                                      │
                                      ▼
                    ┌───────────────────────────────────┐
                    │         AttributeSheet            │
                    │───────────────────────────────────│
                    │ Primary:   Power, Precision,      │
                    │            Toughness, Vitality    │
                    │ Secondary: Ferocity, HealingPower,│
                    │            ConditionDamage, etc.  │
                    │ Derived:   Armor (Tough+Defense), │
                    │            CritChance (Precision),│
                    │            CritDamage (Ferocity), │
                    │            MaxHealth (Vitality)   │
                    └───────────────────────────────────┘
```
