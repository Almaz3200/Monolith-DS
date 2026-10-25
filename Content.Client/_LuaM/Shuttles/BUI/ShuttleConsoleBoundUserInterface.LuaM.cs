using Content.Client.Shuttles.UI;
using Content.Shared._LuaM.HoloComms;
using Content.Shared._LuaM.Shuttles.Events;

namespace Content.Client.Shuttles.BUI;

public sealed partial class ShuttleConsoleBoundUserInterface
{
    /// <summary>
    /// The comms window keeps itself above this one.
    /// </summary>
    public ShuttleConsoleWindow? Window => _window;

    private void LuaMOpen()
    {
        if (_window == null)
            return;

        _window.OnClearTarget += () => SendMessage(new ShuttleConsoleClearTargetMessage());
        _window.OnHoloCommsPressed += () => SendMessage(new HoloCommsToggleWindowMessage());
        _window.OnRadarMenuRequest += grid => SendMessage(new ShuttleRadarMenuRequestMessage(grid));
        _window.OnRadarFollow += grid => SendMessage(new ShuttleRadarFollowMessage(grid));
        _window.OnRadarCall += target => SendMessage(new ShuttleRadarCallMessage(target));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is ShuttleRadarMenuMessage menu)
            _window?.ShowRadarMenu(menu);
    }
}
