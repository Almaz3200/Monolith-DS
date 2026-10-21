namespace Content.Server._LuaM.Research;

/// <summary>
/// Pulls research point disks lying near the R&amp;D server and adds their points to it.
/// The magnet turns itself off <see cref="ActiveDuration"/> after being turned on.
/// </summary>
[RegisterComponent]
public sealed partial class ResearchServerMagnetComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Range = 1.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool MagnetEnabled;

    /// <summary>
    /// How long the magnet stays on after being turned on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(20);

    [ViewVariables]
    public TimeSpan DisableAt = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextScan = TimeSpan.Zero;
}
