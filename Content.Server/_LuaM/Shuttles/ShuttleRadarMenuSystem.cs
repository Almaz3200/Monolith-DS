using System.Numerics;
using Content.Server._LuaM.HoloComms;
using Content.Server._Mono.Shuttles;
using Content.Server.NPC.HTN;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared._LuaM.HoloComms;
using Content.Shared._LuaM.Shuttles.Events;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._LuaM.Shuttles;

public sealed partial class ShuttleRadarMenuSystem : EntitySystem
{
    [Dependency] private RadarIdentificationSystem _identification = default!;
    [Dependency] private RadarConsoleSystem _radarConsole = default!;
    [Dependency] private ShuttleConsoleSystem _console = default!;
    [Dependency] private ShuttleConsoleAutopilotSystem _autopilot = default!;
    [Dependency] private DockingSystem _docking = default!;
    [Dependency] private HoloCommsConsoleSystem _holoComms = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private IGameTiming _timing = default!;

    private const float HullDistance = 50f;

    // -- автопилот (AutopilotMoveCompound) встаёт за столько метров до своей точки
    private const float ArrivalRange = 40f;

    private const float MarkerStep = 25f;
    private static readonly TimeSpan FollowInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<EntityUid, FollowState> _follows = new();
    private readonly List<EntityUid> _stoppedFollows = new();
    private TimeSpan _nextFollowUpdate;

    private sealed class FollowState(EntityUid grid, EntityUid actor, EntityCoordinates destination, Vector2 marker)
    {
        public readonly EntityUid Grid = grid;
        public readonly EntityUid Actor = actor;
        public EntityCoordinates Destination = destination;
        public Vector2 Marker = marker;
    }

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<ShuttleConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<ShuttleRadarMenuRequestMessage>(OnMenuRequest);
            subs.Event<ShuttleRadarFollowMessage>(OnFollow);
        });
    }

    private void OnMenuRequest(EntityUid uid, ShuttleConsoleComponent component, ShuttleRadarMenuRequestMessage args)
    {
        if (!TryGetEntity(args.Grid, out var grid) || !_identification.CanTarget(uid, grid.Value, args.Actor))
            return;

        var calls = new List<HoloCommsContact>();
        if (TryComp<HoloCommsConsoleComponent>(uid, out var comms))
            _holoComms.GetRadarCalls((uid, comms), grid.Value, args.Actor, calls);

        var msg = new ShuttleRadarMenuMessage(args.Grid, CanFollow(uid, grid.Value), calls);
        _ui.ServerSendUiMessage(uid, ShuttleConsoleUiKey.Key, msg, args.Actor);
    }

    private void OnFollow(EntityUid uid, ShuttleConsoleComponent component, ShuttleRadarFollowMessage args)
    {
        if (!TryGetEntity(args.Grid, out var grid) ||
            !_identification.CanTarget(uid, grid.Value, args.Actor) ||
            !CanFollow(uid, grid.Value) ||
            Transform(uid).GridUid is not { } ourGrid ||
            !TryComp<RadarConsoleComponent>(uid, out var radar))
        {
            return;
        }

        var ourXform = Transform(ourGrid);
        var center = _identification.GetCenter(grid.Value);
        var destination = GetStopPoint(grid.Value, center, ourGrid);

        _radarConsole.SetTargetAt((uid, radar), grid.Value, center);
        _autopilot.StartAutopilot((uid, component), new MapCoordinates(destination, ourXform.MapID), _transform.GetWorldRotation(ourXform));
        _console.RefreshShuttleConsoles(ourGrid);

        if (TryComp<HTNComponent>(uid, out var htn) &&
            htn.Blackboard.TryGetValue<EntityCoordinates>(component.AutopilotTargetKey, out var set, EntityManager))
        {
            _follows[uid] = new FollowState(grid.Value, args.Actor, set, center);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_follows.Count == 0 || _timing.CurTime < _nextFollowUpdate)
            return;

        _nextFollowUpdate = _timing.CurTime + FollowInterval;

        _stoppedFollows.Clear();
        foreach (var (console, follow) in _follows)
        {
            if (!UpdateFollow(console, follow))
                _stoppedFollows.Add(console);
        }

        foreach (var console in _stoppedFollows)
        {
            _follows.Remove(console);
        }
    }

    // -- автопилот перечитывает точку каждый кадр, её двигаем за целью, пока он ведёт именно к ней
    private bool UpdateFollow(EntityUid console, FollowState follow)
    {
        if (!TryComp<ShuttleConsoleComponent>(console, out var component) ||
            !TryComp<HTNComponent>(console, out var htn) ||
            !htn.Blackboard.TryGetValue<EntityCoordinates>(component.AutopilotTargetKey, out var current, EntityManager) ||
            current != follow.Destination ||
            TerminatingOrDeleted(follow.Grid) ||
            !_identification.CanTarget(console, follow.Grid, follow.Actor) ||
            Transform(console).GridUid is not { } ourGrid)
        {
            return false;
        }

        var center = _identification.GetCenter(follow.Grid);
        var destination = new MapCoordinates(GetStopPoint(follow.Grid, center, ourGrid), Transform(ourGrid).MapID);

        follow.Destination = _transform.ToCoordinates(destination);
        htn.Blackboard.SetValue(component.AutopilotTargetKey, follow.Destination);

        if (Vector2.Distance(center, follow.Marker) > MarkerStep && TryComp<RadarConsoleComponent>(console, out var radar))
        {
            follow.Marker = center;
            _radarConsole.SetTargetAt((console, radar), follow.Grid, center);
            _console.RefreshShuttleConsoles(ourGrid);
        }

        return true;
    }

    private bool CanFollow(EntityUid console, EntityUid grid)
    {
        return Transform(console).GridUid is { } ourGrid && !_docking.AreGridsDocked(ourGrid, grid);
    }

    private Vector2 GetStopPoint(EntityUid target, Vector2 center, EntityUid ourGrid)
    {
        var ourPos = _transform.GetWorldPosition(ourGrid);
        var toUs = ourPos - center;
        if (toUs.LengthSquared() < 0.01f)
            return ourPos;

        var direction = toUs.Normalized();
        var distance = GetExtent(target, center, direction) + HullDistance + GetExtent(ourGrid, ourPos, -direction) - ArrivalRange;
        return center + direction * MathF.Max(distance, 0f);
    }

    private float GetExtent(EntityUid grid, Vector2 from, Vector2 direction)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return 0f;

        var (gridPos, gridRot) = _transform.GetWorldPositionRotation(grid);
        var localFrom = (-gridRot).RotateVec(from - gridPos);
        var localDir = (-gridRot).RotateVec(direction);
        var box = gridComp.LocalAABB;

        var extent = float.MaxValue;
        if (MathF.Abs(localDir.X) > 0.0001f)
            extent = MathF.Min(extent, ((localDir.X > 0f ? box.Right : box.Left) - localFrom.X) / localDir.X);
        if (MathF.Abs(localDir.Y) > 0.0001f)
            extent = MathF.Min(extent, ((localDir.Y > 0f ? box.Top : box.Bottom) - localFrom.Y) / localDir.Y);

        return extent == float.MaxValue ? 0f : MathF.Max(extent, 0f);
    }
}
