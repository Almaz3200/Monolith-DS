using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._LuaM.HoloComms;

[Serializable, NetSerializable]
public enum HoloCommsUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum HoloCommsPhase : byte
{
    Unpowered,
    Idle,
    Calling,
    Ringing,
    InCall,
    Ending,
}

[Serializable, NetSerializable]
public enum HoloCommsContactKind : byte
{
    Ship,
    Holopad,
}

/// <summary>
/// Somebody the console can call. A ship is listed once, <see cref="Target"/> is one of its comms points.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct HoloCommsContact(NetEntity Target, string Name, HoloCommsContactKind Kind, bool Busy);

[Serializable, NetSerializable]
public sealed class HoloCommsBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly HoloCommsPhase Phase;

    /// <summary>
    /// The ship or holopad on the other end of the line.
    /// </summary>
    public readonly string? RemoteName;

    /// <summary>
    /// Who is calling, from their ID card, while the line rings.
    /// </summary>
    public readonly string? CallerName;

    /// <summary>
    /// The hologram the remote side is shown through, it exists while the call is up.
    /// </summary>
    public readonly NetEntity? Hologram;

    public readonly bool DoNotDisturb;

    /// <summary>
    /// Only filled while there is no call, the list is useless otherwise.
    /// </summary>
    public readonly List<HoloCommsContact> Contacts;

    public HoloCommsBoundUserInterfaceState(
        HoloCommsPhase phase,
        string? remoteName,
        string? callerName,
        NetEntity? hologram,
        bool doNotDisturb,
        List<HoloCommsContact> contacts)
    {
        Phase = phase;
        RemoteName = remoteName;
        CallerName = callerName;
        Hologram = hologram;
        DoNotDisturb = doNotDisturb;
        Contacts = contacts;
    }

    // The state is compared on every refresh, an equal one is not sent again.
    public override bool Equals(object? obj)
    {
        return obj is HoloCommsBoundUserInterfaceState other &&
               Phase == other.Phase &&
               RemoteName == other.RemoteName &&
               CallerName == other.CallerName &&
               Hologram == other.Hologram &&
               DoNotDisturb == other.DoNotDisturb &&
               Contacts.SequenceEqual(other.Contacts);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Phase, RemoteName, CallerName, Hologram, DoNotDisturb, Contacts.Count);
    }
}

/// <summary>
/// Sent from the shuttle console window: opens or closes the comms window.
/// </summary>
[Serializable, NetSerializable]
public sealed class HoloCommsToggleWindowMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class HoloCommsCallMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity Target;

    public HoloCommsCallMessage(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class HoloCommsAnswerMessage : BoundUserInterfaceMessage;

/// <summary>
/// Ends or declines the call for the whole ship.
/// </summary>
[Serializable, NetSerializable]
public sealed class HoloCommsHangUpMessage : BoundUserInterfaceMessage;

/// <summary>
/// Sent from the comms window and from the window of a ship holopad.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShipCommsSetDoNotDisturbMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public ShipCommsSetDoNotDisturbMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

/// <summary>
/// A line said or emoted by the one shown on the hologram, for the comms window.
/// </summary>
[Serializable, NetSerializable]
public sealed class HoloCommsLineMessage : BoundUserInterfaceMessage
{
    public readonly string Speaker;
    public readonly string Text;
    public readonly bool Emote;

    public HoloCommsLineMessage(string speaker, string text, bool emote)
    {
        Speaker = speaker;
        Text = text;
        Emote = emote;
    }
}
