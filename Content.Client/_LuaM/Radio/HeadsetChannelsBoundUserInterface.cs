using Content.Shared._LuaM.Radio;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._LuaM.Radio;

[UsedImplicitly]
public sealed class HeadsetChannelsBoundUserInterface : BoundUserInterface
{
    private HeadsetChannelsWindow? _window;

    public HeadsetChannelsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<HeadsetChannelsWindow>();
        _window.OnChannelToggled += (channel, enabled) => SendMessage(new HeadsetChannelToggleMessage(channel, enabled));
        _window.OnFrequencySet += frequency => SendMessage(new HeadsetSetFrequencyMessage(frequency));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is HeadsetChannelsBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
