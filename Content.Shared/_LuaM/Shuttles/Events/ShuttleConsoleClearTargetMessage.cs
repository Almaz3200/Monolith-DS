using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.Shuttles.Events;

/// <summary>
/// Removes the console marker shown on the radar and on the sector map.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleConsoleClearTargetMessage : BoundUserInterfaceMessage;
