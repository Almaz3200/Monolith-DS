using Robust.Shared.Map;

namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleConsoleWindow
{
    public event Action? OnFireControlRefresh;
    public event Action<List<NetEntity>, NetCoordinates>? OnGunneryFire;

    private void InitializeGunnery()
    {
        NavContainer.OnFireControlRefresh += () => OnFireControlRefresh?.Invoke();
        NavContainer.OnGunneryFire += (selected, coordinates) => OnGunneryFire?.Invoke(selected, coordinates);
    }
}
