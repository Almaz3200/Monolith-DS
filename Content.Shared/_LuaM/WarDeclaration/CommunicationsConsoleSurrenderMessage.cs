using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.WarDeclaration;

/// <summary>
/// Sent by the communications console UI to capitulate in the war for the console's faction.
/// </summary>
[Serializable, NetSerializable]
public sealed class CommunicationsConsoleSurrenderMessage : BoundUserInterfaceMessage;
