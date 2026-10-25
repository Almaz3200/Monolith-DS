using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.RpPanel;

[Serializable, NetSerializable]
public enum RpPanelKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RpPanelBuiState(NetEntity user, NetEntity target) : BoundUserInterfaceState
{
    public NetEntity User { get; } = user;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class RpPanelSendMessage(string interaction, bool privateMode) : BoundUserInterfaceMessage
{
    public readonly string Interaction = interaction;
    public readonly bool PrivateMode = privateMode;
}

[Serializable, NetSerializable]
public sealed class RpPanelCustomEmoteMessage(RpCustomEmote emote, bool privateMode) : BoundUserInterfaceMessage
{
    public readonly RpCustomEmote Emote = emote;
    public readonly bool PrivateMode = privateMode;
}

[ByRefEvent]
public readonly record struct RpPanelOpenEvent(EntityUid Target);
