using Content.Shared.Materials.OreSilo;
using Content.Shared.Whitelist;

namespace Content.Shared._LuaM.Materials;

/// <summary>
/// Pulls items lying around straight into the material silo this machine is linked to (see <see cref="OreSiloClientComponent"/>),
/// the machine itself keeps nothing. Works while the machine is powered and toggled on.
/// </summary>
[RegisterComponent]
public sealed partial class SiloMagnetPickupComponent : Component
{
    /// <summary>
    /// How far from the machine items are pulled.
    /// </summary>
    [DataField]
    public float Range = 30f;

    /// <summary>
    /// Which items are pulled.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    [ViewVariables]
    public TimeSpan NextScan;
}
