using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class PricesService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IServiceScopeFactory serviceScopeFactory) : IPricesService, IDisposable
{
    private static readonly TimeSpan PriceRefreshTtl = TimeSpan.FromMinutes(15);

    private readonly TokenBucketRateLimiter perSecondLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 30,
        TokensPerPeriod = 30,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        AutoReplenishment = true,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

    private readonly TokenBucketRateLimiter perMinuteLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 60,
        TokensPerPeriod = 60,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
        AutoReplenishment = true,
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

    public async Task<IReadOnlyList<HoldingDto>> RefreshPricesAsync()
    {
        var apiKey = configuration["Finnhub:ApiKey"]
            ?? throw new InvalidOperationException("Finnhub API key is not configured.");

        using var scope = serviceScopeFactory.CreateScope();
        var holdingsRepository = scope.ServiceProvider.GetRequiredService<IHoldingsRepository>();
        var currentUserService = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
        var holdings = await holdingsRepository.GetAllForUpdateAsync(currentUserService.UserId);
        var now = DateTime.UtcNow;
        var staleCutoff = now.Subtract(PriceRefreshTtl);
        var staleTickers = holdings
            .Where(holding => IsStale(holding, staleCutoff))
            .Select(holding => holding.Ticker)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pricesByTicker = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticker in staleTickers)
        {
            if (!TryAcquireFinnhubRequestPermit())
            {
                continue;
            }

            var currentPrice = await GetCurrentPriceAsync(ticker, apiKey);

            if (currentPrice is > 0)
            {
                pricesByTicker[ticker] = DecimalHelpers.RoundToThreeDecimals(currentPrice.Value);
            }
        }

        if (pricesByTicker.Count == 0)
        {
            return HoldingMapper.MapHoldings(holdings);
        }

        var hasUpdatedHoldings = false;

        foreach (var holding in holdings)
        {
            if (!pricesByTicker.TryGetValue(holding.Ticker, out var currentPrice))
            {
                continue;
            }

            holding.CurrentPrice = currentPrice;
            holding.PriceLastUpdatedAt = now;
            holding.UpdatedAt = now;
            hasUpdatedHoldings = true;
        }

        if (hasUpdatedHoldings)
        {
            await holdingsRepository.SaveChangesAsync();
        }

        return HoldingMapper.MapHoldings(holdings);
    }

    private bool TryAcquireFinnhubRequestPermit()
    {
        var perSecondLease = perSecondLimiter.AttemptAcquire(1);

        if (!perSecondLease.IsAcquired)
        {
            perSecondLease.Dispose();
            return false;
        }

        var perMinuteLease = perMinuteLimiter.AttemptAcquire(1);

        if (!perMinuteLease.IsAcquired)
        {
            perMinuteLease.Dispose();
            return false;
        }

        return true;
    }

    private async Task<decimal?> GetCurrentPriceAsync(string ticker, string apiKey)
    {
        var httpClient = httpClientFactory.CreateClient();
        var requestUrl =
            $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(ticker)}&token={Uri.EscapeDataString(apiKey)}";

        try
        {
            var quote = await httpClient.GetFromJsonAsync<FinnhubQuoteResponse>(requestUrl);
            return quote?.CurrentPrice;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsStale(HoldingEntity holding, DateTime staleCutoff)
    {
        return holding.PriceLastUpdatedAt is null || holding.PriceLastUpdatedAt.Value < staleCutoff;
    }

    public void Dispose()
    {
        perSecondLimiter.Dispose();
        perMinuteLimiter.Dispose();
    }

    private sealed record FinnhubQuoteResponse(
        [property: JsonPropertyName("c")] decimal? CurrentPrice);
}
