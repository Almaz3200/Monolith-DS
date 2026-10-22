using Robust.Shared.Map;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Autopilot to the radar marker and removing it.
/// </summary>
public sealed partial class NavScreen
{
    public event Action<MapCoordinates, Angle>? OnTargetAutopilot;
    public event Action? OnClearTarget;

    private void InitializeTargetControls()
    {
        TargetAutopilot.OnPressed += _ => AutopilotToTarget();
        TargetClear.OnPressed += _ => ClearTarget();
    }

    private void AutopilotToTarget()
    {
        // Sets the marker from the fields first, so the pilot sees where the shuttle goes.
        SetTargetCoords();

        if (NavRadar.Target is not { } target
            || !ShuttleMarkerAutopilot.TryGetDestination(_entManager, _shuttleEntity, target, out var destination, out var angle))
            return;

        OnTargetAutopilot?.Invoke(destination, angle);
    }

    private void ClearTarget()
    {
        NavRadar.Target = null;
        NavRadar.TargetEntity = NetEntity.Invalid;
        TargetX.Text = 0.0f.ToString("F1");
        TargetY.Text = 0.0f.ToString("F1");
        _targetCoordsModified = false;

        OnClearTarget?.Invoke();
    }
}
