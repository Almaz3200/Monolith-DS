namespace Content.Shared._LuaM.RpPanel;

[RegisterComponent]
public sealed partial class RpPanelOwnerComponent : Component
{
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(2);

    public EntityUid? Target;

    public TimeSpan LastInteraction;
}
