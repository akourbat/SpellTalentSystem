# Abilities & Reactive Talent Pipeline Diagram

```text
                  ┌──────────────────────────────────────────┐
                  │          closed record Ability           │
                  │──────────────────────────────────────────│
                  │ + Id, Name                               │
                  │ + Nature: AbilityNature (DU)             │
                  │ + Targeting: Targeting (DU)              │
                  │ + Delivery: Delivery (DU)                │
                  │ + Effect: Effect (DU)                    │
                  │ + CastTime, Cooldown: StatValue<double>  │
                  │ + ResourceCosts: ImmutableArray<...>     │
                  └────────────────────┬─────────────────────┘
                                       │
                 ┌─────────────────────┴─────────────────────┐
                 ▼                                           ▼
 ┌───────────────────────────────┐           ┌───────────────────────────────┐
 │  sealed record PrimaryAbility │           │ sealed record DependentAbility│
 │───────────────────────────────│           │───────────────────────────────│
 │ (Standalone / root skill)     │           │ + Trigger: TriggerCondition   │
 │                               │           │ + ParentId: string?           │
 └───────────────────────────────┘           └───────────────────────────────┘

SourceCache<Ability, string> ──┐
                               ├──► AbilityTalentPipeline ──► ReadOnlyObservableCollection<ResolvedAbility>
SourceCache<Talent, string>  ──┘    (Priority-ordered immutable tree rewrites via DynamicData)
```
