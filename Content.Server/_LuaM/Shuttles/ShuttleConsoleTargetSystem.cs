using System.Numerics;
using Content.Server._Mono.NPC.HTN;
using Content.Server.NPC.HTN;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared._LuaM.Shuttles.Events;
using Content.Shared.Popups;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server._LuaM.Shuttles;

/// <summary>
/// Handles removing the console marker; an autopilot flying to that marker stops with it.
/// </summary>
public sealed partial class ShuttleConsoleTargetSystem : EntitySystem
{
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private RadarConsoleSystem _radarConsole = default!;
    [Dependency] private ShipSteeringSystem _steering = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ShuttleConsoleSystem _console = default!;

    /// <summary>
    /// How close the autopilot destination must be to the marker to count as flying to it.
    /// </summary>
    private const float MarkerMatchRange = 1f;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ShuttleConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<ShuttleConsoleClearTargetMessage>(OnClearTarget);
        });
    }

    private void OnClearTarget(EntityUid uid, ShuttleConsoleComponent component, ShuttleConsoleClearTargetMessage args)
    {
        if (!TryComp<RadarConsoleComponent>(uid, out var radarConsole)
            || Transform(uid).GridUid is not { } grid)
            return;

        var stopAutopilot = IsAutopilotFlyingToMarker((uid, component), radarConsole);

        _radarConsole.ClearTarget((uid, radarConsole));

        if (stopAutopilot)
        {
            StopAutopilot((uid, component));
            _popup.PopupEntity(Loc.GetString("shuttle-console-autopilot-popup-marker-removed"), uid, PopupType.Medium);
        }

        _console.RefreshShuttleConsoles(grid);
    }

    private bool IsAutopilotFlyingToMarker(Entity<ShuttleConsoleComponent> console, RadarConsoleComponent radarConsole)
    {
        if (radarConsole.Target is not { } marker
            || !TryComp<HTNComponent>(console, out var htn)
            || !htn.Blackboard.TryGetValue<EntityCoordinates>(console.Comp.AutopilotTargetKey, out var destination, EntityManager))
            return false;

        var destinationMap = _transform.ToMapCoordinates(destination);
        return destinationMap.MapId == Transform(console).MapID
               && Vector2.Distance(destinationMap.Position, marker) <= MarkerMatchRange;
    }

    /// <summary>
    /// Cancels the autopilot plan and its steering without reporting an arrival.
    /// </summary>
    private void StopAutopilot(Entity<ShuttleConsoleComponent> console)
    {
        console.Comp.AutopilotStopping = true;

        if (TryComp<HTNComponent>(console, out var htn))
        {
            htn.PlanningToken?.Cancel();
            htn.PlanningToken = null;

            htn.Blackboard.Remove<EntityCoordinates>(console.Comp.AutopilotTargetKey);
            htn.Blackboard.Remove<Angle>(console.Comp.AutopilotRotationKey);

            if (htn.Plan != null)
            {
                _htn.ShutdownTask(htn.Plan.CurrentOperator, htn.Blackboard, HTNOperatorStatus.Failed);
                _htn.ShutdownPlan(htn);
            }
        }

        _steering.Stop(console.Owner);
        console.Comp.AutopilotStopping = false;
    }
}
