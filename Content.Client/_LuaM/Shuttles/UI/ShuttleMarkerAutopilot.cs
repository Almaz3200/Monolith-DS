using System.Numerics;
using Robust.Shared.Map;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Autopilot destination for the console marker.
/// </summary>
public static class ShuttleMarkerAutopilot
{
    /// <summary>
    /// The marker lives on the shuttle's map; the shuttle keeps its current heading when it arrives.
    /// </summary>
    public static bool TryGetDestination(IEntityManager entMan, EntityUid? shuttle, Vector2 target, out MapCoordinates destination, out Angle angle)
    {
        destination = MapCoordinates.Nullspace;
        angle = Angle.Zero;

        if (!entMan.TryGetComponent(shuttle, out TransformComponent? xform) || xform.MapID == MapId.Nullspace)
            return false;

        destination = new MapCoordinates(target, xform.MapID);
        angle = entMan.System<SharedTransformSystem>().GetWorldRotation(xform);
        return true;
    }
}
