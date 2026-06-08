using PortfolioTracker.Api.DTOs.Trades;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class TradesService(
    ITradesRepository tradesRepository,
    IHoldingsRepository holdingsRepository,
    IHoldingsService holdingsService) : ITradesService
{
    private static readonly Guid MockUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    public async Task<IReadOnlyList<TradeDto>> GetTradesAsync()
    {
        var trades = await tradesRepository.GetAllAsync(MockUserId);
        return trades.Select(MapTrade).ToList();
    }

    public async Task<TradeDto> CreateTradeAsync(CreateTradeRequest request)
    {
        ValidateTrade(request.Ticker, request.Type, request.Quantity, request.Price);

        var trade = new TradeEntity
        {
            Id = Guid.NewGuid(),
            UserId = MockUserId,
            Ticker = NormalizeTicker(request.Ticker),
            Type = request.Type,
            Quantity = request.Quantity,
            Price = RoundToThreeDecimals(request.Price),
            TradeDate = request.TradeDate,
            Notes = CleanOptionalText(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        await holdingsService.ApplyTradeAsync(trade);
        await tradesRepository.AddAsync(trade);

        return MapTrade(trade);
    }

    public async Task<TradeDto?> UpdateTradeAsync(Guid id, UpdateTradeRequest request)
    {
        ValidateTrade(request.Ticker, request.Type, request.Quantity, request.Price);

        var existingTrade = await tradesRepository.GetByIdAsync(id, MockUserId);

        if (existingTrade is null)
        {
            return null;
        }

        var updatedTrade = new TradeEntity
        {
            Id = existingTrade.Id,
            UserId = existingTrade.UserId,
            Ticker = NormalizeTicker(request.Ticker),
            Type = request.Type,
            Quantity = request.Quantity,
            Price = RoundToThreeDecimals(request.Price),
            TradeDate = request.TradeDate,
            Notes = CleanOptionalText(request.Notes),
            CreatedAt = existingTrade.CreatedAt
        };

        await holdingsService.ReverseTradeAsync(existingTrade);
        await holdingsRepository.DeleteSellSnapshotAsync(existingTrade.Id);

        try
        {
            await holdingsService.ApplyTradeAsync(updatedTrade);
        }
        catch
        {
            await holdingsService.ApplyTradeAsync(existingTrade);
            throw;
        }

        existingTrade.Ticker = updatedTrade.Ticker;
        existingTrade.Type = updatedTrade.Type;
        existingTrade.Quantity = updatedTrade.Quantity;
        existingTrade.Price = updatedTrade.Price;
        existingTrade.TradeDate = updatedTrade.TradeDate;
        existingTrade.Notes = updatedTrade.Notes;

        await tradesRepository.UpdateAsync(existingTrade);
        return MapTrade(existingTrade);
    }

    public async Task<bool> DeleteTradeAsync(Guid id)
    {
        var existingTrade = await tradesRepository.GetByIdAsync(id, MockUserId);

        if (existingTrade is null)
        {
            return false;
        }

        await holdingsService.ReverseTradeAsync(existingTrade);
        await holdingsRepository.DeleteSellSnapshotAsync(existingTrade.Id);
        await tradesRepository.DeleteAsync(existingTrade);
        return true;
    }

    private static TradeDto MapTrade(TradeEntity trade)
    {
        return new TradeDto(
            trade.Id,
            trade.UserId,
            trade.Ticker,
            trade.Type,
            trade.Quantity,
            trade.Price,
            trade.TradeDate,
            trade.Notes,
            trade.CreatedAt);
    }

    private static void ValidateTrade(string ticker, TradeType type, decimal quantity, decimal price)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new InvalidOperationException("Ticker is required.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new InvalidOperationException("Trade type must be buy or sell.");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        if (price <= 0)
        {
            throw new InvalidOperationException("Price must be greater than zero.");
        }
    }

    private static string NormalizeTicker(string ticker)
    {
        return ticker.Trim().ToUpperInvariant();
    }

    private static string? CleanOptionalText(string? value)
    {
        var trimmedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmedValue) ? null : trimmedValue;
    }

    private static decimal RoundToThreeDecimals(decimal value)
    {
        return Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
