using System.Diagnostics.CodeAnalysis;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Shared._NF.Market;
using Content.Shared.GameTicking;

namespace Content.Server._NF.Market.Systems;

/// <summary>
/// All station markets share one buyback pool, so goods sold anywhere can be bought back anywhere.
/// </summary>
public sealed partial class MarketSystem
{
    private List<MarketData> _sharedMarket = new();

    private void InitializeSharedPool()
    {
        SubscribeLocalEvent<CargoMarketDataComponent, ComponentInit>(OnMarketDataInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnMarketDataInit(Entity<CargoMarketDataComponent> ent, ref ComponentInit args)
    {
        if (ReferenceEquals(ent.Comp.MarketDataList, _sharedMarket))
            return;

        foreach (var data in ent.Comp.MarketDataList)
        {
            _sharedMarket.Upsert(data.Prototype, data.Quantity, data.Price, data.StackPrototype);
        }

        ent.Comp.MarketDataList = _sharedMarket;
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _sharedMarket = new();
    }

    /// <summary>
    /// The market whose whitelists apply to a sale on this grid.
    /// Falls back to any station market, so sales at grids without their own market still reach the shared pool.
    /// </summary>
    private bool TryGetSaleMarket(EntityUid grid, [NotNullWhen(true)] out CargoMarketDataComponent? market)
    {
        var station = _station.GetOwningStation(grid);
        if (station != null && TryComp(station, out market))
            return true;

        var query = EntityQueryEnumerator<CargoMarketDataComponent>();
        return query.MoveNext(out _, out market);
    }
}
