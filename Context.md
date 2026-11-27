# Project Context — SpellTalentSystem (`Option-2` Branch)

## Tech Stack & Conventions
- **Runtime / Language**: .NET 11 (`TargetFramework: net11.0` in `Directory.Build.props`), C# 15 (`closed record`, `union` discriminated unions, positional `init` properties).
- **Reactive Engine**: `DynamicData` (`SourceCache<T, TKey>`, `Transform`, `Merge`, `Bind`) + `System.Reactive`.
- **Testing**: `xunit.v3` (`[Fact]`, `[Theory]`).
- **Design Rules**:
  - Immutable `record` / `readonly record struct` types with `ImmutableArray<T>`.
  - Positional initialization for records; all properties **PascalCase**.
  - `Nature` / `Category` DUs carry **zero hard numbers** (pure semantic classification).

## Completed Modules & Diagrams
- **`SpellTalentSystem.Abilities` & `SpellTalentSystem.Pipeline`**: `closed record Ability` (`PrimaryAbility` / `DependentAbility`), 4 orthogonal DUs (`Nature`, `Targeting`, `Delivery`, `Effect`), `StatValue<T>`, `CharacterAttributeId`, and stateless `AbilityTalentPipeline`. → See `docs/diagrams/abilities-pipeline.md`
- **`SpellTalentSystem.Attributes`**: `AttributeSheet` resolving Primary (`Power`, `Precision`, `Toughness`, `Vitality`), Secondary (`Ferocity`, `HealingPower`, `ConditionDamage`, `Concentration`, `Expertise`), and Derived stats (`Armor`, `CritChance`, `CritDamage`, `MaxHealth`). → See `docs/diagrams/attributes.md`
- **`SpellTalentSystem.Items`**: `closed record Item` (`ContainerItem` / `LeafItem`), 2-level `ItemCategory` DU of DUs, `ItemEffect` DU (`ItemStatBonus`, `GrantedTalent`, `UsableAbility`), `ItemLifecycle`, `ItemSetDefinition`, and `EquipmentAggregator`. → See `docs/diagrams/items-hierarchy.md` and `docs/diagrams/items-category.md`
- **`SpellTalentSystem.Showcase`**: Interactive Blazor playground.

## Key Architectural Boundary
- `SpellTalentSystem.Items` **does not** resolve final `AttributeSheet` values. `EquipmentAggregator.Aggregate(equippedItems, setDefinitions)` outputs raw `AggregatedStats` (`ImmutableArray<ItemStatBonus>`) and `GrantedTalents` (`ImmutableArray<Talent>`), ready to merge with the upcoming Boon system before final `AttributeSheet` and combat resolution.

## Planned Next Milestones
1. **Equipment Loadout & Category-Driven Validation (`SpellTalentSystem.Items`)**: `EquipmentLoadout` (slot assignment, weapon swaps, two-hand vs. main/off-hand occupancy, socket insertion rules derived from `ItemCategory` + `Lifecycle.RequiredLevel`).
2. **Boon & Condition System (`SpellTalentSystem.Boons`)**: Stackable / intensity-based effects emitting `(ImmutableArray<ItemStatBonus>, ImmutableArray<Talent>)` to merge with `EquipmentAggregator`.
3. **Reactive `AttributeSheet` + Cast-Time Conditionals (`ResolveForCast` & `.Watch(...)`)**: Combine Equipment + Boons + Base Stats into reactive `AttributeSheet`, supporting caster-state predicates (e.g., *Scholar* +5% damage while HP > 90%) and ICD threshold triggers.
4. **Combat Resolution (`SpellTalentSystem.Combat`)**: Hit, crit, strike/condition damage, and healing evaluation using resolved `AttributeSheet` + talent-enhanced `Ability` trees.
