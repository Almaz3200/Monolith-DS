using Content.Shared._LuaM.Shuttles.Events;

namespace Content.Client.Shuttles.BUI;

public sealed partial class ShuttleConsoleBoundUserInterface
{
    private void LuaMOpen()
    {
        if (_window == null)
            return;

        _window.OnClearTarget += () => SendMessage(new ShuttleConsoleClearTargetMessage());
    }
}
