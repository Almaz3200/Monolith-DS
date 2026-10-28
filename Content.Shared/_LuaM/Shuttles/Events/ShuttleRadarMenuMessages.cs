using Content.Shared._LuaM.HoloComms;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class ShuttleRadarMenuRequestMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Grid;

    public ShuttleRadarMenuRequestMessage(NetEntity grid)
    {
        Grid = grid;
    }
}

[Serializable, NetSerializable]
public sealed class ShuttleRadarMenuMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Grid;
    public readonly bool CanFollow;
    public readonly List<HoloCommsContact> Calls;

    public ShuttleRadarMenuMessage(NetEntity grid, bool canFollow, List<HoloCommsContact> calls)
    {
        Grid = grid;
        CanFollow = canFollow;
        Calls = calls;
    }
}

[Serializable, NetSerializable]
public sealed class ShuttleRadarFollowMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Grid;

    public ShuttleRadarFollowMessage(NetEntity grid)
    {
        Grid = grid;
    }
}

[Serializable, NetSerializable]
public sealed class ShuttleRadarCallMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Target;

    public ShuttleRadarCallMessage(NetEntity target)
    {
        Target = target;
    }
}
