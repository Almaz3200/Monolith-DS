using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._LuaM.RpPanel;

public static class RpPanelStyles
{
    public const string Section = "RpPanelSection";
    public const string List = "RpPanelList";
    public const string IconFrame = "RpPanelIconFrame";
    public const string Accent = "RpPanelAccent";
    public const string AccentDanger = "RpPanelAccentDanger";
    public const string Category = "RpPanelCategory";
    public const string Option = "RpPanelOption";
    public const string Text = "RpPanelText";
    public const string Muted = "RpPanelMuted";
    public const string Heading = "RpPanelHeading";
    public const string Toggle = "RpPanelToggle";

    public static readonly Color HeaderBg = Color.FromHex("#202631F5");
    public static readonly Color HeaderBorder = Color.FromHex("#5D6A7C");
    public static readonly Color ControlBg = Color.FromHex("#111923F0");
    public static readonly Color ControlBorder = Color.FromHex("#2D4757");
    public static readonly Color HoverBg = Color.FromHex("#162638F4");
    public static readonly Color HoverBorder = Color.FromHex("#1D7E9D");
    public static readonly Color PressedBg = Color.FromHex("#18344AF5");
    public static readonly Color PressedBorder = Color.FromHex("#2EA7D0");
    public static readonly Color DisabledBg = Color.FromHex("#10161FC8");
    public static readonly Color DisabledBorder = Color.FromHex("#293844");
    public static readonly Color InputBg = Color.FromHex("#0D1219F6");
    public static readonly Color TextColor = Color.FromHex("#F1F3F6");
    public static readonly Color TextMuted = Color.FromHex("#9BA6AD");
    public static readonly Color TextPlaceholder = Color.FromHex("#7A8590");
    public static readonly Color AccentColor = Color.FromHex("#4EC6E8");
    public static readonly Color DangerColor = Color.FromHex("#F85149");

    public static StyleRule[] GetRules(IResourceCache resCache)
    {
        var category = new StyleBoxFlat
        {
            BackgroundColor = HeaderBg,
            BorderColor = HeaderBorder,
            BorderThickness = new Thickness(0, 0, 0, 1),
            ContentMarginTopOverride = 6,
            ContentMarginBottomOverride = 6,
            ContentMarginLeftOverride = 4,
            ContentMarginRightOverride = 8,
        };

        var option = new StyleBoxFlat
        {
            BackgroundColor = ControlBg,
            BorderColor = ControlBorder,
            BorderThickness = new Thickness(1),
            ContentMarginTopOverride = 4,
            ContentMarginBottomOverride = 4,
            ContentMarginLeftOverride = 9,
            ContentMarginRightOverride = 9,
        };

        return
        [
            Element<PanelContainer>().Class(Section)
                .Prop(PanelContainer.StylePropertyPanel, Box(ControlBg, ControlBorder)),
            Element<PanelContainer>().Class(List)
                .Prop(PanelContainer.StylePropertyPanel, Box(InputBg, ControlBorder)),
            Element<PanelContainer>().Class(IconFrame)
                .Prop(PanelContainer.StylePropertyPanel, Box(InputBg, ControlBorder)),
            Element<PanelContainer>().Class(Accent)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = AccentColor }),
            Element<PanelContainer>().Class(AccentDanger)
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat { BackgroundColor = DangerColor }),

            ContainerRule<ContainerButton>(Category, null, category),
            ContainerRule<ContainerButton>(Category, ContainerButton.StylePseudoClassHover,
                new StyleBoxFlat(category) { BackgroundColor = HoverBg, BorderColor = HoverBorder }),
            ContainerRule<ContainerButton>(Category, ContainerButton.StylePseudoClassPressed,
                new StyleBoxFlat(category) { BackgroundColor = PressedBg, BorderColor = PressedBorder }),

            ContainerRule<OptionButton>(Option, null, option),
            ContainerRule<OptionButton>(Option, ContainerButton.StylePseudoClassHover,
                new StyleBoxFlat(option) { BackgroundColor = HoverBg, BorderColor = HoverBorder }),
            ContainerRule<OptionButton>(Option, ContainerButton.StylePseudoClassPressed,
                new StyleBoxFlat(option) { BackgroundColor = PressedBg, BorderColor = PressedBorder }),
            ContainerRule<OptionButton>(Option, ContainerButton.StylePseudoClassDisabled,
                new StyleBoxFlat(option) { BackgroundColor = DisabledBg, BorderColor = DisabledBorder }),

            Element<Label>().Class(Text)
                .Prop(Label.StylePropertyFontColor, TextColor),
            Element<Label>().Class(Muted)
                .Prop(Label.StylePropertyFontColor, TextMuted),
            Element<Label>().Class(Heading)
                .Prop(Label.StylePropertyFont, resCache.NotoStack(variation: "Bold", size: 12))
                .Prop(Label.StylePropertyFontColor, TextColor),

            Element<Button>().Class(Toggle).Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(ContainerButton.StylePropertyStyleBox, new StyleBoxFlat(option)
                {
                    BackgroundColor = HoverBorder.WithAlpha(0.55f),
                    BorderColor = AccentColor,
                }),
            Child().Parent(Element<Button>().Class(Toggle).Pseudo(ContainerButton.StylePseudoClassPressed))
                .Child(Element<Label>())
                .Prop(Label.StylePropertyFont, resCache.NotoStack(variation: "Bold"))
                .Prop(Label.StylePropertyFontColor, TextColor),
        ];
    }

    private static StyleBoxFlat Box(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BackgroundColor = background,
            BorderColor = border,
            BorderThickness = new Thickness(1),
        };
    }

    private static StyleRule ContainerRule<T>(string styleClass, string? pseudoClass, StyleBox styleBox) where T : ContainerButton
    {
        var selector = Element<T>().Class(styleClass);
        if (pseudoClass != null)
            selector = selector.Pseudo(pseudoClass);

        return selector.Prop(ContainerButton.StylePropertyStyleBox, styleBox);
    }
}
