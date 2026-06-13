using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.DTOs.Trades;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class TradesService(
    ITradesRepository tradesRepository,
    IHoldingsTradeService holdingsTradeService,
    ITickersService tickersService,
    ICurrentUserService currentUserService) : ITradesService
{
    public async Task<IReadOnlyList<TradeDto>> GetTradesAsync()
    {
        var trades = await tradesRepository.GetAllAsync(currentUserService.UserId);
        return trades.Select(MapTrade).ToList();
    }

    public async Task<TradeDto> CreateTradeAsync(CreateTradeRequest request)
    {
        ValidateTrade(request.Ticker, request.Type, request.Quantity, request.Price);
        var ticker = await GetOrCreateTickerForTradeAsync(request.Ticker, request.Price);

        var trade = new TradeEntity
        {
            Id = Guid.NewGuid(),
            UserId = currentUserService.UserId,
            TickerId = ticker.Id,
            Ticker = ticker,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = DecimalHelpers.RoundToThreeDecimals(request.Price),
            TradeDate = DateTimeHelpers.NormalizeUtc(request.TradeDate),
            Notes = CleanOptionalText(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        await holdingsTradeService.ApplyTradeAsync(trade);
        await tradesRepository.AddAsync(trade);

        return MapTrade(trade);
    }

    public async Task<TradeDto?> UpdateTradeAsync(Guid id, UpdateTradeRequest request)
    {
        ValidateTrade(request.Ticker, request.Type, request.Quantity, request.Price);

        var existingTrade = await tradesRepository.GetByIdAsync(id, currentUserService.UserId);

        if (existingTrade is null)
        {
            return null;
        }

        var ticker = await GetOrCreateTickerForTradeAsync(request.Ticker, request.Price);

        var updatedTrade = new TradeEntity
        {
            Id = existingTrade.Id,
            UserId = existingTrade.UserId,
            TickerId = ticker.Id,
            Ticker = ticker,
            Type = request.Type,
            Quantity = request.Quantity,
            Price = DecimalHelpers.RoundToThreeDecimals(request.Price),
            TradeDate = DateTimeHelpers.NormalizeUtc(request.TradeDate),
            Notes = CleanOptionalText(request.Notes),
            CreatedAt = existingTrade.CreatedAt
        };

        await holdingsTradeService.ReverseTradeAsync(existingTrade);

        try
        {
            await holdingsTradeService.ApplyTradeAsync(updatedTrade);
        }
        catch
        {
            await holdingsTradeService.ApplyTradeAsync(existingTrade);
            throw;
        }

        existingTrade.TickerId = updatedTrade.TickerId;
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
        var existingTrade = await tradesRepository.GetByIdAsync(id, currentUserService.UserId);

        if (existingTrade is null)
        {
            return false;
        }

        await holdingsTradeService.ReverseTradeAsync(existingTrade);
        await tradesRepository.DeleteAsync(existingTrade);
        return true;
    }

    private static TradeDto MapTrade(TradeEntity trade)
    {
        return new TradeDto(
            trade.Id,
            trade.UserId,
            trade.TickerId,
            trade.Ticker.Symbol,
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

    private static string? CleanOptionalText(string? value)
    {
        var trimmedValue = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmedValue) ? null : trimmedValue;
    }

    private async Task<TickerEntity> GetOrCreateTickerForTradeAsync(string symbol, decimal price)
    {
        return await tickersService.GetTickerAsync(symbol)
            ?? await tickersService.CreateTickerAsync(symbol, price);
    }
}
