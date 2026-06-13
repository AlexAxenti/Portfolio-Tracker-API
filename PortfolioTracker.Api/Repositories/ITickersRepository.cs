using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public interface ITickersRepository
{
    Task<TickerEntity?> GetByIdAsync(Guid id);

    Task<TickerEntity?> GetBySymbolAsync(string symbol);

    Task<IReadOnlyList<TickerEntity>> GetByIdsForUpdateAsync(IEnumerable<Guid> ids);

    Task AddAsync(TickerEntity ticker);

    Task SaveChangesAsync();
}
