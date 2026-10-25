using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.HoloComms;

/// <summary>
/// A console with the holographic comms window.
/// The calls go through a hidden holopad module attached to the console, so the console's own intercom keeps its microphone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HoloCommsConsoleComponent : Component
{
    [DataField]
    public EntProtoId ModulePrototype = "HoloCommsModule";

    /// <summary>
    /// The hidden holopad that places and takes the calls of this console.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Module;

    [ViewVariables, AutoNetworkedField]
    public string? RemoteName;
}

/// <summary>
/// The hidden holopad of a <see cref="HoloCommsConsoleComponent"/>, lives as a child of the console.
/// </summary>
[RegisterComponent]
public sealed partial class HoloCommsModuleComponent : Component
{
    [DataField]
    public EntityUid? Console;
}

/// <summary>
/// A comms point that is a part of its ship's line: a call to the ship rings every point,
/// and once someone answers, every point of both ships joins the call.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShipCommsLineComponent : Component
{
    /// <summary>
    /// Off for the holopads that stand on their own while sharing the prototype, like the POI ones.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// The ship takes no calls. Kept the same on every point of the ship.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoNotDisturb;
}
