# Domain-Scoped Item Category Unions

```text
         ┌───────────────────────────────────────────────────────────────────────┐
         │                   Domain-Scoped Category Unions                       │
         └───────────────────────────────────┬───────────────────────────────────┘
                                             │
        ┌────────────────────┬───────────────┴───────┬────────────────────┐
        ▼                    ▼                       ▼                    ▼
┌───────────────┐    ┌───────────────┐       ┌───────────────┐    ┌───────────────┐
│ GearCategory  │    │UpgradeCategory│       │  Consumable   │    │SimpleCategory │
│    (Gear)     │    │ (GearUpgrade) │       │   Category    │    │ (SimpleItem)  │
└───────┬───────┘    └───────┬───────┘       └───────┬───────┘    └───────┬───────┘
        │                    │                       │                    │
        ├─ WeaponCategory    ├─ StatAffix            ├─ Potion            ├─ CraftingMaterial
        │  (Sword..Rifle)    ├─ Sigil                ├─ Food              ├─ Trophy
        ├─ ArmorCategory     ├─ Rune                 ├─ UtilityOil        ├─ QuestItem
        │  (Weight + Piece)  ├─ Infusion             └─ Scroll            ├─ SalvageKit
        └─ TrinketCategory   ├─ Gem                                       └─ CurrencyToken
           ├─ Amulet         └─ Enchantment
           ├─ Ring
           ├─ Accessory
           ├─ Backpiece
           └─ Relic
```

```mermaid
classDiagram
    direction LR

    class GearCategory {
        <<union>>
        +WeaponCategory
        +ArmorCategory
        +TrinketCategory
    }

    class TrinketCategory {
        <<union>>
        +Amulet
        +Ring
        +Accessory
        +Backpiece
        +Relic
    }

    class UpgradeCategory {
        <<union>>
        +StatAffix
        +Sigil
        +Rune
        +Infusion
        +Gem
        +Enchantment
        +UpgradeKind Kind
    }

    class ConsumableCategory {
        <<union>>
        +Potion
        +Food
        +UtilityOil
        +Scroll
    }

    class SimpleCategory {
        <<union>>
        +CraftingMaterial
        +Trophy
        +QuestItem
        +SalvageKit
        +CurrencyToken
    }

    GearCategory --> TrinketCategory
```
