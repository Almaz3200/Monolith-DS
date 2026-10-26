using Content.Shared.Holopad;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;

namespace Content.Client._LuaM.HoloComms;

/// <summary>
/// A hologram shows the SSD indicator of the one it shows, on the holopads and in the comms window of the consoles.
/// </summary>
public sealed class HologramStatusIconSystem : EntitySystem
{
    private readonly List<StatusIconData> _linkedIcons = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolopadHologramComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<HolopadHologramComponent> ent, ref GetStatusIconsEvent args)
    {
        if (ent.Comp.LinkedEntity is not { } linked ||
            !TryComp<SSDIndicatorComponent>(linked, out var ssd) ||
            !ssd.IsSSD)
        {
            return;
        }

        // The body decides when its indicator shows, the hologram only repeats it
        _linkedIcons.Clear();
        var linkedEv = new GetStatusIconsEvent(_linkedIcons);
        RaiseLocalEvent(linked, ref linkedEv);

        foreach (var icon in _linkedIcons)
        {
            if (icon is SsdIconPrototype)
                args.StatusIcons.Add(icon);
        }
    }
}
