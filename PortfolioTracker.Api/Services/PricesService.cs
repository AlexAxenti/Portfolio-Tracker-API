using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.DTOs.Messaging;
using PortfolioTracker.Api.DTOs.Prices;
using PortfolioTracker.Api.Repositories;
using PortfolioTracker.Api.Services.Messaging;

namespace PortfolioTracker.Api.Services;

public sealed class PricesService(
    IServiceScopeFactory serviceScopeFactory,
    IMessagePublisher messagePublisher) : IPricesService
{
    public async Task<PriceRefreshQueuedResponse> RefreshPricesAsync()
    {
        using var scope = serviceScopeFactory.CreateScope();
        var holdingsRepository = scope.ServiceProvider.GetRequiredService<IHoldingsRepository>();
        var tickersRepository = scope.ServiceProvider.GetRequiredService<ITickersRepository>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var holdings = await holdingsRepository.GetAllForUpdateAsync(currentUserService.UserId);
        var now = DateTime.UtcNow;
        var tickerIds = holdings
            .Select(holding => holding.TickerId)
            .Distinct()
            .ToList();
        var tickers = await tickersRepository.GetByIdsForUpdateAsync(tickerIds);
        var staleTickers = tickers
            .Where(ticker => PriceRefreshStaleness.IsStale(ticker.PriceLastUpdatedAt, now))
            .ToList();

        foreach (var ticker in staleTickers)
        {
            await messagePublisher.PublishPriceRefreshRequestedAsync(new PriceRefreshRequestedMessage(
                UserId: currentUserService.UserId,
                TickerId: ticker.Id,
                Ticker: ticker.Symbol,
                RequestedAtUtc: now));
        }

        return new PriceRefreshQueuedResponse(
            Message: staleTickers.Count == 0
                ? "No stale prices needed to be queued."
                : "Price refresh queued.",
            QueuedTickers: staleTickers.Select(ticker => ticker.Symbol).ToList(),
            QueuedAtUtc: now);
    }
}
