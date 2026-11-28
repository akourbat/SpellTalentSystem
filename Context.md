# Spell & Talent System — Architecture & Progress Context

## Solution Overview
A C# (.NET 10, C# preview `union` discriminated unions, `closed record` hierarchies, `ImmutableArray<T>`, and `DynamicData` reactive streams) RPG combat, attribute, talent, and equipment engine.

### Projects
1. **`SpellTalentSystem.Abilities`**:
   - **`StatValue<T>` (`readonly record struct`)**: Stack-allocated composite value `(Base, FlatModifier, IncreasedPercent)` for `double`, `int`, and `TimeSpan`.
     - **Strict Construction**: Implicit conversion from raw `T` is removed; callers must construct via `StatValue<T>.FromBase(...)`, `FromFlat(...)`, `FromPercent(...)`, or named constructor args so every layer explicitly declares its bucket.
     - **Uniform Composition (`+`)**: `left + right` sums `Base + Base`, `FlatModifier + FlatModifier`, and `IncreasedPercent + IncreasedPercent` across Talents, Gear, and Attributes.
     - **Final Evaluation**: `.Resolve()` computes `(Base + FlatModifier) * (1.0 + IncreasedPercent)`.
   - **`Ability` (`closed record`)**: `PrimaryAbility` (holds `CastTimeSeconds`, `CooldownSeconds`, and `SubAbilities`) vs. `DependentAbility` (triggered child sub-ability).
   - **Domain Unions**: `AbilityNature` (`ElementalNature`, `PhysicalNature`, `SoftControlNature`, `HardControlNature`, `BoonNature`, `DefensiveNature`, `HealingNature`), `AbilityTargeting`, `AbilityDelivery`, `AbilityEffect`.
   - **`CharacterAttribute` (`union`)**: Categorizes `PrimaryAttribute` (`Power`, `Precision`, `Toughness`, `Vitality`), `SecondaryAttribute` (`Ferocity`, `ConditionDamage`, `HealingPower`, `Expertise`, `Concentration`), and `SpecialAttribute` (`CriticalChance`, `CriticalDamage`, `BoonDuration`, `ConditionDuration`, `Defense`, `WeaponStrength`), each wrapping `StatValue<double>` and aggregating via `+`.

2. **`SpellTalentSystem.Pipeline`**:
   - **`Talent`**: `(Id, Name, Priority, Func<Ability, Ability> Apply)`. Lower `Priority` runs first (e.g., `0` for transformative nature conversions like Frost -> Fire; `100` for commutative numeric enrichments).
   - **`AbilityTalentPipeline` & `DynamicData` Extensions**: `.ResolveWithTalents(IObservable<IChangeSet<Talent, string>>)` reactively applies ordered talents and flattens each ability tree into `ImmutableArray<ResolvedAbilityNode>` with ancestor-path cycle detection (allowing shared sub-ability IDs across distinct sibling branches).

3. **`SpellTalentSystem.Attributes`**:
   - **`AttributeSheet`**: Stores un-collapsed `StatValue<double>` buckets per `AttributeKind` and evaluates derived combat stats (`EffectivePower`, `EffectiveCritChance`, `EffectiveCritDamage`, `EffectiveBoonDuration`, `EffectiveConditionDuration`, `EffectiveArmor`, `EffectiveMaxHealth`).

4. **`SpellTalentSystem.Items`** *(Refactored)*:
   - **`GameItem` (`closed record`)**: Shared root carrying only `Id`, `Name`, `Description`, and `ItemLifecycle` (`Rarity`, `Binding`, `IsBound`, `VendorCopper` — `ItemLevel` removed).
   - **`Gear` (`sealed record`)**: Equippable weapons, armor, trinkets, and `Relic`s (`GearCategory`). Holds `RequiredLevel`, `SetId`, `BaseEffects`, and explicit typed `ImmutableArray<UpgradeSlot>` (`WithSlot`, `WithSlotted`, `CollectEffects`).
   - **`GearUpgrade` (`sealed record`)**: Socketable enhancements (`UpgradeCategory`: `StatAffix`, `Sigil`, `Rune`, `Infusion`, `Gem`, `Enchantment`) carrying `CharacterAttribute` and/or `GrantedTalent` effects, plus optional nested `UpgradeSlots` restricted to `UpgradeKind.StatAffix`.
   - **`ConsumableItem` (`sealed record`)**: Materialized combat/buff items (`ConsumableCategory`: `Potion`, `Food`, `UtilityOil`, `Scroll`) with `UsableAbility`, `Effects`, `SpoilDuration`, and `Charges`.
   - **`SimpleItem` (`sealed record`)**: Non-equippable inventory items (`SimpleCategory`: `CraftingMaterial`, `Trophy`, `QuestItem`, `SalvageKit`, `CurrencyToken`) with `MaxStackSize`.
   - **`UpgradeSlot` (`sealed record`)**: Explicit `(UpgradeKind AcceptedKind, GearUpgrade? Slotted)` enforcing socket compatibility at construction/slotting time.
   - **`EquipmentAggregator`**: Recursively traverses equipped `Gear`, slotted `GearUpgrade`s, nested `StatAffix`es, and `ItemSetDefinition` tier thresholds to produce `AggregatedEquipment` (`AggregatedAttributes`, `GrantedTalents`, `ActiveSetPieceCounts`).

5. **`SpellTalentSystem.Showcase` & `SpellTalentSystem.Tests`**:
   - Interactive Blazor Server demo (`Home.razor`) showcasing real-time `DynamicData` ability tree diffs.
   - Full xUnit v3 test coverage across `Abilities`, `Pipeline`, `Attributes`, and `Items` (all passing).

---

## Next Planned Milestones
1. **Reactive Character Loadout Pipeline**: Connect `SourceCache<Gear, string>` + `SourceCache<Talent, string>` + base `AttributeSheet` + base `PrimaryAbility` trees so equipping/unequipping gear reactively updates both `AttributeSheet` and `ResolvedAbilityNode` trees (via `GrantedTalents` and `ConsumableItem` `UsableAbility`).
2. **Combat Formula Resolution**: Evaluate a `ResolvedAbilityNode` against caster and target `AttributeSheet`s (direct damage, critical strikes, condition ticks, boon/condition durations).
3. **Database Persistence DTOs**: Lightweight serializable snapshots for `Gear` and its `UpgradeSlots`.
