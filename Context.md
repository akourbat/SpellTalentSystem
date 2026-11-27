# SpellTalentSystem — Project Context

## Tech Stack & Environment
- **.NET 11 (`net11.0`)**, **C# 15** (using C# 15 `union` types), **DynamicData**, **System.Reactive**, **xUnit v3**, **Blazor** (`Home.razor`).
- Branch: `Option-2` (stateless extension-based DynamicData pipeline).

## Solution Projects
1. `SpellTalentSystem.Abilities` — Pure domain primitives (`Ability`, `PrimaryAbility`, `DependentAbility`, C# 15 `union` types for `AbilityNature`, `AbilityTargeting`, `AbilityDelivery`, `AbilityEffect`, and commutative `StatValue<T>` buckets: `Base`, `FlatModifier`, `IncreasedPercent`, `MoreMultiplier`, `.Resolve()`). Zero external dependencies.
2. `SpellTalentSystem.Pipeline` — Stateless reactive DynamicData extension pipeline (`Observable.Defer` + `SortAndBind` + `QueryWhenChanged` + `TransformOnObservable`) with structural-sharing tree transforms (`TransformTree`, `MapEffect`, `MapTargeting`, `MapPrimaryTiming`).
3. `SpellTalentSystem.Attributes` — Character attribute sheet & modifiers (`AttributeSheet.cs`, `AttributeModifiers.cs`).
4. `SpellTalentSystem.Tests` — xUnit v3 test suite wired into VS Code Test Explorer.
5. Blazor Web App (`Components/Pages/Home.razor`) — Interactive UI visualizer.

## Core Design Principles
- **Immutability & Structural Sharing:** All abilities, stats, and attributes are immutable records/structs. Untouched subtrees preserve reference equality.
- **Commutative Stat Enrichment:** Numeric modifiers accumulate into commutative buckets (`FlatModifier`, `IncreasedPercent`, `MoreMultiplier`) so numeric talents are order-independent; `Priority` is reserved for structural/transformative talents.
- **Reactive Purity:** Pipelines use `Observable.Defer` + DynamicData operators with zero `BehaviorSubject` state.
- **Conditional & Item Architecture (Planned Next):**
  - Lazy cast-time predicates (`ResolveForCast`) for caster-state modifiers (e.g., GW2 *Scholar*: +10% while HP > 90%).
  - Reactive `.Watch(key)` + `.DistinctUntilChanged()` triggers for autonomous threshold reactions (e.g., cast *Lesser Regen* when HP < 50%, with ICD).
  - `SpellTalentSystem.Items`: `EquipSlot` (TwoHand vs MainHand+OffHand mutual exclusion), weapon swaps, Rune set counts (1–6 pc), and Sigils projecting into `Attributes`, `_baseAbilities`, and `_stableTalents`.
