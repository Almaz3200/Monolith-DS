using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._LuaM.Radio;

/// <summary>
/// Built-in, non-removable encryption key of a headset: always gives <see cref="Channel"/>
/// (the handheld radio channel by default) and lets the user tune its frequency.
/// Talking on it only happens through the channel prefix, there is no always-on microphone.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeadsetTunableRadioComponent : Component
{
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Handheld";

    /// <summary>
    /// Current frequency. 0 means "not set yet", it is picked on map init (see <see cref="RandomFrequency"/>).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Frequency;

    /// <summary>
    /// If true, an unset frequency becomes a random one between the min and max frequency,
    /// otherwise the channel's default frequency is used.
    /// </summary>
    [DataField]
    public bool RandomFrequency = true;

    [DataField]
    public int MinFrequency = 1000;

    [DataField]
    public int MaxFrequency = 3000;
}
