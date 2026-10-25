using Content.Shared.Fax.Components;
using Content.Shared.Paper;

namespace Content.Server.StationEvents.Components;

public interface IPreFaxAction
{
    /// <summary>
    ///     Initializes the action. Intended to setup resources, but the action should not be stateful.
    /// </summary>
    public void Initialize();

    /// <summary>
    ///     Formats a fax printout with general information (target station)
    /// </summary>
    public void Format(EntityUid station, ref EditableFaxPrintout printout, ref string? fromAddress);
}

public interface IRecipientFaxAction
{
    /// <summary>
    ///     Initializes the action. Intended to setup resources, but the action should not be stateful.
    /// </summary>
    public void Initialize();

    /// <summary>
    ///     Formats a fax printout with recipient-specific information (target station, fax machine entity)
    /// </summary>
    public void Format(EntityUid station, EntityUid fax, FaxMachineComponent faxComponent, ref EditableFaxPrintout printout, ref string? fromAddress);
}

public sealed partial class EditableFaxPrintout
{
    public string Name = default!;
    public string? Label;
    public string Content = default!;
    public string PrototypeId = default!;
    public string? StampState;
    public List<StampDisplayInfo> StampedBy = new();
    public bool Locked;
    public bool StampProtected;
}