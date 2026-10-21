namespace Content.Server._Mono.NPC.HTN;

/// <summary>
/// Makes entities marked this be targeted by ship HTN.
/// </summary>
[RegisterComponent]
public sealed partial class ShipNpcTargetComponent : Component
{
    [DataField]
    public bool NeedPower = false;

    [DataField]
    public NpcTargetGridMode NeedGrid = NpcTargetGridMode.OnGrid;

// LuaM-start:
// LuaM-start:
    /// <summary>
    /// Maximum distance at which drones can consider this entity a target.
    /// </summary>
    [DataField]
    public float? MaxTargetRange = null;
// LuaM-end.
// LuaM-end.
}

public enum NpcTargetGridMode
{
    OnGrid,
    Either,
    NoGrid
}
