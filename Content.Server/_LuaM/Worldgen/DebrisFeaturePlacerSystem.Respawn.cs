using System.Linq;
using System.Numerics;
using Content.Server._NF.Worldgen.Components.Debris;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Components.Debris;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Debris;

public sealed partial class DebrisFeaturePlacerSystem
{
    /// <summary>
    /// Deletes the debris of a loaded chunk and places new debris at new points, as if the chunk was generated again.
    /// Debris on a grid from <paramref name="keep"/> stays, new debris doesn't spawn on top of it.
    /// </summary>
    public void RespawnDebris(Entity<DebrisFeaturePlacerControllerComponent, WorldChunkComponent> chunk, HashSet<EntityUid> keep)
    {
        var uid = chunk.Owner;
        var placer = chunk.Comp1;
        var worldChunk = chunk.Comp2;

        // Deleting debris drops its point from the list (OnDebrisShutdown), so go over a copy.
        foreach (var debris in placer.OwnedDebris.Values.ToList())
        {
            if (debris is { } ent && !keep.Contains(ent) && !TerminatingOrDeleted(ent))
                Del(ent);
        }

        foreach (var (point, debris) in placer.OwnedDebris.ToList())
        {
            if (debris is not { } ent || TerminatingOrDeleted(ent))
                placer.OwnedDebris.Remove(point);
        }

        if (!TryComp<MapComponent>(worldChunk.Map, out var map))
            return;

        var densityChannel = placer.DensityNoiseChannel;
        var density = _noiseIndex.Evaluate(uid, densityChannel, worldChunk.Coordinates + new Vector2(0.5f, 0.5f));
        if (density == 0)
            return;

        // The same placement as OnChunkLoaded, only always at new points.
        var points = GeneratePointsInChunk(uid, density, worldChunk.Coordinates, worldChunk.Map);
        var safetyBounds = Box2.UnitCentered.Enlarged(placer.SafetyZoneRadius);
        foreach (var point in points)
        {
            if (placer.OwnedDebris.ContainsKey(point))
                continue;

            var pointDensity = _noiseIndex.Evaluate(uid, densityChannel, WorldGen.WorldToChunkCoords(point));
            if (pointDensity == 0 && placer.DensityClip || _random.Prob(placer.RandomCancellationChance))
                continue;

            if (HasCollisions(map.MapId, safetyBounds.Translated(point)))
                continue;

            var coords = new EntityCoordinates(worldChunk.Map, point);

            var preEv = new PrePlaceDebrisFeatureEvent(coords, uid);
            RaiseLocalEvent(uid, ref preEv);
            if (preEv.Handled)
                continue;

            var debrisFeatureEv = new TryGetPlaceableDebrisFeatureEvent(coords, uid);
            RaiseLocalEvent(uid, ref debrisFeatureEv);
            if (debrisFeatureEv.DebrisProto == null)
                continue;

            var ent = Spawn(debrisFeatureEv.DebrisProto, coords);
            placer.OwnedDebris.Add(point, ent);

            var owned = EnsureComp<OwnedDebrisComponent>(ent);
            owned.OwningController = uid;
            owned.LastKey = point;

            EnsureComp<SpaceDebrisComponent>(ent);
        }
    }
}
