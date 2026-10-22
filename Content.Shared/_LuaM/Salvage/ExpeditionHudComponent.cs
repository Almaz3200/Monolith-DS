using Content.Shared.Salvage;
using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Salvage;

/// <summary>
/// Put on an expedition map. Tells the players there how long the expedition lasts,
/// what its objective is and how much it pays.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ExpeditionHudComponent : Component
{
    /// <summary>
    /// When the expedition ends. Shifted while the map is paused, like the expedition's own end time.
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    [ViewVariables, AutoNetworkedField]
    public SalvageMissionType MissionType;

    /// <summary>
    /// How many structures or megafauna the mission started with.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int ObjectiveTotal;

    [ViewVariables, AutoNetworkedField]
    public int ObjectiveRemaining;

    /// <summary>
    /// Name of the structure or megafauna to destroy, null if the mission has none.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public string? ObjectiveName;

    [ViewVariables, AutoNetworkedField]
    public bool Completed;

    /// <summary>
    /// Which way the dungeon is from the landing site, the same one announced on arrival. Null if there is no dungeon.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Direction? DungeonDirection;

    /// <summary>
    /// Credits paid for finishing the expedition with its objective done.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int Reward;
}
