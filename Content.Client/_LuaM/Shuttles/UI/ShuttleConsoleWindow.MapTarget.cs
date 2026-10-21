namespace Content.Client.Shuttles.UI;

/// <summary>
/// The map marker sends the same requests as the radar marker on NAV, both can fly the autopilot there or remove it.
/// </summary>
public sealed partial class ShuttleConsoleWindow
{
    public event Action? OnClearTarget;

    private void InitializeMapTarget()
    {
        MapContainer.OnSetTargetCoordinates += (entity, position) => OnSetTargetCoordinates?.Invoke(entity, position);
        MapContainer.OnSetHideTarget += (entity, hide) => OnSetHideTarget?.Invoke(entity, hide);
        MapContainer.OnClearTarget += () => OnClearTarget?.Invoke();

        NavContainer.OnTargetAutopilot += (coords, angle) => RequestAutopilot?.Invoke(coords, angle);
        NavContainer.OnClearTarget += () => OnClearTarget?.Invoke();
    }
}
