using System.Runtime.InteropServices;
using Content.Server.Chat.Systems;
using Robust.Shared.Player;

namespace Content.Server._LuaM.Telephone;

/// <summary>
/// A line relayed through a call can be repeated by several telephones of the same side, like the points of a ship line.
/// Everybody near them gets the line in the chat once, the other copies only show their speech bubble.
/// </summary>
public sealed class TelephoneRelayChatSystem : EntitySystem
{
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
