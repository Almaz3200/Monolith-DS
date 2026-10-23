using Robust.Shared.GameStates;

namespace Content.Shared._LuaM.Shuttles;

/// <summary>
/// Gives a grid a friendly IFF group directly, instead of taking it from the grid's company.
/// Used for faction stations such as Helios.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FriendlyIffComponent : Component
{
    /// <summary>
    /// Viewers whose company has this <see cref="Content.Shared._Mono.Company.CompanyPrototype.FriendlyIffGroup"/> treat the grid as friendly.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Group = string.Empty;

    /// <summary>
    /// If true, friendly viewers see this grid's label at any range, even through <c>HideLabelAlways</c>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlwaysVisible;
}
