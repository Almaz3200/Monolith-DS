using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared._LuaM.Shuttles;

/// <summary>
/// Radar-side checks for <see cref="RadarStealthFieldComponent"/>.
/// </summary>
public sealed class RadarStealthFieldSystem : EntitySystem
{
    [Dependency] private FriendlyIffSystem _friendlyIff = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    /// <summary>
    /// Whether the grid must not be shown on the local player's radar looking from the given entity.
    /// Nothing is hidden if there is no viewer.
    /// </summary>
    public bool IsHiddenFromLocalPlayer(EntityUid grid, EntityUid? viewer)
    {
        return viewer != null && IsHiddenFromLocalPlayer(grid, _transform.GetMapCoordinates(viewer.Value));
    }

    /// <summary>
    /// Whether the grid must not be shown on the local player's radar looking from the given position.
    /// </summary>
    public bool IsHiddenFromLocalPlayer(EntityUid grid, MapCoordinates viewer)
    {
        var query = EntityQueryEnumerator<RadarStealthFieldComponent, TransformComponent>();
        while (query.MoveNext(out var fieldUid, out var field, out var fieldXform))
        {
            if (fieldXform.MapID != viewer.MapId || _friendlyIff.IsFriendlyToLocalPlayer(fieldUid))
                continue;

            var fieldCenter = GetCenter(fieldUid, fieldXform);

            if (fieldUid == grid)
            {
                if (Vector2.Distance(fieldCenter, viewer.Position) > field.RevealRange)
                    return true;

                continue;
            }

            if (Vector2.Distance(fieldCenter, GetCenter(grid, Transform(grid))) <= field.FieldRadius)
                return true;
        }

        return false;
    }

    private Vector2 GetCenter(EntityUid uid, TransformComponent xform)
    {
        var localCenter = TryComp<PhysicsComponent>(uid, out var physics) ? physics.LocalCenter : Vector2.Zero;
        return Vector2.Transform(localCenter, _transform.GetWorldMatrix(xform));
    }
}
