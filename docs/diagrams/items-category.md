# ItemCategory DU-of-DUs Hierarchy Diagram

```text
                                 ┌─────────────────────────┐
                                 │   union ItemCategory    │
                                 │  ("What is this item?") │
                                 └────────────┬────────────┘
                                              │
     ┌──────────────┬──────────────┬──────────┴───┬──────────────┬──────────────┬──────────────┐
     ▼              ▼              ▼              ▼              ▼              ▼              ▼
┌──────────┐  ┌──────────┐  ┌───────────┐  ┌───────────┐  ┌────────────┐  ┌──────────┐  ┌──────────┐
│  Weapon  │  │  Armor   │  │  Trinket  │  │  Upgrade  │  │ Consumable │  │ Material │  │   Misc   │
│ Category │  │ Category │  │ Category  │  │ Category  │  │  Category  │  │ Category │  │ Category │
└────┬─────┘  └────┬─────┘  └─────┬─────┘  └─────┬─────┘  └─────┬──────┘  └────┬─────┘  └────┬─────┘
     │             │              │              │              │              │             │
     ├─ Sword      ├─ Weight:     ├─ Amulet      ├─ StatAffix   ├─ Potion      ├─ Ore        ├─ QuestToken
     ├─ Greatsword │  Light/Med/  ├─ Ring        ├─ Sigil       ├─ Food        ├─ Ingot      ├─ SalvageKit
     ├─ Dagger     │  HeavyArmor  ├─ Accessory   ├─ Rune        ├─ UtilityOil  ├─ Wood       ├─ Trophy
     ├─ Axe        └─ Piece:      └─ Backpiece   ├─ Infusion    └─ Scroll      ├─ Plank      └─ ContainerBag
     ├─ Mace          Helm,                      ├─ Gem                        ├─ Leather
     ├─ Hammer        Shoulders,                 └─ Enchantment                ├─ Cloth
     ├─ Staff         Chestpiece,                                              ├─ Herb
     ├─ Scepter       Gloves,                                                  └─ Reagent
     ├─ Focus         Leggings,
     ├─ Shield        Boots
     ├─ Longbow
     ├─ Shortbow
     ├─ Pistol
     └─ Rifle
```
