using System.Runtime.InteropServices;
using Content.Server.Chat.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._LuaM.Telephone;

/// <summary>
/// A line relayed through a call can be repeated by several telephones of the same side, like the points of a ship line.
/// Only one of the telephones near each other says it, one far off says it again for the people around it.
/// Where the hearing of two of them overlaps, everybody gets the line in the chat once, the other copy only shows its speech bubble.
/// </summary>
public sealed class TelephoneRelayChatSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _xform = default!;

    // Where the lines relayed this tick were said
    private readonly Dictionary<(EntityUid Origin, string Message), List<MapCoordinates>> _said = new();

    // The lines relayed this tick and who already got them in the chat
    private readonly Dictionary<(EntityUid Origin, string Message), RelayListeners> _lines = new();
    private readonly Stack<RelayListeners> _pool = new();

    private RelayListeners? _pending;
    private EntityUid _pendingSpeaker;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _said.Clear();

        if (_lines.Count == 0)
            return;

        foreach (var listeners in _lines.Values)
        {
            listeners.Clear();
            _pool.Push(listeners);
        }

        _lines.Clear();
    }

    /// <summary>
    /// Whether the speaker says a line of the origin: not when another telephone said it this tick close enough to be heard here.
    /// </summary>
    public bool TrySay(EntityUid speaker, EntityUid origin, string message)
    {
        var position = _xform.GetMapCoordinates(speaker);

        if (!_said.TryGetValue((origin, message), out var said))
        {
            said = new List<MapCoordinates>();
            _said.Add((origin, message), said);
        }

        foreach (var other in said)
        {
            if (other.MapId == position.MapId && (other.Position - position.Position).Length() <= ChatSystem.VoiceRange)
                return false;
        }

        said.Add(position);
        return true;
    }

    /// <summary>
    /// Wraps the chat message of the speaker that repeats a line of the origin.
    /// </summary>
    public RelayScope Relay(EntityUid speaker, EntityUid origin, string message)
    {
        if (!_lines.TryGetValue((origin, message), out var listeners))
        {
            listeners = _pool.TryPop(out var pooled) ? pooled : new RelayListeners();
            _lines.Add((origin, message), listeners);
        }

        _pending = listeners;
        _pendingSpeaker = speaker;
        return new RelayScope(this);
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        // The chat asks for its recipients first, whoever asks after it (like the voice) gets them as they are
        if (_pending is not { } listeners || ev.Source != _pendingSpeaker)
            return;

        _pending = null;

        foreach (var session in ev.Recipients.Keys)
        {
            ref var data = ref CollectionsMarshal.GetValueRefOrNullRef(ev.Recipients, session);

            // Out of range observers are counted on their own, only some of them see such lines in the chat
            var heard = data.Range >= 0
                ? !listeners.InRange.Add(session)
                : listeners.InRange.Contains(session) || !listeners.Far.Add(session);

            if (heard)
                data = data with { HideChatOverride = true };
        }
    }

    public readonly struct RelayScope : IDisposable
    {
        private readonly TelephoneRelayChatSystem _system;

        public RelayScope(TelephoneRelayChatSystem system)
        {
            _system = system;
        }

        public void Dispose()
        {
            _system._pending = null;
        }
    }

    private sealed class RelayListeners
    {
        public readonly HashSet<ICommonSession> InRange = new();
        public readonly HashSet<ICommonSession> Far = new();

        public void Clear()
        {
            InRange.Clear();
            Far.Clear();
        }
    }
}
