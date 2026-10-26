using Content.Shared.Chat;

namespace Content.Server._LuaM.Chat;

/// <summary>
/// Raised after an entity emoted into the chat, the typed emotes included.
/// </summary>
[ByRefEvent]
public readonly record struct EntityEmoteSentEvent(EntityUid Source, string Action, ChatTransmitRange Range);
