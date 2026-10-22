using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.PartExchanger;

/// <summary>
/// Limits what a RPED-like storage is allowed to suck in: machine parts, power cells or both.
/// The storage whitelist itself must allow everything the mixed mode should accept.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RpedPickupModeComponent : Component
{
    [DataField, AutoNetworkedField]
    public RpedPickupMode Mode = RpedPickupMode.Components;

    /// <summary>
    /// If false, items lying in the world are not sucked in (area/quick insert); inserting from hands still works.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PickupEnabled = true;

    /// <summary>
    /// Emptying takes <see cref="EmptyDelayMin"/> plus <see cref="EmptyDelayPerItem"/> per stored item, up to <see cref="EmptyDelayMax"/>.
    /// </summary>
    [DataField]
    public TimeSpan EmptyDelayMin = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan EmptyDelayMax = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan EmptyDelayPerItem = TimeSpan.FromSeconds(0.05);
}

[Serializable, NetSerializable]
public sealed partial class RpedEmptyDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public enum RpedPickupMode : byte
{
    /// <summary>Machine parts only.</summary>
    Components,

    /// <summary>Power cells only.</summary>
    Batteries,

    /// <summary>Machine parts and power cells.</summary>
    Mixed,
}
