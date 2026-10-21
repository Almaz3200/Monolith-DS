using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.Stylesheets;

/// <summary>
/// Shuttle, gunnery and radar console look, in the colors of the Dead Space 14 AHelp menu.
/// </summary>
public static class ConsoleStyles
{
    /// <summary>
    /// A section of the console.
    /// </summary>
    public const string Section = "LuaMConsoleSection";

    /// <summary>
    /// A darker inset inside a section: readouts and lists.
    /// </summary>
    public const string Inset = "LuaMConsoleInset";

    /// <summary>
    /// Section header strip, holds a <see cref="HeaderLabel"/>.
    /// </summary>
    public const string Header = "LuaMConsoleHeader";

    /// <summary>
    /// Section title.
    /// </summary>
    public const string HeaderLabel = "LuaMConsoleHeaderLabel";

    /// <summary>
    /// Any console button, toggles show the pressed look while on.
    /// </summary>
    public const string Button = "LuaMConsoleButton";

    /// <summary>
    /// Console text input.
    /// </summary>
    public const string Input = "LuaMConsoleInput";

    public static StyleRule[] GetRules(IResourceCache resCache)
    {
        var section = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#111923F0"),
            BorderColor = Color.FromHex("#2D4757"),
            BorderThickness = new Thickness(1),
        };
        var inset = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0D1219F6"),
            BorderColor = Color.FromHex("#2D4757"),
            BorderThickness = new Thickness(1),
        };
        var header = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#202631F5"),
            BorderColor = Color.FromHex("#5D6A7C"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            ContentMarginTopOverride = 8,
            ContentMarginBottomOverride = 8,
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
        };
        var input = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#0D1219F6"),
            BorderColor = Color.FromHex("#2D4757"),
            BorderThickness = new Thickness(1),
            ContentMarginTopOverride = 4,
            ContentMarginBottomOverride = 4,
            ContentMarginLeftOverride = 7,
            ContentMarginRightOverride = 7,
        };

        var button = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#111923F0"),
            BorderColor = Color.FromHex("#2D4757"),
            BorderThickness = new Thickness(1),
            ContentMarginTopOverride = 3,
            ContentMarginBottomOverride = 3,
            ContentMarginLeftOverride = 6,
            ContentMarginRightOverride = 6,
        };
        var buttonHover = new StyleBoxFlat(button)
        {
            BackgroundColor = Color.FromHex("#162638F4"),
            BorderColor = Color.FromHex("#1D7E9D"),
        };
        var buttonPressed = new StyleBoxFlat(button)
        {
            BackgroundColor = Color.FromHex("#18344AF5"),
            BorderColor = Color.FromHex("#2EA7D0"),
        };
        var buttonDisabled = new StyleBoxFlat(button)
        {
            BackgroundColor = Color.FromHex("#10161FC8"),
            BorderColor = Color.FromHex("#293844"),
        };

        return
        [
            Element<PanelContainer>().Class(Section)
                .Prop(PanelContainer.StylePropertyPanel, section),
            Element<PanelContainer>().Class(Inset)
                .Prop(PanelContainer.StylePropertyPanel, inset),
            Element<PanelContainer>().Class(Header)
                .Prop(PanelContainer.StylePropertyPanel, header),
            Element<Label>().Class(HeaderLabel)
                .Prop(Label.StylePropertyFont, resCache.NotoStack(variation: "Bold", size: 12))
                .Prop(Label.StylePropertyFontColor, Color.FromHex("#4EC6E8")),

            Element<LineEdit>().Class(Input)
                .Prop(LineEdit.StylePropertyStyleBox, input)
                .Prop("font-color", Color.FromHex("#F1F3F6")),
            Element<LineEdit>().Class(Input).Pseudo(LineEdit.StylePseudoClassPlaceholder)
                .Prop("font-color", Color.FromHex("#7A8590")),

            // The default button rules tint every state through ModulateSelf, the console colors stay as they are.
            Element<ContainerButton>().Class(Button)
                .Prop(ContainerButton.StylePropertyStyleBox, button)
                .Prop(Control.StylePropertyModulateSelf, Color.White),
            ButtonState(ContainerButton.StylePseudoClassNormal, button),
            ButtonState(ContainerButton.StylePseudoClassHover, buttonHover),
            ButtonState(ContainerButton.StylePseudoClassPressed, buttonPressed),
            ButtonState(ContainerButton.StylePseudoClassDisabled, buttonDisabled),
        ];
    }

    private static StyleRule ButtonState(string pseudoClass, StyleBox styleBox)
    {
        return Element<ContainerButton>().Class(Button).Pseudo(pseudoClass)
            .Prop(ContainerButton.StylePropertyStyleBox, styleBox)
            .Prop(Control.StylePropertyModulateSelf, Color.White);
    }
}
