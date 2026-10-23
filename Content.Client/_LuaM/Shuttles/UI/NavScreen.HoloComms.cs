using Content.Shared._LuaM.HoloComms;
using Content.Shared.Telephone;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// The comms button on top of the weapons panel, it blinks while the ship is being called.
/// </summary>
public sealed partial class NavScreen
{
    private static readonly Color HoloCommsRingColor = Color.FromHex("#4EC6E8");
    private static readonly Color HoloCommsLineColor = Color.FromHex("#6FD38A");

    public event Action? OnHoloCommsPressed;

    private bool _holoCommsAvailable;
    private TelephoneState? _holoCommsState;

    private void InitializeHoloComms()
    {
        HoloCommsButton.OnPressed += _ => OnHoloCommsPressed?.Invoke();
    }

    private void UpdateHoloCommsButton()
    {
        var available = _entManager.TryGetComponent<HoloCommsConsoleComponent>(_consoleEntity, out var comms);
        if (available != _holoCommsAvailable)
        {
            _holoCommsAvailable = available;
            HoloCommsButton.Visible = available;
            UpdateWeaponsPanelVisibility();
        }

        if (comms == null)
            return;

        var state = TelephoneState.Idle;
        if (_entManager.TryGetComponent<TelephoneComponent>(comms.Module, out var telephone))
            state = telephone.CurrentState;

        if (state != _holoCommsState)
        {
            _holoCommsState = state;
            HoloCommsButton.Text = Loc.GetString(state switch
            {
                TelephoneState.Ringing => "holo-comms-button-ringing",
                TelephoneState.Calling => "holo-comms-button-calling",
                TelephoneState.InCall => "holo-comms-button-in-call",
                _ => "holo-comms-button",
            });
        }

        // Only the color changes every frame, the layout stays as it is
        HoloCommsButton.ModulateSelfOverride = state switch
        {
            TelephoneState.Ringing => (int) (_timing.RealTime.TotalSeconds * 2) % 2 == 0 ? HoloCommsRingColor : null,
            TelephoneState.InCall => HoloCommsLineColor,
            _ => null,
        };
    }
}
