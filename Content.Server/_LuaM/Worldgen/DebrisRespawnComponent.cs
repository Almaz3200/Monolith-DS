namespace Content.Server._LuaM.Worldgen;

/// <summary>
/// Generates the debris of a loaded world chunk again every <see cref="Interval"/>, see <see cref="DebrisRespawnSystem"/>.
/// Added to chunks by space biomes.
/// </summary>
[RegisterComponent]
public sealed partial class DebrisRespawnComponent : Component
{
    /// <summary>
    /// How often the debris of the chunk is generated again while the chunk stays loaded.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromMinutes(20);

    /// <summary>
    /// How long before the respawn the players nearby are warned.
    /// </summary>
    [DataField]
    public TimeSpan WarningTime = TimeSpan.FromMinutes(1);

    [ViewVariables]
    public TimeSpan NextRespawn;

    /// <summary>
    /// Whether the players nearby were already warned about the next respawn.
    /// </summary>
    [ViewVariables]
    public bool Warned;
}
