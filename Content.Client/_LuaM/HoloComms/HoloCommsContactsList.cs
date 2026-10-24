using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared._LuaM.HoloComms;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._LuaM.HoloComms;

/// <summary>
/// The list of everybody the console can call, ships first, then the holopads.
/// It stays in the comms window, picking a contact places the call.
/// </summary>
public sealed class HoloCommsContactsList : BoxContainer
{
    public event Action<NetEntity>? OnContactPicked;

    private readonly LineEdit _search;
    private readonly BoxContainer _list;
    private readonly Label _empty;
    private IReadOnlyList<HoloCommsContact> _contacts = Array.Empty<HoloCommsContact>();
    private bool _locked;

    public HoloCommsContactsList()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;

        var header = new Label { Text = Loc.GetString("holo-comms-contacts-title") };
        header.AddStyleClass(ConsoleStyles.HeaderLabel);

        _search = new LineEdit
        {
            PlaceHolder = Loc.GetString("holo-comms-contacts-search"),
            HorizontalExpand = true,
        };
        _search.AddStyleClass(ConsoleStyles.Input);
        _search.OnTextChanged += _ => Rebuild();

        _list = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 2,
            Margin = new Thickness(4),
        };

        _empty = new Label
        {
            Text = Loc.GetString("holo-comms-contacts-empty"),
            HorizontalAlignment = HAlignment.Center,
            Margin = new Thickness(4, 8),
            FontColorOverride = Color.FromHex("#7A8899"),
        };

        var listPanel = new PanelContainer { VerticalExpand = true, MinHeight = 90 };
        listPanel.AddStyleClass(ConsoleStyles.Inset);
        listPanel.AddChild(new ScrollContainer
        {
            HScrollEnabled = false,
            VerticalExpand = true,
            Children = { new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children = { _list, _empty },
            } },
        });

        var root = new PanelContainer { VerticalExpand = true };
        root.AddStyleClass(ConsoleStyles.Section);
        root.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(4),
            SeparationOverride = 4,
            Children = { header, _search, listPanel },
        });

        AddChild(root);
        Rebuild();
    }

    public void SetContacts(IReadOnlyList<HoloCommsContact> contacts)
    {
        // Rebuilding the same list would throw the scroll back to the top
        if (_contacts.SequenceEqual(contacts))
            return;

        _contacts = contacts.ToArray();
        Rebuild();
    }

    /// <summary>
    /// No calls can be placed while the line is taken, the list stays to read.
    /// </summary>
    public void SetLocked(bool locked)
    {
        if (_locked == locked)
            return;

        _locked = locked;
        Rebuild();
    }

    private void Rebuild()
    {
        _list.RemoveAllChildren();

        var filter = _search.Text.Trim();
        HoloCommsContactKind? section = null;
        var number = 0;

        foreach (var contact in _contacts)
        {
            if (filter.Length > 0 && !contact.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                continue;

            if (section != contact.Kind)
            {
                section = contact.Kind;

                var header = new Label
                {
                    Text = Loc.GetString(contact.Kind == HoloCommsContactKind.Ship
                        ? "holo-comms-contacts-ships"
                        : "holo-comms-contacts-holopads"),
                    Margin = new Thickness(2, _list.ChildCount == 0 ? 0 : 6, 2, 2),
                    FontColorOverride = Color.FromHex("#9FB3C8"),
                };
                _list.AddChild(header);
            }

            number++;
            var name = contact.Busy
                ? Loc.GetString("holo-comms-contact-busy", ("name", contact.Name))
                : contact.Name;

            var button = new Button
            {
                Text = Loc.GetString("holo-comms-contact-entry", ("number", number), ("name", name)),
                Disabled = contact.Busy || _locked,
                ClipText = true,
                HorizontalExpand = true,
                ToolTip = contact.Name,
            };
            button.AddStyleClass(ConsoleStyles.Button);

            // A clipped label wants no width, aligned to the left it got none and the names were not drawn.
            // It keeps the whole button and the text goes to the left, the style centers it for that class.
            button.Label.RemoveStyleClass(Button.StyleClassButton);
            button.TextAlign = Label.AlignMode.Left;

            var target = contact.Target;
            button.OnPressed += _ => OnContactPicked?.Invoke(target);

            _list.AddChild(button);
        }

        _empty.Visible = _list.ChildCount == 0;
    }
}
