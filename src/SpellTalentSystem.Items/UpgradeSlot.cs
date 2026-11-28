using System;
using System.Collections.Immutable;

namespace SpellTalentSystem.Items;

/// <summary>
/// Explicit upgrade socket on <see cref="Gear"/> (or <see cref="GearUpgrade"/> for nested StatAffixes)
/// declaring the permitted <see cref="UpgradeKind"/> and optional slotted <see cref="GearUpgrade"/>.
/// </summary>
public sealed record UpgradeSlot(
    UpgradeKind AcceptedKind,
    GearUpgrade? Slotted = null)
{
    public UpgradeKind AcceptedKind { get; init; } = AcceptedKind;

    public GearUpgrade? Slotted
    {
        get;
        init
        {
            if (value is not null && value.Category.Kind != AcceptedKind)
            {
                throw new ArgumentException(
                    $"Upgrade slot accepts {AcceptedKind}, but received {value.Category.Kind} ('{value.Name}').",
                    nameof(Slotted));
            }
            field = value;
        }
    } = ValidateSlotted(AcceptedKind, Slotted);

    public bool IsEmpty => Slotted is null;

    private static GearUpgrade? ValidateSlotted(UpgradeKind acceptedKind, GearUpgrade? slotted)
    {
        if (slotted is not null && slotted.Category.Kind != acceptedKind)
        {
            throw new ArgumentException(
                $"Upgrade slot accepts {acceptedKind}, but received {slotted.Category.Kind} ('{slotted.Name}').",
                nameof(Slotted));
        }
        return slotted;
    }

    internal static ImmutableArray<UpgradeSlot> SlotAt(
        ImmutableArray<UpgradeSlot> slots,
        int index,
        GearUpgrade? upgrade,
        string ownerName)
    {
        if ((uint)index >= (uint)slots.Length)
            throw new ArgumentOutOfRangeException(nameof(index), $"'{ownerName}' has {slots.Length} upgrade slots.");

        return slots.SetItem(index, slots[index] with { Slotted = upgrade });
    }

    internal static ImmutableArray<UpgradeSlot> SlotFirstAvailable(
        ImmutableArray<UpgradeSlot> slots,
        GearUpgrade upgrade,
        string ownerName)
    {
        var kind = upgrade.Category.Kind;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty && slots[i].AcceptedKind == kind)
                return slots.SetItem(i, slots[i] with { Slotted = upgrade });
        }

        throw new InvalidOperationException(
            $"'{ownerName}' has no empty upgrade slot accepting {kind} for '{upgrade.Name}'.");
    }
}
