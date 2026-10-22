using Content.Shared._LuaM.Salvage;
using Content.Shared.Localizations;
using Content.Shared.Salvage;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._LuaM.HUD;

/// <summary>
/// Small HUD block at the top of the screen with the time until the end of the round
/// and the state of the expedition the player is on.
/// </summary>
public sealed class ShiftInfoPanel : PanelContainer
{
    private static readonly Color TitleColor = Color.FromHex("#CCCCCC");
    private static readonly Color TimeColor = Color.FromHex("#FFDD44");
    private static readonly Color UrgentColor = Color.FromHex("#FF5555");
    private static readonly Color DoneColor = Color.FromHex("#55FF55");
    private static readonly Color RewardColor = Color.FromHex("#44DD44");
    private static readonly Color HintColor = Color.FromHex("#AAAAAA");

    /// <summary>
    /// Timers turn red when less than this is left.
    /// </summary>
    private static readonly TimeSpan UrgentTime = TimeSpan.FromMinutes(1);

    private readonly BoxContainer _roundEndBox;
    private readonly Label _roundEndLabel;

    private readonly BoxContainer _expeditionBox;
    private readonly Label _expeditionTimeLabel;
    private readonly Label _objectiveLabel;
    private readonly Label _directionLabel;
    private readonly Label _rewardLabel;
    private readonly Label _completedLabel;

    public ShiftInfoPanel()
    {
        MouseFilter = MouseFilterMode.Ignore;
        MinWidth = 240;
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#1B1B1ECC"),
            BorderColor = Color.FromHex("#3D3D44"),
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
            ContentMarginTopOverride = 4,
            ContentMarginBottomOverride = 4,
        };

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };

        _roundEndLabel = new Label { HorizontalAlignment = HAlignment.Center };
        _roundEndBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Visible = false };
        _roundEndBox.AddChild(_roundEndLabel);

        _expeditionTimeLabel = new Label();
        _objectiveLabel = new Label();
        _directionLabel = new Label { FontColorOverride = HintColor };
        _rewardLabel = new Label { FontColorOverride = RewardColor };
        _completedLabel = new Label
        {
            Text = Loc.GetString("shift-info-expedition-completed"),
            FontColorOverride = HintColor,
        };

        _expeditionBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Visible = false };
        _expeditionBox.AddChild(new Label
        {
            Text = Loc.GetString("shift-info-expedition-title"),
            FontColorOverride = TitleColor,
            HorizontalAlignment = HAlignment.Center,
        });
        _expeditionBox.AddChild(_expeditionTimeLabel);
        _expeditionBox.AddChild(_objectiveLabel);
        _expeditionBox.AddChild(_directionLabel);
        _expeditionBox.AddChild(_rewardLabel);
        _expeditionBox.AddChild(_completedLabel);

        root.AddChild(_roundEndBox);
        root.AddChild(_expeditionBox);
        AddChild(root);
    }

    /// <summary>
    /// Whether any section has something to show.
    /// </summary>
    public bool HasContent => _roundEndBox.Visible || _expeditionBox.Visible;

    public void UpdateRoundEnd(TimeSpan? remaining)
    {
        _roundEndBox.Visible = remaining is { } left && left > TimeSpan.Zero;
        if (!_roundEndBox.Visible)
            return;

        SetText(_roundEndLabel, Loc.GetString("shift-info-round-end", ("time", FormatTime(remaining!.Value))));
        _roundEndLabel.FontColorOverride = remaining < UrgentTime ? UrgentColor : TimeColor;
    }

    public void UpdateExpedition(ExpeditionHudComponent? hud, TimeSpan curTime)
    {
        _expeditionBox.Visible = hud != null;
        if (hud == null)
            return;

        var remaining = hud.EndTime - curTime;
        SetText(_expeditionTimeLabel, Loc.GetString("shift-info-expedition-time", ("time", FormatTime(remaining))));
        _expeditionTimeLabel.FontColorOverride = remaining < UrgentTime ? UrgentColor : TimeColor;

        var done = hud.Completed || hud.ObjectiveTotal > 0 && hud.ObjectiveRemaining == 0;
        var (objectiveKey, defaultName) = hud.MissionType switch
        {
            SalvageMissionType.Destruction => ("shift-info-expedition-objective-destruction", "shift-info-expedition-default-structures"),
            SalvageMissionType.Elimination => ("shift-info-expedition-objective-elimination", "shift-info-expedition-default-megafauna"),
            _ => (null, null),
        };

        _objectiveLabel.Visible = objectiveKey != null && hud.ObjectiveTotal > 0;
        if (_objectiveLabel.Visible)
        {
            SetText(_objectiveLabel, Loc.GetString(objectiveKey!,
                ("name", hud.ObjectiveName ?? Loc.GetString(defaultName!)),
                ("done", hud.ObjectiveTotal - hud.ObjectiveRemaining),
                ("total", hud.ObjectiveTotal),
                ("completed", done ? "yes" : "no")));
            _objectiveLabel.FontColorOverride = done ? DoneColor : TimeColor;
        }

        _directionLabel.Visible = hud.DungeonDirection != null;
        if (hud.DungeonDirection is { } direction)
        {
            SetText(_directionLabel, Loc.GetString("shift-info-expedition-direction",
                ("direction", ContentLocalizationManager.FormatDirection(direction).ToLower())));
        }

        _rewardLabel.Visible = hud.Reward > 0;
        if (_rewardLabel.Visible)
            SetText(_rewardLabel, Loc.GetString("shift-info-expedition-reward", ("amount", hud.Reward)));

        _completedLabel.Visible = hud.Completed;
    }

    private static string FormatTime(TimeSpan time)
    {
        var seconds = Math.Max(0, (int) Math.Ceiling(time.TotalSeconds));
        return seconds >= 3600
            ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}"
            : $"{seconds / 60:00}:{seconds % 60:00}";
    }

    /// <summary>
    /// Only touches the label when the text changes, the panel is updated every frame.
    /// </summary>
    private static void SetText(Label label, string text)
    {
        if (label.Text != text)
            label.Text = text;
    }
}
