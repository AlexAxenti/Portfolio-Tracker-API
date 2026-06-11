using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class PricesService(
    IHoldingsRepository holdingsRepository,
    ICurrentUserService currentUserService,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IPricesService
{
    public async Task<IReadOnlyList<HoldingDto>> RefreshPricesAsync()
    {
        var apiKey = configuration["Finnhub:ApiKey"]
            ?? throw new InvalidOperationException("Finnhub API key is not configured.");

        var holdings = await holdingsRepository.GetAllForUpdateAsync(currentUserService.UserId);
        var tickers = holdings
            .Select(holding => holding.Ticker)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pricesByTicker = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticker in tickers)
        {
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

        var now = DateTime.UtcNow;

        foreach (var holding in holdings)
        {
            if (!pricesByTicker.TryGetValue(holding.Ticker, out var currentPrice))
            {
                continue;
            }

            holding.CurrentPrice = currentPrice;
            holding.PriceLastUpdatedAt = now;
            holding.UpdatedAt = now;
        }

        await holdingsRepository.SaveChangesAsync();
        return HoldingMapper.MapHoldings(holdings);
    }

    private async Task<decimal?> GetCurrentPriceAsync(string ticker, string apiKey)
    {
        var httpClient = httpClientFactory.CreateClient();
        var requestUrl =
            $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(ticker)}&token={Uri.EscapeDataString(apiKey)}";
        var quote = await httpClient.GetFromJsonAsync<FinnhubQuoteResponse>(requestUrl);

        return quote?.CurrentPrice;
    }

    private sealed record FinnhubQuoteResponse(
        [property: JsonPropertyName("c")] decimal? CurrentPrice);
}
