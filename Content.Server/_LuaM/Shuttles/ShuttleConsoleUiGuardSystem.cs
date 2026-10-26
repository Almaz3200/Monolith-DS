using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Components;

namespace Content.Server._LuaM.Shuttles;

public sealed partial class ShuttleConsoleUiGuardSystem : EntitySystem
{
    [Dependency] private ShuttleConsoleLockSystem _lock = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleConsoleComponent, BoundUserInterfaceMessageAttempt>(OnMessageAttempt);
    }

    private void OnMessageAttempt(EntityUid uid, ShuttleConsoleComponent component, BoundUserInterfaceMessageAttempt args)
    {
        if (!ShuttleConsoleUiKey.Key.Equals(args.UiKey))
            return;

        // -- окно консоли открывается только через TryPilot, тот, кто открывает его иначе, ещё не пилот
        if (args.Message is OpenBoundInterfaceMessage)
        {
            if (!TryComp<PilotComponent>(args.Actor, out var pilot) || pilot.Console != uid)
                args.Cancel();

            return;
        }

        if (IsLocked(uid))
            args.Cancel();
    }

    public bool IsLocked(EntityUid uid)
    {
        return TryComp<ShuttleConsoleLockComponent>(uid, out var lockComp) && _lock.GetEffectiveLockState(uid, lockComp);
    }
}
