using Content.Server.Chat.Managers;
using Content.Server.Worldgen;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Components.Debris;
using Content.Server.Worldgen.Systems;
using Content.Server.Worldgen.Systems.Debris;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._LuaM.Worldgen;

/// <summary>
/// Handles <see cref="DebrisRespawnComponent"/>. Chunks only generate their debris when they get loaded,
/// so a chunk that always has someone nearby never gets new asteroids. This generates them again every so often
/// and warns the players nearby beforehand.
/// </summary>
public sealed partial class DebrisRespawnSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DebrisFeaturePlacerSystem _placer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// Chunks respawned in one tick at most, generating big asteroids is heavy. The rest wait for the next ticks.
    /// </summary>
    private const int MaxChunksPerTick = 1;

    /// <summary>
    /// Players this many chunks away from a respawning chunk are warned too, they can see its asteroids.
    /// </summary>
    private const int WarningChunkRange = 1;

    /// <summary>
    /// Neighbouring chunks respawn at about the same time, a player gets one warning for all of them.
    /// </summary>
    private static readonly TimeSpan WarningCooldown = TimeSpan.FromSeconds(90);

    private readonly List<Entity<DebrisFeaturePlacerControllerComponent, WorldChunkComponent>> _due = new();
    private readonly List<WorldChunkComponent> _warn = new();
    private readonly HashSet<EntityUid> _occupiedGrids = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastWarning = new();

    private EntityQuery<GhostComponent> _ghostQuery;

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();

        SubscribeLocalEvent<DebrisRespawnComponent, WorldChunkLoadedEvent>(OnChunkLoaded);
    }

    private void OnChunkLoaded(Entity<DebrisRespawnComponent> ent, ref WorldChunkLoadedEvent args)
    {
        // The chunk has just generated its debris.
        ent.Comp.NextRespawn = _timing.CurTime + ent.Comp.Interval;
        ent.Comp.Warned = false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        _due.Clear();
        _warn.Clear();

        var query = EntityQueryEnumerator<DebrisRespawnComponent, LoadedChunkComponent, DebrisFeaturePlacerControllerComponent, WorldChunkComponent>();
        while (query.MoveNext(out var uid, out var respawn, out _, out var placer, out var chunk))
        {
            if (!respawn.Warned && now >= respawn.NextRespawn - respawn.WarningTime)
            {
                respawn.Warned = true;
                _warn.Add(chunk);
            }

            if (respawn.NextRespawn > now || _due.Count >= MaxChunksPerTick)
                continue;

            respawn.NextRespawn = now + respawn.Interval;
            respawn.Warned = false;
            _due.Add((uid, placer, chunk));
        }

        if (_warn.Count > 0)
            WarnPlayers(now);

        if (_due.Count == 0)
            return;

        CollectOccupiedGrids();
        foreach (var chunk in _due)
        {
            _placer.RespawnDebris(chunk, _occupiedGrids);
        }
    }

    /// <summary>
    /// Tells the players in and around the chunks from <see cref="_warn"/> that their debris is about to be replaced.
    /// </summary>
    private void WarnPlayers(TimeSpan now)
    {
        var message = Loc.GetString("debris-respawn-warning");

        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var actor, out var xform))
        {
            // Ghosts lose nothing.
            if (_ghostQuery.HasComp(uid))
                continue;

            if (xform.MapUid is not { } map || !IsNearWarnedChunk(map, _transform.GetWorldPosition(xform)))
                continue;

            var session = actor.PlayerSession;
            if (_lastWarning.TryGetValue(session.UserId, out var last) && now - last < WarningCooldown)
                continue;

            _lastWarning[session.UserId] = now;
            _chat.ChatMessageToOne(ChatChannel.Server, message, message, EntityUid.Invalid, false, session.Channel, Color.Orange);
        }
    }

    private bool IsNearWarnedChunk(EntityUid map, System.Numerics.Vector2 worldPosition)
    {
        var playerChunk = WorldGen.WorldToChunkCoords(worldPosition).Floored();
        foreach (var chunk in _warn)
        {
            if (chunk.Map != map)
                continue;

            var offset = playerChunk - chunk.Coordinates;
            if (Math.Abs(offset.X) <= WarningChunkRange && Math.Abs(offset.Y) <= WarningChunkRange)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Grids someone's mind is on: players, including disconnected ones and the bodies they ghosted from.
    /// Their asteroid is kept so nobody is thrown into space. Mobs without a mind (fauna, NPCs) and ghosts don't count.
    /// </summary>
    private void CollectOccupiedGrids()
    {
        _occupiedGrids.Clear();

        var query = EntityQueryEnumerator<MindContainerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var mind, out var xform))
        {
            if (!mind.HasMind || _ghostQuery.HasComp(uid))
                continue;

            if (xform.GridUid is { } grid)
                _occupiedGrids.Add(grid);
        }
    }
}
