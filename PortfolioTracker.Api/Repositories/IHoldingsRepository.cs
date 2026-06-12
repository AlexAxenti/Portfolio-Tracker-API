using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public interface IHoldingsRepository
{
    Task<IReadOnlyList<HoldingEntity>> GetAllAsync(Guid userId);

    Task<IReadOnlyList<HoldingEntity>> GetAllForUpdateAsync(Guid userId);

    Task<HoldingEntity?> GetByIdAsync(Guid id, Guid userId);

    Task<HoldingEntity?> GetByTickerAsync(string ticker, Guid userId);

    Task AddAsync(HoldingEntity holding);

    Task UpdateAsync(HoldingEntity holding);

    Task SaveChangesAsync();

    Task DeleteAsync(HoldingEntity holding);
}
