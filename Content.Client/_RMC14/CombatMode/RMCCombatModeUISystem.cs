using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared._RMC14.CombatMode;
using Robust.Client.Graphics;
using Robust.Client.UserInterface; // LuaM
using Robust.Client.UserInterface.CustomControls; // LuaM
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.CombatMode;

public sealed partial class RMCCombatModeUISystem : EntitySystem
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private CombatModeSystem _combatMode = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private RMCCombatModeSystem _rmcCombatMode = default!;
    [Dependency] private IUserInterfaceManager _ui = default!; // LuaM

    private ICursor? _crosshairCursor;
    private bool _cursorHidden; // LuaM

    public override void FrameUpdate(float frameTime)
    {
        // LuaM-start
        // -- курсор прячется только над игровым миром: SetCursor(null) каждый кадр сбрасывал курсоры интерфейса (прицел радара, IBeam, ресайз)
        if (_combatMode.IsInCombatMode() &&
            _hands.GetActiveHandEntity() is { } held &&
            _rmcCombatMode.GetCrosshair(held) != null &&
            _ui.CurrentlyHovered is IViewportControl)
        {
            _crosshairCursor ??= _clyde.CreateCursor(new Image<Rgba32>(1, 1), Vector2i.Zero);
            _clyde.SetCursor(_crosshairCursor);
            _cursorHidden = true;
        }
        else if (_cursorHidden)
        {
            _cursorHidden = false;
            _ui.WorldCursor = null;
        }
        // LuaM-end
    }
}
