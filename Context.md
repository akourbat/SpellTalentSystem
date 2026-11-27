# Project Context — SpellTalentSystem (`Option-2` Branch)

## Tech Stack & Conventions
- **Runtime / Language**: .NET 11 (`TargetFramework: net11.0` in `Directory.Build.props`), C# 15 (`closed record`, `union` discriminated unions, positional `init` properties).
- **Reactive Engine**: `DynamicData` (`SourceCache<T, TKey>`, `Transform`, `Merge`, `Bind`) + `System.Reactive`.
- **Testing**: `xunit.v3` (`[Fact]`, `[Theory]`).
- **Design Rules**:
  - Immutable `record` / `readonly record struct` types with `ImmutableArray<T>`.
  - Positional initialization for records; all properties **PascalCase**.
  - `Nature` / `Category` DUs carry **zero hard numbers** (pure semantic classification).
  - Every modifiable numeric value uses `StatValue<T>(Base, FlatModifier, IncreasedPercent)` keeping Base, Flat, and % buckets separate until `.Resolve()` is called.

## Completed Modules & Diagrams
- **`SpellTalentSystem.Abilities`**:
  - `closed record Ability` (`PrimaryAbility` / `DependentAbility`), 4 orthogonal DUs (`AbilityNature`, `Targeting`, `Delivery`, `AbilityEffect` including `ApplyBoon(BoonType, StatValue<TimeSpan>, StatValue<int>)`).
  - `StatValue<T>` (`double`, `int`, `TimeSpan`) with monoid `Combine`, `AddFlat`, `AddIncreased`, implicit `T` conversion, and `.Resolve()` -> `(Base + FlatModifier) * (1.0 + IncreasedPercent)`.
  - `CharacterAttribute` DU (`PrimaryAttribute`, `SecondaryAttribute`, `SpecialAttribute`) keyed by `AttributeKind` with pattern-matched `Deconstruct()` / `FromKind()` and monoid `CharacterAttribute.Aggregate(...)`.
  - Diagram: `docs/diagrams/abilities-pipeline.md`
- **`SpellTalentSystem.Attributes`**:
  - `AttributeSheet` holding `ImmutableDictionary<AttributeKind, StatValue<double>>` aggregated from Base/Leveling, Equipment, Talents, and Boons.
  - Evaluates final values and cross-stat conversions (`Precision` -> `CriticalChance`, `Ferocity` -> `CriticalDamage`, `Concentration` -> `BoonDuration`, `Expertise` -> `ConditionDuration`, `Toughness + Defense` -> `Armor`, `Vitality` -> `MaxHealth`, `ResolveBoonDuration(ApplyBoon)`) **only when queried**.
  - Diagram: `docs/diagrams/attributes.md`
- **`SpellTalentSystem.Pipeline`**:
  - Stateless reactive `AbilityTalentPipeline` applying priority-ordered `Talent` tree transformations via `DynamicData`.
  - Diagram: `docs/diagrams/abilities-pipeline.md`
- **`SpellTalentSystem.Items`**:
  - `closed record Item` (`ContainerItem` / `LeafItem`), 2-level `ItemCategory` DU of DUs, `ItemEffect` DU (`CharacterAttribute`, `GrantedTalent`, `UsableAbility`), `ItemLifecycle`, `ItemSetDefinition`, and `EquipmentAggregator`.
  - Diagrams: `docs/diagrams/items-hierarchy.md` and `docs/diagrams/items-category.md`
- **`SpellTalentSystem.Showcase` & `SpellTalentSystem.Tests`**:
  - Interactive Blazor playground and full green xUnit v3 test suite.

## Key Architectural Boundary
- `SpellTalentSystem.Items` **does not** resolve final `AttributeSheet` values. `EquipmentAggregator.Aggregate(equippedItems, setDefinitions)` outputs aggregated `CharacterAttribute` buckets (`AggregatedAttributes`) and `GrantedTalents` (`ImmutableArray<Talent>`).
- These streams merge with the upcoming **Boon / Condition System** before final `AttributeSheet` calculation and combat resolution.

## Planned Next Milestones
1. **Equipment Loadout & Category-Driven Validation (`SpellTalentSystem.Items`)**: `EquipmentLoadout` (slot assignment, weapon swaps, two-hand vs. main/off-hand occupancy, socket insertion rules derived from `ItemCategory` + `Lifecycle.RequiredLevel`).
2. **Boon & Condition System (`SpellTalentSystem.Boons`)**: Stackable / intensity-based temporary effects emitting `(ImmutableArray<CharacterAttribute>, ImmutableArray<Talent>)` to merge with `EquipmentAggregator`.
3. **Reactive `AttributeSheet` + Cast-Time Conditionals (`ResolveForCast` & `.Watch(...)`)**: Combine Equipment + Boons + Base Stats into reactive `AttributeSheet`, supporting caster-state predicates (e.g., *Scholar* +5% damage while HP > 90%) and ICD threshold triggers.
4. **Combat Resolution (`SpellTalentSystem.Combat`)**: Hit, crit, strike/condition damage, healing, and boon application using resolved `AttributeSheet` + talent-enhanced `Ability` trees.
