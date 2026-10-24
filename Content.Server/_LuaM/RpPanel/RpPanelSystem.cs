using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Shared._LuaM.CCVar;
using Content.Shared._LuaM.RpPanel;
using Content.Shared._LuaM.RpPanel.Requirements;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Chat;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
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

public sealed partial class RpPanelSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private INetConfigurationManager _netCfg = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

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
        // --  движок молча режет сообщения связанного/оглушённого игрока; проверяем сами в CanAct, чтобы показать попап
        var interfaceData = new InterfaceData(
            clientType: "Content.Client._LuaM.RpPanel.RpPanelBoundUserInterface",
            requireInputValidation: false
        );

        _ui.SetUi(entity.Owner, RpPanelKey.Key, interfaceData);
    }

    private void OnOpenPanel(Entity<RpPanelOwnerComponent> entity, ref RpPanelOpenEvent args)
    {
        var user = entity.Owner;
        if (!IsValidUi(user, args.Target))
            return;

        if (!_interaction.IsAccessible(user, args.Target))
        {
            Popup(user, "rp-panel-out-of-reach");
            return;
        }

        if (!CanAct(user, args.Target))
            return;

        if (!_ui.TryOpenUi(user, RpPanelKey.Key, user))
            return;

        entity.Comp.Target = args.Target;
        _ui.SetUiState(user, RpPanelKey.Key, new RpPanelBuiState(GetNetEntity(user), GetNetEntity(args.Target)));
    }

    private void OnBoundUIClosed(Entity<RpPanelOwnerComponent> entity, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not RpPanelKey.Key)
            return;

        entity.Comp.Target = null;
    }

    private void OnSendMessage(Entity<RpPanelOwnerComponent> entity, ref RpPanelSendMessage args)
    {
        if (!TryGetTarget(entity, args.Actor, out var target))
            return;

        ProcessInteraction(args.Actor, target.Value, args.Interaction, args.PrivateMode);
    }

    private void OnCustomEmote(Entity<RpPanelOwnerComponent> entity, ref RpPanelCustomEmoteMessage args)
    {
        if (!TryGetTarget(entity, args.Actor, out var target))
            return;

        ProcessCustomInteraction(args.Actor, target.Value, args.Emote, args.PrivateMode);
    }

    private bool TryGetTarget(Entity<RpPanelOwnerComponent> entity, EntityUid actor, [NotNullWhen(true)] out EntityUid? target)
    {
        target = entity.Comp.Target;
        if (target == null || actor != entity.Owner)
            return false;

        if (!TerminatingOrDeleted(target.Value))
            return true;

        entity.Comp.Target = null;
        target = null;
        return false;
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

        if (!CanRunInteraction(user, target, interaction.Range, out var userPanel))
            return;

        var message = Loc.GetString(_random.Pick(messages.Values),
            ("target", Identity.Name(target, EntityManager, user)));

        RunInteraction(user, target, userPanel, interaction.Sound, message, privateMode);
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

        if (!CanRunInteraction(user, target, RpCustomEmote.Range, out var userPanel))
            return;

        message = message.Replace(RpCustomEmote.TargetPlaceholder, Identity.Name(target, EntityManager, user));

        RunInteraction(user, target, userPanel, sound, message, privateMode);
    }

    private void RunInteraction(
        EntityUid user,
        EntityUid target,
        RpPanelOwnerComponent userPanel,
        SoundSpecifier? sound,
        string message,
        bool privateMode)
    {
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
            _chat.TrySendInGameICMessage(source, message, InGameICChatType.Emote, false, checkRadioPrefix: false);
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

    private bool IsValidUi(EntityUid user, EntityUid target)
    {
        if (!HasComp<RpPanelOwnerComponent>(user) || !HasComp<RpPanelOwnerComponent>(target))
            return false;

        if (!IsPanelEnabled(user) || !IsPanelEnabled(target))
            return false;

        return !_mobState.IsIncapacitated(user) && !_mobState.IsIncapacitated(target);
    }

    private bool CanRunInteraction(
        EntityUid user,
        EntityUid target,
        float range,
        [NotNullWhen(true)] out RpPanelOwnerComponent? userPanel)
    {
        if (!TryComp(user, out userPanel))
            return false;

        if (userPanel.LastInteraction + RpPanelOwnerComponent.Cooldown > _timing.CurTime)
            return false;

        if (!IsValidUi(user, target))
        {
            Popup(user, "rp-panel-target-unavailable");
            return false;
        }

        if (!CanAct(user, target))
            return false;

        if (!_interaction.InRangeAndAccessible(user, target, range))
        {
            Popup(user, "rp-panel-out-of-reach");
            return false;
        }

        return true;
    }

    private bool CanAct(EntityUid user, EntityUid target)
    {
        if (!_actionBlocker.CanInteract(user, null) || !_actionBlocker.CanEmote(user))
        {
            Popup(user, $"rp-panel-blocked-self-{GetBlockReason(user)}");
            return false;
        }

        if (user == target)
            return true;

        if (!_actionBlocker.CanInteract(target, null))
        {
            Popup(user, $"rp-panel-blocked-target-{GetBlockReason(target)}");
            return false;
        }

        if (!_actionBlocker.CanInteract(user, target))
        {
            Popup(user, "rp-panel-blocked-target-busy");
            return false;
        }

        return true;
    }

    private string GetBlockReason(EntityUid uid)
    {
        if (TryComp<CuffableComponent>(uid, out var cuffable) && !cuffable.CanStillInteract)
            return "cuffed";

        if (HasComp<SleepingComponent>(uid))
            return "asleep";

        if (HasComp<StunnedComponent>(uid))
            return "stunned";

        return "busy";
    }

    private void Popup(EntityUid user, string locId)
    {
        _popup.PopupEntity(Loc.GetString(locId), user, user, PopupType.SmallCaution);
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
