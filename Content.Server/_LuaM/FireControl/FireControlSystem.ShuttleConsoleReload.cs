using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server._Mono.FireControl;

public sealed partial class FireControlSystem
{
    /// <summary>
    /// Refreshes the open shuttle consoles of a grid, so their weapons list gets the new reload times.
    /// </summary>
    public void QueueShuttleConsoleUpdates(EntityUid grid)
    {
        var query = EntityQueryEnumerator<ShuttleConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var console, out _, out var xform))
        {
            if (xform.GridUid == grid && _ui.IsUiOpen(console, ShuttleConsoleUiKey.Key))
                QueueShuttleConsoleUpdate(console);
        }
    }
}
