using System.Numerics;
using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._LuaM.RpPanel;

public sealed class RpSoundOptionButton : OptionButton
{
    private const string PlayIcon = "/Textures/Interface/AdminActions/play.png";

    public event Action<int>? OnPreview;

    public override void ButtonOverride(Button button)
    {
        var id = GetItemId(ItemCount - 1);
        if (id < 0)
            return;

        var play = new Button
        {
            SetSize = new Vector2(22, 22),
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Center,
            ToolTip = Loc.GetString("rp-custom-emote-sound-preview"),
            StyleClasses = { DeadSpaceAHelpStyles.ProfileControl },
        };
        play.AddChild(new TextureRect
        {
            TexturePath = PlayIcon,
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(10, 10),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Modulate = RpPanelStyles.AccentColor,
            MouseFilter = MouseFilterMode.Ignore,
        });
        play.OnPressed += _ => OnPreview?.Invoke(id);

        button.Label.Margin = new Thickness(0, 0, 26, 0);
        button.AddChild(play);
    }
}
