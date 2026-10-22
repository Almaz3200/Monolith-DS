using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits;

public sealed partial class TraitSystem
{
    /// <summary>
    /// Several traits give damage protection, but only the first one could add the component.
    /// The rest write their modifiers into the component the mob already has.
    /// </summary>
    private void MergeDamageProtection(EntityUid mob, ComponentRegistry components)
    {
        if (!TryComp<DamageProtectionBuffComponent>(mob, out var existing))
            return;

        foreach (var entry in components.Values)
        {
            if (entry.Component is not DamageProtectionBuffComponent trait)
                continue;

            foreach (var (key, modifier) in trait.Modifiers)
                existing.Modifiers[key] = modifier;
        }
    }
}
