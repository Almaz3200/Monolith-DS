using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Construction;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using System.Xml.Schema;
using Content.Server.Station.Components;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CargoTelepadComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<CargoTelepadComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<CargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<CargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
    }
    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(uid, out var appearance);

            if (comp.CurrentState == CargoTelepadState.Unpowered)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                comp.Accumulator = comp.Delay;
                continue;
            }

            if (!TryComp<DeviceLinkSinkComponent>(uid, out var sinkComponent) ||
                !sinkComponent.LinkedSources.Any(HasComp<CargoOrderConsoleComponent>))
            {
                comp.Accumulator = comp.Delay;
                continue;
            }

            comp.Accumulator -= frameTime;

            // Uhh listen teleporting takes time and I just want the 1 float.
            if (comp.Accumulator > 0f)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                continue;
            }

            var consoles = sinkComponent.LinkedSources.Where(HasComp<CargoOrderConsoleComponent>).ToList();
            var consoleUidList = consoles.Select(console => GetNetEntity(console)).ToList();
            var xform = Transform(uid);

            foreach (var console in consoles)
            {
                if (_station.GetOwningStation(console) is not { } station ||
                    !TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase) ||
                    !FulfillNextOrder(consoleUidList, orderDatabase, xform.Coordinates, comp.PrinterOutput))
                    continue;

                _audio.PlayPvs(_audio.ResolveSound(comp.TeleportSound), uid, AudioParams.Default.WithVolume(-8f));
                UpdateOrders(station); // Frontier

                comp.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting, appearance);
                break;
            }

            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(EntityUid uid, CargoTelepadComponent telepad, ComponentInit args)
    {
        _linker.EnsureSinkPorts(uid, telepad.ReceiverPort);
    }

    private void OnRefreshParts(EntityUid uid, CargoTelepadComponent component, RefreshPartsEvent args)
    {
        var rating = args.PartRatings[component.MachinePartTeleportDelay] - 1;
        component.Delay = component.BaseDelay * MathF.Pow(component.PartRatingTeleportDelay, rating);
    }

    private void OnUpgradeExamine(EntityUid uid, CargoTelepadComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("cargo-telepad-delay-upgrade", component.Delay / component.BaseDelay);
    }

    private void SetEnabled(EntityUid uid, CargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(uid, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Setting idle state should be handled by Update();
        if (disabled)
            return;

        TryComp<AppearanceComponent>(uid, out var appearance);
        component.CurrentState = CargoTelepadState.Unpowered;
        _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Unpowered, appearance);
    }

    private void OnTelepadPowerChange(EntityUid uid, CargoTelepadComponent component, ref PowerChangedEvent args)
    {
        SetEnabled(uid, component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, CargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(uid, component);
    }
}
