using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Sandevistan;

/// <summary>
/// Shows the Sandevistan glitch shader to the user while the Sandevistan is running.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanGlitchComponent : Component
{
    /// <summary>
    /// Strength of the glitch, 0 to 1.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Intensity = 1f;
}
