using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Data;

public sealed class AppDbContext
{
    public List<HoldingEntity> Holdings { get; } = [];

    public List<TradeEntity> Trades { get; } = [];

    public Dictionary<Guid, HoldingEntity> SellHoldingSnapshots { get; } = [];
}
