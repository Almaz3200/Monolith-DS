using Content.Shared._LuaM.RoundEnd;

namespace Content.Client._LuaM.RoundEnd;

/// <summary>
/// Remembers when the round ends, see <see cref="RoundEndCountdownEvent"/>.
/// </summary>
public sealed class RoundEndCountdownSystem : EntitySystem
{
    /// <summary>
    /// When the round ends, or null if the round end countdown isn't running.
    /// </summary>
    public TimeSpan? RoundEndTime { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundEndCountdownEvent>(ev => RoundEndTime = ev.RoundEndTime);
    }
}
