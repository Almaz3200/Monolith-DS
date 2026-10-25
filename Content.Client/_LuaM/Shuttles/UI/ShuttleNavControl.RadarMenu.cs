using System.Numerics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Shuttles.UI;

public partial class ShuttleNavControl
{
    [Dependency] private IInputManager _input = default!;

    private const float RadarMenuBlipRadius = 12f;

    private readonly List<RadarMenuTarget> _radarMenuTargets = new();

    public event Action<EntityUid>? OnRadarMenuRequested;

    private readonly record struct RadarMenuTarget(EntityUid Grid, Vector2 Anchor, UIBox2 Label, UIBox2? Hull);

    public bool IsRadarMenuTarget(EntityUid grid)
    {
        foreach (var target in _radarMenuTargets)
        {
            if (target.Grid == grid)
                return true;
        }

        return false;
    }

    private void AddRadarMenuTarget(EntityUid grid, Vector2 anchor, Vector2 labelPosition, Vector2 labelSize, Box2? hull)
    {
        UIBox2? hullBox = hull is { } box
            ? new UIBox2(box.BottomLeft / UIScale, box.TopRight / UIScale)
            : null;

        _radarMenuTargets.Add(new RadarMenuTarget(grid, anchor, UIBox2.FromDimensions(labelPosition, labelSize), hullBox));
    }

    private bool TryRequestRadarMenu(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UseSecondary || !_input.IsKeyDown(Keyboard.Key.Alt))
            return false;

        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var target in _radarMenuTargets)
        {
            var distance = Vector2.Distance(target.Anchor, args.RelativePosition);
            var hit = distance <= RadarMenuBlipRadius ||
                      target.Label.Contains(args.RelativePosition) ||
                      target.Hull?.Contains(args.RelativePosition) == true;

            if (!hit || distance >= bestDistance)
                continue;

            best = target.Grid;
            bestDistance = distance;
        }

        if (best != null)
            OnRadarMenuRequested?.Invoke(best.Value);

        args.Handle();
        return true;
    }
}
