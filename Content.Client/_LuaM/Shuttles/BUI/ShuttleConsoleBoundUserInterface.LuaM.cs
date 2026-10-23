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
    }
}
