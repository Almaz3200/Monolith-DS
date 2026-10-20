using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Movement;

/// <summary>
/// Added to a mob for the duration of a jump or dash.
/// The first creature hit stops the jump and gets knocked down.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DashImpactComponent : Component
{
    /// <summary>
    /// How long the creature that was hit stays on the floor. It is a knockdown, not a stun.
    /// </summary>
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);
}
