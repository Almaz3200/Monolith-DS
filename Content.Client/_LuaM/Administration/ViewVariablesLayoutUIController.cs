using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._LuaM.Administration;

/// <summary>
/// Keeps the entity View Variables window clickable when its tabs wrap to a second row.
/// The window scrolls sideways, so the tabs are measured as one row but laid out as two,
/// and the last component button is left with no height.
/// </summary>
public sealed class ViewVariablesLayoutUIController : UIController
{
    public override void Initialize()
    {
        base.Initialize();
        UIManager.WindowRoot.OnChildAdded += OnWindowAdded;
    }

    private void OnWindowAdded(Control control)
    {
        if (control is not DefaultWindow window || window.Title != Loc.GetString("view-variables"))
            return;

        foreach (var child in window.Contents.Children)
        {
            TryFitWidth(child);
        }

        // A server entity fills its window only after the data arrives.
        window.Contents.OnChildAdded += child => UIManager.DeferAction(() => TryFitWidth(child));
    }

    private static void TryFitWidth(Control control)
    {
        if (control.Disposed || control is not ScrollContainer scroll)
            return;

        foreach (var box in scroll.Children)
        {
            foreach (var child in box.Children)
            {
                if (child is not TabContainer)
                    continue;

                scroll.HScrollEnabled = false;
                return;
            }
        }
    }
}
