using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.RoundEnd;

/// <summary>
/// Sent to clients whenever the round end countdown starts or stops.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoundEndCountdownEvent(TimeSpan? roundEndTime) : EntityEventArgs
{
    /// <summary>
    /// When the round ends, or null if the countdown isn't running.
    /// </summary>
    public TimeSpan? RoundEndTime = roundEndTime;
}
