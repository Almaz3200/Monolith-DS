using System.Numerics;
using Content.Shared._LuaM.HoloComms;
using Content.Shared._LuaM.Shuttles.Events;

namespace Content.Client.Shuttles.UI;

public sealed partial class NavScreen
{
    public event Action<NetEntity>? OnRadarMenuRequest;
    public event Action<NetEntity>? OnRadarFollow;
    public event Action<NetEntity>? OnRadarCall;

    private NetEntity? _radarMenuGrid;
    private Vector2 _radarMenuPosition;
    private RadarContextMenu? _radarMenu;

    private void InitializeRadarMenu()
    {
        NavRadar.OnRadarMenuRequested += OnRadarMenuRequested;
    }

    private void OnRadarMenuRequested(EntityUid grid)
    {
        CloseRadarMenu();

        _radarMenuGrid = _entManager.GetNetEntity(grid);
        _radarMenuPosition = UserInterfaceManager.MousePositionScaled.Position;
        OnRadarMenuRequest?.Invoke(_radarMenuGrid.Value);
    }

    public void ShowRadarMenu(ShuttleRadarMenuMessage msg)
    {
        if (msg.Grid != _radarMenuGrid ||
            _radarMenu != null ||
            !_entManager.TryGetEntity(msg.Grid, out var grid) ||
            !NavRadar.IsRadarMenuTarget(grid.Value))
        {
            return;
        }

        var menu = new RadarContextMenu();
        menu.AddHeader(_entManager.GetComponentOrNull<MetaDataComponent>(grid.Value)?.EntityName ?? string.Empty);
        menu.AddItem(Loc.GetString("shuttle-radar-menu-follow"), msg.CanFollow ? () => OnRadarFollow?.Invoke(msg.Grid) : null);

        if (_holoCommsAvailable)
            AddRadarCallItem(menu, msg.Calls);

        _radarMenu = menu;
        menu.OpenAt(_radarMenuPosition);
    }

    private void AddRadarCallItem(RadarContextMenu menu, List<HoloCommsContact> calls)
    {
        if (!calls.Exists(call => !call.Busy))
        {
            menu.AddItem(Loc.GetString("shuttle-radar-menu-unreachable"), null);
            return;
        }

        if (calls.Count == 1 && calls[0].Kind == HoloCommsContactKind.Ship)
        {
            var target = calls[0].Target;
            menu.AddItem(Loc.GetString("shuttle-radar-menu-call"), () => OnRadarCall?.Invoke(target));
            return;
        }

        menu.AddSubMenu(Loc.GetString("shuttle-radar-menu-call"), subMenu =>
        {
            foreach (var call in calls)
            {
                var target = call.Target;
                subMenu.AddItem(call.Name, call.Busy ? null : () => OnRadarCall?.Invoke(target),
                    call.Busy ? Loc.GetString("shuttle-radar-menu-unreachable") : null);
            }
        });
    }

    private void UpdateRadarMenu()
    {
        if (_radarMenu is not { Visible: true } || !_entManager.TryGetEntity(_radarMenuGrid, out var grid))
            return;

        if (!NavRadar.IsRadarMenuTarget(grid.Value))
            CloseRadarMenu();
    }

    private void CloseRadarMenu()
    {
        _radarMenuGrid = null;

        if (_radarMenu == null)
            return;

        _radarMenu.Close();
        _radarMenu.Orphan();
        _radarMenu = null;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        CloseRadarMenu();
    }
}
