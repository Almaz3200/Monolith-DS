using Content.Client._Lua.Tick;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Lua.UserInterface.Controls;

public sealed class HudPerfLabel : RichTextLabel
{
    private readonly IGameTiming _gameTiming;
    private readonly ClientServerPerfSystem _serverPerf;

    // LuaM-start: setting the text parses its markup and relayouts the label, so it isn't done every frame
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.25);
    private TimeSpan _nextUpdate;
    private string? _lastText;
    // LuaM-end

    public HudPerfLabel(IGameTiming gameTiming, ClientServerPerfSystem serverPerf)
    {
        _gameTiming = gameTiming;
        _serverPerf = serverPerf;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (!VisibleInTree)
            return;
        // LuaM-start
        var now = _gameTiming.RealTime;
        if (now < _nextUpdate)
            return;
        _nextUpdate = now + UpdateInterval;
        // LuaM-end
        var clientFps = _gameTiming.FramesPerSecondAvg;
        var serverFps = _serverPerf.ServerFpsAvg;
        var tps = _serverPerf.ServerTickRate;
        // var version = Loc.GetString("connecting-version"); LuaM Deleted
        string statusText;
        string statusColorHex;
        if (serverFps < 50)
        {
            statusText = Loc.GetString("server-status-high");
            statusColorHex = "#FF0000";
        }
        else if (serverFps < 150)
        {
            statusText = Loc.GetString("server-status-medium");
            statusColorHex = "#FFFF00";
        }
        else
        {
            statusText = Loc.GetString("server-status-stable");
            statusColorHex = "#00FF00";
        }
        var text = $"FPS: {clientFps:N0} | SrvFPS: [color={statusColorHex}]{serverFps:N0}[/color] | TPS: {tps} | [color={statusColorHex}]{statusText}[/color]"; //LuaM Deleted {version} // LuaM: Text > var text
        // LuaM-start
        if (text == _lastText)
            return;
        _lastText = text;
        Text = text;
        // LuaM-end
    }
}


