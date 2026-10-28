using System.Numerics;
using Content.Client.Shuttles.BUI;
using Content.Shared._LuaM.HoloComms;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._LuaM.HoloComms;

[UsedImplicitly]
public sealed class HoloCommsBoundUserInterface : BoundUserInterface
{
    /// <summary>
    /// Where the window opens over the shuttle console window when it has no saved position yet: on top of its left panel.
    /// </summary>
    private static readonly Vector2 ConsoleOffset = new(16f, 72f);

    [ViewVariables]
    private HoloCommsWindow? _window;

    public HoloCommsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var hasPosition = EntMan.System<UserInterfaceSystem>().TryGetPosition(Owner, UiKey, out _);

        _window = this.CreateWindow<HoloCommsWindow>();
        _window.OnCall += target => SendMessage(new HoloCommsCallMessage(target));
        _window.OnAnswer += () => SendMessage(new HoloCommsAnswerMessage());
        _window.OnHangUp += () => SendMessage(new HoloCommsHangUpMessage());
        _window.OnDoNotDisturb += enabled => SendMessage(new ShipCommsSetDoNotDisturbMessage(enabled));
        _window.AnchorWindow = GetConsoleWindow;

        if (!hasPosition && GetConsoleWindow() is { } console && _window.Parent is { } root)
        {
            // Kept on the screen when the console window stands at its edge
            var max = Vector2.Max(Vector2.Zero, root.Size - _window.SetSize);
            LayoutContainer.SetPosition(_window, Vector2.Clamp(console.Position + ConsoleOffset, Vector2.Zero, max));
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is HoloCommsBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is HoloCommsLineMessage line)
            _window?.AddLine(line);
    }

    private BaseWindow? GetConsoleWindow()
    {
        return EntMan.System<UserInterfaceSystem>().TryGetOpenUi<ShuttleConsoleBoundUserInterface>(Owner, ShuttleConsoleUiKey.Key, out var console)
            ? console.Window
            : null;
    }
}
