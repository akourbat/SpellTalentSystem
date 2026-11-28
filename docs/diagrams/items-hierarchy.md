# Item Hierarchy & Slot Architecture

```text
                        ┌──────────────────────────────────────────────┐
                        │           closed record GameItem             │
                        │──────────────────────────────────────────────│
                        │ + Id: string                                 │
                        │ + Name: string                               │
                        │ + Description: string                        │
                        │ + Lifecycle: ItemLifecycle                   │
                        └──────────────────────┬───────────────────────┘
                                               │
        ┌──────────────────────┬───────────────┴───────┬──────────────────────┐
        ▼                      ▼                       ▼                      ▼
┌───────────────┐    ┌───────────────────┐   ┌───────────────────┐   ┌─────────────────┐
│     Gear      │    │    GearUpgrade    │   │  ConsumableItem   │   │   SimpleItem    │
│───────────────│    │───────────────────│   │───────────────────│   │─────────────────│
│ GearCategory  │    │ UpgradeCategory   │   │ ConsumableCategory│   │ SimpleCategory  │
│ RequiredLevel │    │ RequiredLevel     │   │ RequiredLevel     │   │ MaxStackSize    │
│ SetId?        │    │ SetId?            │   │ UsableAbility?    │   └─────────────────┘
│ BaseEffects   │    │ Effects           │   │ Effects           │
│ UpgradeSlots ─┼───►│ UpgradeSlots ─────┼─┐ │ SpoilDuration?    │
└───────────────┘    └───────────────────┘ │ │ Charges           │
                               ▲           │ └───────────────────┘
                               └───────────┘
                           (StatAffix slots only)

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                 Composed Value Objects                                  │
├───────────────────────────────────────────┬─────────────────────────────────────────────┤
│   UpgradeSlot & ItemEffect                │   ItemLifecycle (Shared Economy & Binding)  │
│───────────────────────────────────────────│─────────────────────────────────────────────│
│ • UpgradeSlot(AcceptedKind, Slotted?)     │ • Rarity: ItemRarity                        │
│ • ItemEffect:                             │ • Binding: BindingType                      │
│   ├─ CharacterAttribute (StatValue<double>)│ • IsBound: bool                            │
│   ├─ GrantedTalent(Talent)                │ • VendorCopper: long?                       │
│   └─ UsableAbility(Ability, Charges, ...) │                                             │
│                                           │   EquipmentAggregator.Aggregate(...)        │
│ • ItemSetDefinition(SetId, Name, Tiers)   │   ├─► AggregatedAttributes (StatValue +)    │
│ • SetBonusTier(RequiredPieces, Effects)   │   ├─► GrantedTalents (Talent[])             │
│                                           │   └─► ActiveSetPieceCounts                │
└───────────────────────────────────────────┴─────────────────────────────────────────────┘
```

```mermaid
classDiagram
    direction TB

    class GameItem {
        <<closed record>>
        +string Id
        +string Name
        +string Description
        +ItemLifecycle Lifecycle
    }

    class ItemLifecycle {
        <<record>>
        +ItemRarity Rarity
        +BindingType Binding
        +bool IsBound
        +long? VendorCopper
    }

    class Gear {
        <<sealed record>>
        +GearCategory Category
        +int RequiredLevel
        +string? SetId
        +ImmutableArray~ItemEffect~ BaseEffects
        +ImmutableArray~UpgradeSlot~ UpgradeSlots
        +WithSlotted(GearUpgrade) Gear
        +WithSlot(int, GearUpgrade?) Gear
        +CollectEffects() IEnumerable~ItemEffect~
    }

    class GearUpgrade {
        <<sealed record>>
        +UpgradeCategory Category
        +int RequiredLevel
        +string? SetId
        +ImmutableArray~ItemEffect~ Effects
        +ImmutableArray~UpgradeSlot~ UpgradeSlots
        +WithSlotted(GearUpgrade) GearUpgrade
        +WithSlot(int, GearUpgrade?) GearUpgrade
        +CollectEffects() IEnumerable~ItemEffect~
    }

    class ConsumableItem {
        <<sealed record>>
        +ConsumableCategory Category
        +int RequiredLevel
        +UsableAbility? UsableAbility
        +ImmutableArray~ItemEffect~ Effects
        +TimeSpan? SpoilDuration
        +int Charges
    }

    class SimpleItem {
        <<sealed record>>
        +SimpleCategory Category
        +int MaxStackSize
    }

    class UpgradeSlot {
        <<sealed record>>
        +UpgradeKind AcceptedKind
        +GearUpgrade? Slotted
        +bool IsEmpty
    }

    class ItemEffect {
        <<union>>
        +CharacterAttribute
        +GrantedTalent
        +UsableAbility
    }

    GameItem <|-- Gear
    GameItem <|-- GearUpgrade
    GameItem <|-- ConsumableItem
    GameItem <|-- SimpleItem

    GameItem *-- ItemLifecycle
    Gear *-- "0..*" UpgradeSlot
    Gear *-- "0..*" ItemEffect
    GearUpgrade *-- "0..*" UpgradeSlot : StatAffix only
    GearUpgrade *-- "0..*" ItemEffect
    UpgradeSlot o-- "0..1" GearUpgrade
```
