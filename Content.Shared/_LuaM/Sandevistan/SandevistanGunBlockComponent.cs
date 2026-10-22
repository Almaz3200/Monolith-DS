using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Sandevistan;

/// <summary>
/// Added while a Sandevistan is running: the user can't shoot guns, only fight in melee.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SandevistanGunBlockComponent : Component
{
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextPopup;
}
