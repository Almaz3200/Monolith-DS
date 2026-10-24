using System.Linq;
using Content.Shared._Mono.Company;
using Robust.Shared.Prototypes;

namespace Content.Server._Mono.WarDeclarator;

// LuaM: capitulation from the faction consoles and the admin war cancel
public sealed partial class ManualPortstrikeRuleSystem
{
    /// <summary>
    /// The state of the war the faction takes part in.
    /// </summary>
    /// <param name="active">Some faction has declared war and nobody has capitulated yet (warm or hot war level).</param>
    /// <param name="over">The war ended with a capitulation (white war level).</param>
    public bool TryGetWarStatus(ProtoId<CompanyPrototype> faction, out bool active, out bool over)
    {
        active = false;
        over = false;
        var found = false;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            if (!comp.SectorStatus.ContainsKey(faction))
                continue;

            found = true;
            over |= comp.SurrenderedFaction != null;
            active |= comp.SurrenderedFaction == null && comp.SectorStatus.ContainsValue(true);
        }

        return found;
    }

    /// <summary>
    /// The faction gives up. The war ends with the white war level and can't be declared again this round.
    /// </summary>
    public bool TrySurrender(ProtoId<CompanyPrototype> faction)
    {
        var surrendered = false;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            if (!comp.SectorStatus.ContainsKey(faction) ||
                comp.SurrenderedFaction != null ||
                !comp.SectorStatus.ContainsValue(true))
                continue;

            comp.SurrenderedFaction = faction;
            comp.FirstDeclarationTime = null; // Nobody gets war declared for them automatically anymore.
            surrendered = true;
        }

        if (surrendered)
            _warLevelSystem.SetWhite();

        return surrendered;
    }

    /// <summary>
    /// Admin action: back to the cold war level, as if nobody had declared war. A capitulation is reset too.
    /// </summary>
    public void CancelWar()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out _, out var comp, out _))
        {
            foreach (var faction in comp.SectorStatus.Keys.ToArray())
            {
                comp.SectorStatus[faction] = false;
            }

            comp.FirstDeclarationTime = null;
            comp.SurrenderedFaction = null;
        }

        _warLevelSystem.SetLevel(false);
        _comms.UpdateCommsConsoleInterface();
    }
}
