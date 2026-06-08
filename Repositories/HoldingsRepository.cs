using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public sealed class HoldingsRepository(AppDbContext dbContext) : IHoldingsRepository
{
    public Task<IReadOnlyList<HoldingEntity>> GetAllAsync(Guid userId)
    {
        var holdings = dbContext.Holdings
            .Where(holding => holding.UserId == userId)
            .OrderBy(holding => holding.Ticker)
            .ToList();

        return Task.FromResult<IReadOnlyList<HoldingEntity>>(holdings);
    }

    public Task<HoldingEntity?> GetByIdAsync(Guid id, Guid userId)
    {
        var holding = dbContext.Holdings.FirstOrDefault(holding =>
            holding.Id == id && holding.UserId == userId);

        return Task.FromResult(holding);
    }

    public Task<HoldingEntity?> GetByTickerAsync(string ticker, Guid userId)
    {
        var holding = dbContext.Holdings.FirstOrDefault(holding =>
            holding.Ticker == ticker && holding.UserId == userId);

        return Task.FromResult(holding);
    }

    public Task AddAsync(HoldingEntity holding)
    {
        dbContext.Holdings.Add(holding);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(HoldingEntity holding)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(HoldingEntity holding)
    {
        dbContext.Holdings.Remove(holding);
        return Task.CompletedTask;
    }

    public Task<HoldingEntity?> GetSellSnapshotAsync(Guid tradeId)
    {
        dbContext.SellHoldingSnapshots.TryGetValue(tradeId, out var holding);
        return Task.FromResult(holding is null ? null : Clone(holding));
    }

    public Task StoreSellSnapshotAsync(Guid tradeId, HoldingEntity holding)
    {
        dbContext.SellHoldingSnapshots[tradeId] = Clone(holding);
        return Task.CompletedTask;
    }

    public Task DeleteSellSnapshotAsync(Guid tradeId)
    {
        dbContext.SellHoldingSnapshots.Remove(tradeId);
        return Task.CompletedTask;
    }

    private static HoldingEntity Clone(HoldingEntity holding)
    {
        return new HoldingEntity
        {
            Id = holding.Id,
            UserId = holding.UserId,
            Ticker = holding.Ticker,
            CompanyName = holding.CompanyName,
            ShareCount = holding.ShareCount,
            AverageCost = holding.AverageCost,
            CurrentPrice = holding.CurrentPrice,
            PriceLastUpdatedAt = holding.PriceLastUpdatedAt,
            Sector = holding.Sector,
            Categories = [.. holding.Categories],
            Notes = holding.Notes,
            PurchaseDate = holding.PurchaseDate,
            CreatedAt = holding.CreatedAt,
            UpdatedAt = holding.UpdatedAt
        };
    }
}
