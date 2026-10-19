using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._LuaM.CCVar;
using Content.Shared._LuaM.RpPanel;
using Content.Shared._LuaM.RpPanel.Requirements;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._LuaM.RpPanel;

public sealed class RpPanelSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetConfigurationManager _netCfg = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private static readonly SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/_LuaM/Interface/RpPanel/friendly.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RpPanelOwnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RpPanelOwnerComponent, RpPanelOpenEvent>(OnOpenPanel);
        SubscribeLocalEvent<RpPanelOwnerComponent, BoundUIClosedEvent>(OnBoundUIClosed);
        SubscribeLocalEvent<RpPanelOwnerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);

        Subs.BuiEvents<RpPanelOwnerComponent>(RpPanelKey.Key, subs =>
        {
            subs.Event<RpPanelSendMessage>(OnSendMessage);
            subs.Event<RpPanelCustomEmoteMessage>(OnCustomEmote);
        });
    }

    private void OnMapInit(Entity<RpPanelOwnerComponent> entity, ref MapInitEvent args)
    {
        var interfaceData = new InterfaceData(
            clientType: "Content.Client._LuaM.RpPanel.RpPanelBoundUserInterface"
        );

        _ui.SetUi(entity.Owner, RpPanelKey.Key, interfaceData);
    }

    private void OnOpenPanel(Entity<RpPanelOwnerComponent> entity, ref RpPanelOpenEvent args)
    {
        if (!_interaction.IsAccessible(entity.Owner, args.Target))
            return;

        if (!TryOpenPanel(entity.Owner, args.Target))
            return;

        entity.Comp.Target = args.Target;
    }

    private void OnBoundUIClosed(Entity<RpPanelOwnerComponent> entity, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not RpPanelKey.Key)
            return;

        entity.Comp.Target = null;
    }

    private void OnSendMessage(Entity<RpPanelOwnerComponent> entity, ref RpPanelSendMessage args)
    {
        var target = entity.Comp.Target;
        if (target == null || args.Actor != entity.Owner)
            return;

        ProcessInteraction(args.Actor, target.Value, args.Interaction, args.PrivateMode);
    }

    private void OnCustomEmote(Entity<RpPanelOwnerComponent> entity, ref RpPanelCustomEmoteMessage args)
    {
        var target = entity.Comp.Target;
        if (target == null || args.Actor != entity.Owner)
            return;

        ProcessCustomInteraction(args.Actor, target.Value, args.Emote, args.PrivateMode);
    }

    private void OnGetVerbs(Entity<RpPanelOwnerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!IsValidUi(args.User, ent.Owner))
            return;

        var user = args.User;

        args.Verbs.Add(new Verb
        {
            Act = () =>
            {
                var ev = new RpPanelOpenEvent(ent.Owner);
                RaiseLocalEvent(user, ref ev);
            },
            Text = Loc.GetString("rp-panel-open-verb"),
            Icon = VerbIcon,
            Priority = 2,
        });
    }

    public void ProcessInteraction(EntityUid user, EntityUid target, string interactionId, bool privateMode = false)
    {
        if (!_prototype.TryIndex<RpInteractionPrototype>(interactionId, out var interaction))
            return;

        if (!CheckRequirements(user, interaction.UserRequirements) ||
            !CheckRequirements(target, interaction.TargetRequirements))
            return;

        var messagesId = user == target ? interaction.SelfMessages : interaction.Messages;
        if (!_prototype.TryIndex(messagesId, out var messages))
            return;

        var message = Loc.GetString(_random.Pick(messages.Values),
            ("target", Identity.Name(target, EntityManager, user)));

        RunInteraction(user, target, interaction.Kind, interaction.Range, interaction.Sound, message, privateMode);
    }

    public void ProcessCustomInteraction(EntityUid user, EntityUid target, RpCustomEmote emote, bool privateMode)
    {
        if (emote.Self != (user == target) || !Enum.IsDefined(emote.Kind) || !_prototype.HasIndex(emote.Category))
            return;

        var message = RpCustomEmote.Sanitize(emote.Message, RpCustomEmote.MaxMessageLength);
        if (message.Length == 0)
            return;

        SoundSpecifier? sound = null;
        if (emote.Sound is { } soundId)
        {
            if (!_prototype.TryIndex(soundId, out var soundProto))
                return;

            sound = soundProto.Sound;
        }

        message = message.Replace(RpCustomEmote.TargetPlaceholder, Identity.Name(target, EntityManager, user));

        RunInteraction(user, target, emote.Kind, RpCustomEmote.Range, sound, message, privateMode);
    }

    private void RunInteraction(
        EntityUid user,
        EntityUid target,
        RpInteractionKind kind,
        float range,
        SoundSpecifier? sound,
        string message,
        bool privateMode)
    {
        if (!TryComp<RpPanelOwnerComponent>(user, out var userPanel))
            return;

        if (!IsValidInteraction(user, target, kind, range, userPanel))
            return;

        Filter? privateFilter = privateMode
            ? Filter.Entities(user, target)
            : null;

        userPanel.LastInteraction = _timing.CurTime;

        SendEmote(user, message, privateFilter);
        PlaySound(user, sound, privateFilter);
        ProcessLunge(user, target, privateFilter);
    }

    private void SendEmote(EntityUid source, string message, Filter? privateFilter)
    {
        if (privateFilter == null)
        {
            _chat.TrySendInGameICMessage(source, message, InGameICChatType.Emote, false);
            return;
        }

        var ent = Identity.Entity(source, EntityManager);
        var wrapped = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", FormattedMessage.EscapeText(Name(ent))),
            ("entity", ent),
            ("message", FormattedMessage.RemoveMarkupOrThrow(message)));

        _chatManager.ChatMessageToManyFiltered(privateFilter, ChatChannel.Emotes, message, wrapped, source, false, false, null);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Private RP emote from {ToPrettyString(source):user}: {message}");
    }

    private void PlaySound(EntityUid user, SoundSpecifier? sound, Filter? privateFilter)
    {
        if (privateFilter != null)
            _audio.PlayEntity(sound, privateFilter, user, false);
        else
            _audio.PlayPvs(sound, user);
    }

    private void ProcessLunge(EntityUid user, EntityUid target, Filter? privateFilter)
    {
        if (user == target)
            return;

        var userXform = Transform(user);

        var invMatrix = _transform.GetInvWorldMatrix(userXform);
        var localPos = Vector2.Transform(_transform.GetMapCoordinates(target).Position, invMatrix);

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        RaiseNetworkEvent(
            new RpLungeEvent(GetNetEntity(user), localPos),
            privateFilter ?? Filter.Pvs(user, entityManager: EntityManager));
    }

    private bool TryOpenPanel(EntityUid user, EntityUid target)
    {
        if (!IsValidUi(user, target))
            return false;

        _ui.TryOpenUi(user, RpPanelKey.Key, user);

        var state = new RpPanelBuiState(GetNetEntity(user), GetNetEntity(target));
        _ui.SetUiState(user, RpPanelKey.Key, state);
        return true;
    }

    private bool IsValidUi(EntityUid user, EntityUid target)
    {
        if (!HasComp<RpPanelOwnerComponent>(user) || !HasComp<RpPanelOwnerComponent>(target))
            return false;

        if (!IsPanelEnabled(user) || !IsPanelEnabled(target))
            return false;

        return !_mobState.IsIncapacitated(user) && !_mobState.IsIncapacitated(target);
    }

    private bool IsValidInteraction(
        EntityUid user,
        EntityUid target,
        RpInteractionKind kind,
        float range,
        RpPanelOwnerComponent userPanel)
    {
        if (userPanel.LastInteraction + RpPanelOwnerComponent.Cooldown > _timing.CurTime)
            return false;

        if (!_interaction.InRangeAndAccessible(user, target, range))
            return false;

        if (!IsValidUi(user, target))
            return false;

        return kind == RpInteractionKind.Soft || CanActFreely(user, target);
    }

    private bool CanActFreely(EntityUid user, EntityUid target)
    {
        return _actionBlocker.CanInteract(user, target)
               && (user == target || _actionBlocker.CanInteract(target, null));
    }

    private bool IsPanelEnabled(EntityUid uid)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return false;

        return _netCfg.GetClientCVar(actor.PlayerSession.Channel, LuaMCVars.RpPanelEnabled);
    }

    private bool CheckRequirements(EntityUid uid, List<RpRequirement>? requirements)
    {
        if (requirements == null)
            return true;

        foreach (var requirement in requirements)
        {
            if (!requirement.IsAvailable(uid, EntityManager))
                return false;
        }

        return true;
    }
}
