using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Shuttles;

/// <summary>
/// Hides a grid and the shuttles around it from radars of everyone who is not friendly to it
/// (see <see cref="FriendlyIffComponent"/>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RadarStealthFieldComponent : Component
{
    /// <summary>
    /// Non-friendly radars see this grid only when they are closer to it than this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RevealRange = 3000f;

    /// <summary>
    /// Other grids closer to this grid than this are hidden from non-friendly radars at any range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FieldRadius = 500f;
}
