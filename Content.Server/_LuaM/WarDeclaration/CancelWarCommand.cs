using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server._Mono.WarDeclarator;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server._LuaM.WarDeclaration;

/// <summary>
/// Cancels the war between the factions: the war level goes back to cold, see <see cref="ManualPortstrikeRuleSystem.CancelWar"/>.
/// </summary>
[AdminCommand(AdminFlags.Round)]
public sealed partial class CancelWarCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private ManualPortstrikeRuleSystem _portstrike = default!;

    public override string Command => "cancelwar";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _portstrike.CancelWar();
        _adminLogger.Add(LogType.Action, LogImpact.High, $"{shell.Player?.Name ?? "Server"} cancelled the war");
        shell.WriteLine(Loc.GetString("cmd-cancelwar-done"));
    }
}
