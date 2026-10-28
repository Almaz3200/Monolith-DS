using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Shared._NF.CryoSleep.SharedCryoSleepSystem;

namespace Content.Client._NF.CryoSleep;

public sealed partial class CryosleepWakeupWindow : DefaultWindow
{
    public RichTextLabel Label;
    public Button DenyButton;
    public Button AcceptButton;

    public event Action? AcceptPressed; // LuaM

    public CryosleepWakeupWindow()
    {
        Title = Loc.GetString("cryo-wakeup-window-title");

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children =
                    {
                        (Label = new RichTextLabel()
                        {
                            HorizontalExpand = true,
                            MaxWidth = 300,
                            StyleClasses = {  }
                        }),
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            Align = BoxContainer.AlignMode.Center,
                            Children =
                            {
                                (AcceptButton = new Button
                                {
                                    Text = Loc.GetString("cryo-wakeup-window-accept-button"),
                                }),

                                (new Control()
                                {
                                    MinSize = new Vector2i(20, 0)
                                }),

                                (DenyButton = new Button
                                {
                                    Text = Loc.GetString("cryo-wakeup-window-deny-button"),
                                })
                            }
                        },
                    }
                },
            }
        });

        DenyButton.OnPressed += _ => Close();
        AcceptButton.OnPressed += _ => OnAccept();
    }

    protected override void Opened()
    {
        Label.SetMessage(Loc.GetString("cryo-wakeup-window-rules"));
        DenyButton.Disabled = false;
        AcceptButton.Disabled = false;
    }

    private void OnAccept()
    {
        AcceptPressed?.Invoke(); // LuaM

        // Disable the buttons to make the user wait for a response
        AcceptButton.Disabled = true;
        DenyButton.Disabled = true;
    }

    public void HandleWakeupResponse(ReturnToBodyStatus status) // LuaM
    {
        if (status == ReturnToBodyStatus.Success) // LuaM
        {
            Close();
            return;
        }

        // Failure
        DenyButton.Disabled = false;
        if (status == ReturnToBodyStatus.Occupied) // LuaM
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-occupied"));
        else if (status == ReturnToBodyStatus.NoCryopodAvailable) // LuaM
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-no-cryopod"));
        else if (status == ReturnToBodyStatus.BodyMissing) // LuaM
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-no-body"));
        else if (status == ReturnToBodyStatus.Disabled) // LuaM
            Label.SetMessage(Loc.GetString("cryo-wakeup-result-disabled"));
    }
}
