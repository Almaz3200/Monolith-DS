using System.Linq;
using Content.Shared._Mono.FireControl;
using Content.Shared._Mono.ShipGuns;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Content.Client.Stylesheets;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Weapons panel of the shuttle console, so the pilot can also act as the gunner.
/// </summary>
public sealed partial class NavScreen
{
    public event Action? OnFireControlRefresh;
    public event Action<List<NetEntity>, NetCoordinates>? OnGunneryFire;

    private readonly Dictionary<NetEntity, Button> _weaponButtons = new();
    private readonly Dictionary<NetEntity, ShipGunType> _weaponTypes = new();
    private FireControllableEntry[]? _weapons;
    private bool _gunneryConnected;

    private void InitializeGunnery()
    {
        GunneryRefreshButton.OnPressed += _ => OnFireControlRefresh?.Invoke();
        GunnerySelectAllButton.OnPressed += _ => SelectWeapons(_ => true);
        GunneryUnselectAllButton.OnPressed += _ => SelectWeapons(_ => false);
        GunneryBallisticButton.OnPressed += _ => SelectWeapons(type => type == ShipGunType.Ballistic);
        GunneryEnergyButton.OnPressed += _ => SelectWeapons(type => type == ShipGunType.Energy);
        GunneryMissileButton.OnPressed += _ => SelectWeapons(type => type == ShipGunType.Missile);

        NavRadar.OnRadarClick += OnGunneryRadarClick;
        NavRadar.OnGunneryCursorMove += OnGunneryCursorMove;
    }

    /// <summary>
    /// Shows the weapons of the shuttle's fire control server, or hides the panel when there are none.
    /// </summary>
    public void UpdateWeapons(bool connected, FireControllableEntry[]? weapons)
    {
        _gunneryConnected = connected;
        _weapons = connected ? weapons : null;

        GunneryHeader.Visible = connected;
        GunneryControls.Visible = connected;
        UpdateWeaponsPanelVisibility();
        NavRadar.DefaultCursorShape = connected ? CursorShape.Crosshair : CursorShape.Arrow;
        NavRadar.SetGunneryControllables(_weapons);

        GunneryStatus.Text = Loc.GetString(connected ? "gunnery-window-connected" : "gunnery-window-disconnected");
        GunneryStatus.FontColorOverride = connected ? Color.Green : Color.Red;

        UpdateWeaponButtons();
        UpdateGunnerySelection();
    }

    /// <summary>
    /// The left panel holds the weapons and the shield, it shows up when there is either.
    /// </summary>
    private void UpdateWeaponsPanelVisibility()
    {
        WeaponsPanel.Visible = _gunneryConnected || ShieldPanel.Visible;
    }

    private void OnGunneryRadarClick(EntityCoordinates coordinates)
    {
        if (!_gunneryConnected || !NavRadar.GunneryMouseDown)
            return;

        var selected = GetSelectedWeapons();
        if (selected.Count == 0)
            return;

        OnGunneryFire?.Invoke(selected, _entManager.GetNetCoordinates(coordinates));
    }

    /// <summary>
    /// Sends the cursor with no weapons selected: nothing fires, guided missiles just follow it.
    /// </summary>
    private void OnGunneryCursorMove(EntityCoordinates coordinates)
    {
        if (!_gunneryConnected)
            return;

        OnGunneryFire?.Invoke(new List<NetEntity>(), _entManager.GetNetCoordinates(coordinates));
    }

    private List<NetEntity> GetSelectedWeapons()
    {
        return _weaponButtons.Where(kv => kv.Value.Pressed).Select(kv => kv.Key).ToList();
    }

    private void SelectWeapons(Func<ShipGunType?, bool> predicate)
    {
        foreach (var (weapon, button) in _weaponButtons)
        {
            ShipGunType? type = _weaponTypes.TryGetValue(weapon, out var found) ? found : null;
            button.Pressed = predicate(type);
        }

        UpdateGunnerySelection();
    }

    private void UpdateGunnerySelection()
    {
        NavRadar.SetGunnerySelection(GetSelectedWeapons().ToHashSet());
        UpdateWeaponButtonTexts();
    }

    private void UpdateWeaponButtons()
    {
        var weapons = _weapons ?? Array.Empty<FireControllableEntry>();
        var toRemove = new HashSet<NetEntity>(_weaponButtons.Keys);
        _weaponTypes.Clear();

        foreach (var weapon in weapons)
        {
            toRemove.Remove(weapon.NetEntity);

            var entity = _entManager.GetEntity(weapon.NetEntity);
            if (_entManager.TryGetComponent<ShipGunTypeComponent>(entity, out var typeComp))
                _weaponTypes[weapon.NetEntity] = typeComp.Type;

            if (_weaponButtons.ContainsKey(weapon.NetEntity))
                continue;

            var button = new Button
            {
                ToggleMode = true,
                Text = weapon.Name,
                StyleClasses = { ConsoleStyles.Button }, // LuaM: ButtonSquare > console button
                HorizontalExpand = true,
                Margin = new Thickness(4, 2),
            };
            button.OnToggled += _ => UpdateGunnerySelection();

            GunneryWeaponsBox.AddChild(button);
            _weaponButtons.Add(weapon.NetEntity, button);
        }

        foreach (var weapon in toRemove)
        {
            if (_weaponButtons.Remove(weapon, out var button))
                GunneryWeaponsBox.RemoveChild(button);
        }

        var hasWeapons = _weaponButtons.Count > 0;
        GunnerySelectAllButton.Disabled = !hasWeapons;
        GunneryUnselectAllButton.Disabled = !hasWeapons;
        GunneryBallisticButton.Disabled = !_weaponTypes.ContainsValue(ShipGunType.Ballistic);
        GunneryEnergyButton.Disabled = !_weaponTypes.ContainsValue(ShipGunType.Energy);
        GunneryMissileButton.Disabled = !_weaponTypes.ContainsValue(ShipGunType.Missile);
    }

    private void UpdateWeaponButtonTexts()
    {
        if (_weapons == null)
            return;

        foreach (var weapon in _weapons)
        {
            if (!_weaponButtons.TryGetValue(weapon.NetEntity, out var button))
                continue;

            if (button.Pressed && weapon.HasManualReload && weapon.AmmoCount.HasValue)
            {
                button.Text = Loc.GetString("gunnery-gun-select-ammo", ("name", weapon.Name), ("ammo", weapon.AmmoCount.Value));
                button.ModulateSelfOverride = weapon.AmmoCount.Value == 0 ? Color.Red : null;
            }
            else
            {
                button.Text = Loc.GetString("gunnery-gun-select", ("name", weapon.Name));
                button.ModulateSelfOverride = null;
            }
        }
    }
}
