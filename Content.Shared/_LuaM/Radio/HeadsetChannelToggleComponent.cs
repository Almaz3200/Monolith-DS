using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.Radio;

/// <summary>
/// Lets the user switch off separate channels of a headset through a UI.
/// Disabled channels are neither heard nor transmitted on.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeadsetChannelToggleComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<string> DisabledChannels = new();
}

[Serializable, NetSerializable]
public enum HeadsetChannelsUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class HeadsetChannelsBoundUserInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// Channels provided by the inserted encryption keys.
    /// </summary>
    public readonly List<string> Channels;

    public readonly HashSet<string> DisabledChannels;

    /// <summary>
    /// Frequency of the built-in tunable radio, null if the headset has none.
    /// </summary>
    public readonly int? Frequency;

    public readonly int MinFrequency;
    public readonly int MaxFrequency;

    public HeadsetChannelsBoundUserInterfaceState(
        List<string> channels,
        HashSet<string> disabledChannels,
        int? frequency = null,
        int minFrequency = 0,
        int maxFrequency = 0)
    {
        Channels = channels;
        DisabledChannels = disabledChannels;
        Frequency = frequency;
        MinFrequency = minFrequency;
        MaxFrequency = maxFrequency;
    }
}

[Serializable, NetSerializable]
public sealed class HeadsetSetFrequencyMessage : BoundUserInterfaceMessage
{
    public readonly int Frequency;

    public HeadsetSetFrequencyMessage(int frequency)
    {
        Frequency = frequency;
    }
}

[Serializable, NetSerializable]
public sealed class HeadsetChannelToggleMessage : BoundUserInterfaceMessage
{
    public readonly string Channel;
    public readonly bool Enabled;

    public HeadsetChannelToggleMessage(string channel, bool enabled)
    {
        Channel = channel;
        Enabled = enabled;
    }
}
