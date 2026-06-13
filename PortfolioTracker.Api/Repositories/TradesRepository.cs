using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public sealed class TradesRepository(AppDbContext dbContext) : ITradesRepository
{
    public async Task<IReadOnlyList<TradeEntity>> GetAllAsync(Guid userId)
    {
        return await dbContext.Trades
            .AsNoTracking()
            .Include(trade => trade.Ticker)
            .Where(trade => trade.UserId == userId)
            .OrderByDescending(trade => trade.TradeDate)
            .ThenByDescending(trade => trade.CreatedAt)
            .ToListAsync();
    }

    public async Task<TradeEntity?> GetByIdAsync(Guid id, Guid userId)
    {
        return await dbContext.Trades
            .AsNoTracking()
            .Include(trade => trade.Ticker)
            .FirstOrDefaultAsync(trade => trade.Id == id && trade.UserId == userId);
    }

    public async Task AddAsync(TradeEntity trade)
    {
        await dbContext.Trades.AddAsync(trade);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(TradeEntity trade)
    {
        dbContext.Trades.Update(trade);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(TradeEntity trade)
    {
        dbContext.Trades.Remove(trade);
        await dbContext.SaveChangesAsync();
    }
}
