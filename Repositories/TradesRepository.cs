using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public sealed class TradesRepository(AppDbContext dbContext) : ITradesRepository
{
    public Task<IReadOnlyList<TradeEntity>> GetAllAsync(Guid userId)
    {
        var trades = dbContext.Trades
            .Where(trade => trade.UserId == userId)
            .OrderByDescending(trade => trade.TradeDate)
            .ThenByDescending(trade => trade.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<TradeEntity>>(trades);
    }

    public Task<TradeEntity?> GetByIdAsync(Guid id, Guid userId)
    {
        var trade = dbContext.Trades.FirstOrDefault(trade =>
            trade.Id == id && trade.UserId == userId);

        return Task.FromResult(trade);
    }

    public Task AddAsync(TradeEntity trade)
    {
        dbContext.Trades.Add(trade);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(TradeEntity trade)
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TradeEntity trade)
    {
        dbContext.Trades.Remove(trade);
        return Task.CompletedTask;
    }
}
