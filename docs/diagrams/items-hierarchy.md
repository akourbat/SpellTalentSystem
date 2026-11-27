# Item Tree & Composed Value Objects Diagram

```text
                        ┌──────────────────────────────────────────────┐
                        │              closed record Item              │
                        │──────────────────────────────────────────────│
                        │ + Id: string                                 │
                        │ + Name: string                               │
                        │ + Category: ItemCategory (DU of DUs)         │
                        │ + Effects: ImmutableArray<ItemEffect>        │
                        │ + Lifecycle: ItemLifecycle                   │
                        └──────────────────────┬───────────────────────┘
                                               │
                         ┌─────────────────────┴─────────────────────┐
                         ▼                                           ▼
     ┌───────────────────────────────────────┐   ┌───────────────────────────────────────┐
     │     sealed record ContainerItem       │   │        sealed record LeafItem         │
     │───────────────────────────────────────│   │───────────────────────────────────────│
     │ + MaxChildSlots: int                  │   │ (Indivisible item: StatAffix, Sigil,  │
     │ + Children: ImmutableArray<Item> ─────┼──►│  Rune, Infusion, Potion, Ore, Quest)  │
     └───────────────────────────────────────┘   └───────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                 Composed Value Objects                                  │
├───────────────────────────────────────────┬─────────────────────────────────────────────┤
│   union ItemEffect (Gameplay Payloads)    │   ItemLifecycle (Progression & Economy)     │
│───────────────────────────────────────────│─────────────────────────────────────────────│
│ • ItemStatBonus(CharacterAttributeId,     │ • RequiredLevel: int (default 1)            │
│                 double Value)             │ • ItemLevel: int (default 1)                │
│ • GrantedTalent(Talent)                   │ • Rarity: ItemRarity                        │
│ • UsableAbility(Ability, Charges,         │ • Binding: BindingType                      │
│                 ConsumeOnUse)             │ • IsBound: bool                             │
│                                           │ • VendorCopper: long?                       │
│   ItemSet & Aggregation                   │ • IsQuestItem: bool                         │
│───────────────────────────────────────────│ • SpoilDuration: TimeSpan?                  │
│ • ItemSetDefinition(SetId, Name, Tiers)   │ • SetId: string?                            │
│ • SetBonusTier(RequiredPieces, Effects)   │                                             │
│ • EquipmentAggregator.Aggregate(...)      │ (Structural slot/proficiency rules derived  │
│   ├─► AggregatedStats (ItemStatBonus[])   │  by systems inspecting Item.Category)       │
│   ├─► GrantedTalents (Talent[])           │                                             │
│   └─► ActiveSetPieceCounts                │                                             │
└───────────────────────────────────────────┴─────────────────────────────────────────────┘
```
