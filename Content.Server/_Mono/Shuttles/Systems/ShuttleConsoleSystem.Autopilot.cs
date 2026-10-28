using Content.Server._Mono.NPC.HTN.Operators;
using Content.Server.NPC.HTN;
using Content.Shared.Popups;
using Content.Server.Shuttles.Components;
using Content.Shared._Mono.Shuttles;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server._Mono.NPC.HTN; // LuaM
using Content.Shared.Shuttles.Components; // LuaM
using Robust.Shared.Map; // LuaM

namespace Content.Server._Mono.Shuttles;

public sealed partial class ShuttleConsoleAutopilotSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        // LuaM-start
        Subs.BuiEvents<ShuttleConsoleComponent>(ShuttleConsoleUiKey.Key, subs =>
        {
            subs.Event<ShuttleConsoleAutopilotPositionMessage>(OnAutopilotMessage);
        });
        // LuaM-end
        SubscribeLocalEvent<ShuttleConsoleComponent, SteeringDoneEvent>(OnSteeringDone);
    }

    private void OnAutopilotMessage(Entity<ShuttleConsoleComponent> ent, ref ShuttleConsoleAutopilotPositionMessage args)
    {
        StartAutopilot(ent, args.Coordinates, args.Angle); // LuaM
    }

    // LuaM-start
    public void StartAutopilot(Entity<ShuttleConsoleComponent> ent, MapCoordinates coordinates, Angle angle)
    {
        if (!TryComp<HTNComponent>(ent, out var htn))
            return;

        var blackboard = htn.Blackboard;
        blackboard.SetValue(ent.Comp.AutopilotTargetKey, _transform.ToCoordinates(coordinates));
        blackboard.SetValue(ent.Comp.AutopilotRotationKey, angle + MathF.PI);

        var steerer = EnsureComp<ShipSteererComponent>(ent.Owner);
        steerer.MaxVelocity = ent.Comp.AutopilotMaxSpeed;
    }
    // LuaM-end

    private void OnSteeringDone(Entity<ShuttleConsoleComponent> ent, ref SteeringDoneEvent args)
    {
        if (ent.Comp.AutopilotStopping) // LuaM: stopped, not arrived
            return;

        _audio.PlayPvs(ent.Comp.AutopilotDoneSound, ent);
        _popup.PopupEntity(Loc.GetString("shuttle-console-autopilot-popup-done"), ent, PopupType.Medium);
    }
}
