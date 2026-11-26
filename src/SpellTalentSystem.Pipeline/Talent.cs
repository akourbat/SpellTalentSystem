using System;
using System.Collections.Immutable;
using SpellTalentSystem.Abilities;

namespace SpellTalentSystem.Pipeline;

/// <summary>
/// Represents a talent or trait that inspects an <see cref="Ability"/> and produces
/// a modified copy. Because <see cref="Ability"/> is an immutable record hierarchy,
/// untouched branches retain reference equality.
/// </summary>
public sealed record Talent(
    string Id,
    string Name,
    int Priority,
    Func<Ability, Ability> Apply);

/// <summary>
/// Represents a node in a flattened, talent-resolved ability execution tree.
/// </summary>
public sealed record ResolvedAbilityNode(
    Ability Ability,
    int Depth,
    string? ParentAbilityId,
    ImmutableArray<string> AppliedTalentIds);
