using Robust.Shared.Maths;

namespace Content.Client.Stylesheets;

/// <summary>
/// Monolith-DS interface colors: navy panels, blue borders and cyan accents, the same as the consoles and the AHelp.
/// </summary>
public static class LuaMPalette
{
    /// <summary>
    /// Window and panel background.
    /// </summary>
    public static readonly Color Panel = Color.FromHex("#111923");

    /// <summary>
    /// Darker inset: lists, inputs, trees.
    /// </summary>
    public static readonly Color PanelDeep = Color.FromHex("#0D1219");

    /// <summary>
    /// Slightly lighter rows and secondary panels.
    /// </summary>
    public static readonly Color PanelRaised = Color.FromHex("#162433");

    public static readonly Color Header = Color.FromHex("#202631");
    public static readonly Color Border = Color.FromHex("#2D4757");

    /// <summary>
    /// Headings, window titles and other highlighted text.
    /// </summary>
    public static readonly Color AccentText = Color.FromHex("#4EC6E8");

    public static readonly Color Accent = Color.FromHex("#2EA7D0");

    public static readonly Color Button = Color.FromHex("#1C2E40");
    public static readonly Color ButtonHover = Color.FromHex("#23506B");
    public static readonly Color ButtonPressed = Color.FromHex("#1A6A8A");
    public static readonly Color ButtonDisabled = Color.FromHex("#121922");

    /// <summary>
    /// Selected list row or active tab.
    /// </summary>
    public static readonly Color Selected = Color.FromHex("#1E4A66");

    public static readonly Color Scrollbar = Color.FromHex("#4E7FA0");
    public static readonly Color ScrollbarHover = Color.FromHex("#6FA3C4");
}
