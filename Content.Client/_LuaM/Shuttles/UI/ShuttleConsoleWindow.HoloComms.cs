namespace Content.Client.Shuttles.UI;

/// <summary>
/// The comms button of the NAV screen opens the comms window of the console.
/// </summary>
public sealed partial class ShuttleConsoleWindow
{
    public event Action? OnHoloCommsPressed;

    private void InitializeHoloComms()
    {
        NavContainer.OnHoloCommsPressed += () => OnHoloCommsPressed?.Invoke();
    }
}
