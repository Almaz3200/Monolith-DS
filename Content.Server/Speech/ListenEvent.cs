using Content.Shared._EinsteinEngines.Language; // LuaM

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;

    /// <summary>
    /// LuaM - the language the message was spoken in, so devices that pass speech on keep it.
    /// </summary>
    public readonly LanguagePrototype? Language;

    public ListenEvent(string message, EntityUid source, LanguagePrototype? language = null) // LuaM - language
    {
        Message = message;
        Source = source;
        Language = language;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
