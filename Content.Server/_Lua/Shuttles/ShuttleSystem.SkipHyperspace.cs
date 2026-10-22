using Content.Server.Emp;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared._NF.Emp.Components;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

/// <summary>
/// Drives that jump straight to the target, without going through hyperspace.
/// </summary>
public sealed partial class ShuttleSystem
{
    /// <summary>
    /// Moves the shuttle straight to its target if its drive skips hyperspace,
    /// with an EMP flash where it left and where it arrived.
    /// </summary>
    private bool TrySkipHyperspace(Entity<FTLComponent, ShuttleComponent> entity, TransformComponent xform)
    {
        var uid = entity.Owner;
        if (!TryGetFTLDrive(uid, out _, out var drive) || !drive.SkipHyperspace)
            return false;

        var comp = entity.Comp1;
        var fromMapUid = xform.MapUid;
        var fromMatrix = _transform.GetWorldMatrix(xform);
        var fromRotation = _transform.GetWorldRotation(xform);

        SpawnEmpVisualOnly(_transform.GetMapCoordinates(uid, xform), drive.SkipHyperspaceEmpRange);

        var ev = new FTLStartedEvent(uid, comp.TargetCoordinates, fromMapUid, fromMatrix, fromRotation);
        RaiseLocalEvent(uid, ref ev, true);

        UpdateFTLArriving(entity);
        LeaveNoFTLBehind((uid, xform), fromMatrix, fromMapUid);

        SpawnEmpVisualOnly(_transform.GetMapCoordinates(uid, xform), drive.SkipHyperspaceEmpRange);
        return true;
    }

    /// <summary>
    /// EMP pulse effect that does not affect anything.
    /// </summary>
    private void SpawnEmpVisualOnly(MapCoordinates coordinates, float range)
    {
        var blast = Spawn(EmpSystem.EmpPulseEffectPrototype, coordinates);
        var blastComp = EnsureComp<EmpBlastComponent>(blast);
        blastComp.VisualRange = range;
        Dirty(blast, blastComp);
    }
}
