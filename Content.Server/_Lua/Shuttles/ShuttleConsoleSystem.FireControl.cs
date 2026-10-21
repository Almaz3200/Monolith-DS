using Content.Server._Mono.FireControl;
using Content.Shared._Mono.FireControl;
using Content.Shared.Shuttles.BUIStates;
using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Events;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Gunnery inside the shuttle console: weapon list in the state, firing and refreshing.
/// </summary>
public sealed partial class ShuttleConsoleSystem
{
    [Dependency] private FireControlSystem _fireControl = default!;

    private void InitializeFireControl()
    {
        Subs.BuiEvents<ShuttleConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<ShuttleConsoleFireMessage>(OnShuttleConsoleFire);
            subs.Event<ShuttleConsoleRefreshFireControlMessage>(OnShuttleConsoleRefreshFireControl);
        });
    }

    /// <summary>
    /// Re-sends the UI state of a single shuttle console.
    /// </summary>
    public void UpdateConsoleUi(EntityUid console)
    {
        DockingInterfaceState? dockState = null;
        UpdateState(console, ref dockState);
    }

    private ShuttleBoundUserInterfaceState WithFireControl(EntityUid console, ShuttleBoundUserInterfaceState state)
    {
        var weapons = _fireControl.GetShuttleConsoleWeapons(console);
        state.FireControlConnected = weapons != null;
        state.FireControllables = weapons;
        return state;
    }

    private void OnShuttleConsoleFire(EntityUid uid, ShuttleConsoleComponent component, ShuttleConsoleFireMessage args)
    {
        _fireControl.FireFromShuttleConsole(uid, args.Actor, args.Selected, args.Coordinates);
    }

    private void OnShuttleConsoleRefreshFireControl(EntityUid uid, ShuttleConsoleComponent component, ShuttleConsoleRefreshFireControlMessage args)
    {
        _fireControl.RefreshShuttleConsoleWeapons(uid);
        UpdateConsoleUi(uid);
    }
}
