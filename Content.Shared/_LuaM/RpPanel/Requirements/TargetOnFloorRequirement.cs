using Content.Shared.Standing;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.RpPanel.Requirements;

[Serializable, NetSerializable]
public sealed partial class TargetOnFloorRequirement : InvertableRpRequirement
{
    public override bool IsAvailable(EntityUid uid, IEntityManager entityManager)
    {
        var isOnFloor = entityManager.System<StandingStateSystem>().IsDown(uid);

        return Inverted ? !isOnFloor : isOnFloor;
    }
}
