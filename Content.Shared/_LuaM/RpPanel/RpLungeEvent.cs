using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.RpPanel;

[Serializable, NetSerializable]
public sealed class RpLungeEvent(NetEntity user, Vector2 localPos) : EntityEventArgs
{
    public readonly NetEntity User = user;

    public readonly Vector2 LocalPos = localPos;
}
