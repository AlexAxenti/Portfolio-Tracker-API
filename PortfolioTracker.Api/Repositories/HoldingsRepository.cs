using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public sealed class HoldingsRepository(AppDbContext dbContext) : IHoldingsRepository
{
    public async Task<IReadOnlyList<HoldingEntity>> GetAllAsync(Guid userId)
    {
        return await dbContext.Holdings
            .AsNoTracking()
            .Include(holding => holding.Ticker)
            .Where(holding => holding.UserId == userId)
            .OrderBy(holding => holding.Ticker.Symbol)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<HoldingEntity>> GetAllForUpdateAsync(Guid userId)
    {
        return await dbContext.Holdings
            .Include(holding => holding.Ticker)
            .Where(holding => holding.UserId == userId)
            .OrderBy(holding => holding.Ticker.Symbol)
            .ToListAsync();
    }

    public async Task<HoldingEntity?> GetByIdAsync(Guid id, Guid userId)
    {
        return await dbContext.Holdings
            .AsNoTracking()
            .Include(holding => holding.Ticker)
            .FirstOrDefaultAsync(holding => holding.Id == id && holding.UserId == userId);
    }

    public async Task<HoldingEntity?> GetByTickerIdAsync(Guid tickerId, Guid userId)
    {
        return await dbContext.Holdings
            .Include(holding => holding.Ticker)
            .FirstOrDefaultAsync(holding => holding.TickerId == tickerId && holding.UserId == userId);
    }

    public async Task AddAsync(HoldingEntity holding)
    {
        await dbContext.Holdings.AddAsync(holding);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(HoldingEntity holding)
    {
        dbContext.Holdings.Update(holding);
        await dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(HoldingEntity holding)
    {
        dbContext.Holdings.Remove(holding);
        await dbContext.SaveChangesAsync();
    }
}
