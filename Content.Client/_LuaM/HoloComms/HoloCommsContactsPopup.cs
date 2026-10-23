using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared._LuaM.HoloComms;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._LuaM.HoloComms;

/// <summary>
/// The drop-down list of everybody the console can call, ships first, then the holopads.
/// </summary>
public sealed class HoloCommsContactsPopup : Popup
{
    private const float MaxListHeight = 320f;

    public event Action<NetEntity>? OnContactPicked;

    private readonly LineEdit _search;
    private readonly BoxContainer _list;
    private readonly Label _empty;
    private IReadOnlyList<HoloCommsContact> _contacts = Array.Empty<HoloCommsContact>();

    public HoloCommsContactsPopup()
    {
        _search = new LineEdit
        {
            PlaceHolder = Loc.GetString("holo-comms-contacts-search"),
            HorizontalExpand = true,
        };
        _search.AddStyleClass(ConsoleStyles.Input);
        _search.OnTextChanged += _ => Rebuild();

        _list = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
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

        var listPanel = new PanelContainer { VerticalExpand = true };
        listPanel.AddStyleClass(ConsoleStyles.Inset);
        listPanel.AddChild(new ScrollContainer
        {
            HScrollEnabled = false,
            VerticalExpand = true,
            Children = { new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children = { _list, _empty },
            } },
        });

        var root = new PanelContainer();
        root.AddStyleClass(ConsoleStyles.Section);
        root.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(4),
            SeparationOverride = 4,
            Children = { _search, listPanel },
        });

        AddChild(root);
        OnPopupHide += () =>
        {
            _search.Clear();
            Orphan();
        };
    }

    public void SetContacts(IReadOnlyList<HoloCommsContact> contacts)
    {
        _contacts = contacts;

        if (Visible)
            Rebuild();
    }

    /// <summary>
    /// Opens the list right under the given control, as wide as it.
    /// </summary>
    public void OpenBelow(Control anchor)
    {
        if (anchor.Root == null)
            return;

        Rebuild();

        var position = anchor.GlobalPosition + new Vector2(0f, anchor.Size.Y + 2f);
        var height = MathF.Min(MaxListHeight, anchor.Root.Size.Y - position.Y - 4f);

        anchor.Root.ModalRoot.AddChild(this);
        Open(UIBox2.FromDimensions(position, new Vector2(anchor.Size.X, MathF.Max(120f, height))));
        _search.GrabKeyboardFocus();
    }

    private void Rebuild()
    {
        _list.RemoveAllChildren();

        var filter = _search.Text.Trim();
        HoloCommsContactKind? section = null;

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
                };
                header.AddStyleClass(ConsoleStyles.HeaderLabel);
                _list.AddChild(header);
            }

            var button = new Button
            {
                Text = contact.Busy
                    ? Loc.GetString("holo-comms-contact-busy", ("name", contact.Name))
                    : contact.Name,
                Disabled = contact.Busy,
                ClipText = true,
                HorizontalExpand = true,
                ToolTip = contact.Name,
            };
            button.AddStyleClass(ConsoleStyles.Button);

            var target = contact.Target;
            button.OnPressed += _ =>
            {
                OnContactPicked?.Invoke(target);
                Close();
            };

            _list.AddChild(button);
        }

        _empty.Visible = _list.ChildCount == 0;
    }
}
