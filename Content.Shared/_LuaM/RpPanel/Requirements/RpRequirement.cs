using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.RpPanel.Requirements;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class RpRequirement
{
    public abstract bool IsAvailable(EntityUid uid, IEntityManager entityManager);
}

[Serializable, NetSerializable]
public abstract partial class InvertableRpRequirement : RpRequirement
{
    [DataField]
    public bool Inverted;
}
