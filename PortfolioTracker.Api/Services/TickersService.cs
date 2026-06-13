using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class TickersService(ITickersRepository tickersRepository) : ITickersService
{
    public async Task<TickerEntity?> GetTickerAsync(Guid id)
    {
        return await tickersRepository.GetByIdAsync(id);
    }

    public async Task<TickerEntity?> GetTickerAsync(string symbol)
    {
        return await tickersRepository.GetBySymbolAsync(NormalizeSymbol(symbol));
    }

    public async Task<TickerEntity> CreateTickerAsync(string symbol, decimal? initialCurrentPrice = null)
    {
        var now = DateTime.UtcNow;
        var ticker = new TickerEntity
        {
            Id = Guid.NewGuid(),
            Symbol = NormalizeSymbol(symbol),
            CurrentPrice = initialCurrentPrice is null ? null : DecimalHelpers.RoundToThreeDecimals(initialCurrentPrice.Value),
            IsValid = true,
            ConsecutiveFailureCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await tickersRepository.AddAsync(ticker);
        return ticker;
    }

    public async Task<TickerEntity> GetOrCreateTickerAsync(string symbol)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var ticker = await tickersRepository.GetBySymbolAsync(normalizedSymbol);

        if (ticker is not null)
        {
            return ticker;
        }

        return await CreateTickerAsync(normalizedSymbol);
    }

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidOperationException("Ticker is required.");
        }

        return symbol.Trim().ToUpperInvariant();
    }
}
