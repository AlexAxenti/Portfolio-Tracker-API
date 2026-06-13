using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public sealed class TickersRepository(AppDbContext dbContext) : ITickersRepository
{
    public async Task<TickerEntity?> GetByIdAsync(Guid id)
    {
        return await dbContext.Tickers
            .FirstOrDefaultAsync(ticker => ticker.Id == id);
    }

    public async Task<TickerEntity?> GetBySymbolAsync(string symbol)
    {
        return await dbContext.Tickers
            .FirstOrDefaultAsync(ticker => ticker.Symbol == symbol);
    }

    public async Task<IReadOnlyList<TickerEntity>> GetByIdsForUpdateAsync(IEnumerable<Guid> ids)
    {
        var tickerIds = ids.Distinct().ToList();

        return await dbContext.Tickers
            .Where(ticker => tickerIds.Contains(ticker.Id))
            .ToListAsync();
    }

    public async Task AddAsync(TickerEntity ticker)
    {
        await dbContext.Tickers.AddAsync(ticker);
        await dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
