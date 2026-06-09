using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Repositories;

public interface ITradesRepository
{
    Task<IReadOnlyList<TradeEntity>> GetAllAsync(Guid userId);

    Task<TradeEntity?> GetByIdAsync(Guid id, Guid userId);

    Task AddAsync(TradeEntity trade);

    Task UpdateAsync(TradeEntity trade);

    Task DeleteAsync(TradeEntity trade);
}
