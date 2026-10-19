using Content.Shared._LuaM.RpPanel;
using Robust.Client.UserInterface;

namespace Content.Client._LuaM.RpPanel;

public sealed class RpPanelBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private RpPanelWindow? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<RpPanelWindow>();
        _menu.OnSendEmote += (interaction, privateMode) => SendMessage(new RpPanelSendMessage(interaction, privateMode));
        _menu.OnSendCustomEmote += (emote, privateMode) => SendMessage(new RpPanelCustomEmoteMessage(emote, privateMode));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu is null)
            return;

        if (state is RpPanelBuiState newState)
            _menu.SetTarget(newState.User, newState.Target);
    }
}
