using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class HoldingsTradeService(
    IHoldingsRepository holdingsRepository,
    ITickersService tickersService,
    ICurrentUserService currentUserService) : IHoldingsTradeService
{
    public async Task ApplyTradeAsync(TradeEntity trade)
    {
        await EnsureTradeTickerLoadedAsync(trade);

        if (trade.Type == TradeType.Buy)
        {
            await ApplyBuyTradeAsync(trade);
            return;
        }

        await ApplySellTradeAsync(trade);
    }

    public async Task ReverseTradeAsync(TradeEntity trade)
    {
        await EnsureTradeTickerLoadedAsync(trade);

        if (trade.Type == TradeType.Buy)
        {
            await ReverseBuyTradeAsync(trade);
            return;
        }

        await ReverseSellTradeAsync(trade);
    }

    private async Task ApplyBuyTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerIdAsync(trade.TickerId, currentUserService.UserId);
        var now = DateTime.UtcNow;

        if (holding is null)
        {
            await holdingsRepository.AddAsync(new HoldingEntity
            {
                Id = Guid.NewGuid(),
                UserId = currentUserService.UserId,
                TickerId = trade.TickerId,
                Ticker = trade.Ticker,
                ShareCount = trade.Quantity,
                AverageCost = DecimalHelpers.RoundToThreeDecimals(trade.Price),
                PurchaseDate = DateOnly.FromDateTime(trade.TradeDate),
                CreatedAt = now,
                UpdatedAt = now
            });
            return;
        }

        var totalShares = holding.ShareCount + trade.Quantity;
        holding.AverageCost = DecimalHelpers.RoundToThreeDecimals(
            ((holding.ShareCount * holding.AverageCost) + (trade.Quantity * trade.Price)) / totalShares);
        holding.ShareCount = totalShares;
        holding.UpdatedAt = now;

        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task ApplySellTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerIdAsync(trade.TickerId, currentUserService.UserId);

        if (holding is null)
        {
            throw new InvalidOperationException($"You do not own {trade.Ticker.Symbol}.");
        }

        if (trade.Quantity > holding.ShareCount)
        {
            throw new InvalidOperationException($"You only own {holding.ShareCount} shares of {trade.Ticker.Symbol}.");
        }

        var remainingShares = holding.ShareCount - trade.Quantity;

        if (remainingShares == 0)
        {
            await holdingsRepository.DeleteAsync(holding);
            return;
        }

        holding.ShareCount = remainingShares;
        holding.UpdatedAt = DateTime.UtcNow;
        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task ReverseBuyTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerIdAsync(trade.TickerId, currentUserService.UserId);

        if (holding is null || holding.ShareCount < trade.Quantity)
        {
            throw new InvalidOperationException($"Cannot safely reverse the {trade.Ticker.Symbol} buy trade.");
        }

        var remainingShares = holding.ShareCount - trade.Quantity;

        if (remainingShares == 0)
        {
            await holdingsRepository.DeleteAsync(holding);
            return;
        }

        var remainingCost = (holding.ShareCount * holding.AverageCost) - (trade.Quantity * trade.Price);

        if (remainingCost < 0)
        {
            throw new InvalidOperationException($"Cannot safely recalculate {trade.Ticker.Symbol} average cost.");
        }

        holding.ShareCount = remainingShares;
        holding.AverageCost = DecimalHelpers.RoundToThreeDecimals(remainingCost / remainingShares);
        holding.UpdatedAt = DateTime.UtcNow;
        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task ReverseSellTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerIdAsync(trade.TickerId, currentUserService.UserId);
        var now = DateTime.UtcNow;

        if (holding is null)
        {
            throw new InvalidOperationException($"Cannot safely reverse the {trade.Ticker.Symbol} sell trade.");
        }

        holding.ShareCount += trade.Quantity;
        holding.UpdatedAt = now;
        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task EnsureTradeTickerLoadedAsync(TradeEntity trade)
    {
        var ticker = await tickersService.GetTickerAsync(trade.TickerId);

        if (ticker is null)
        {
            throw new InvalidOperationException("Ticker could not be found.");
        }

        trade.Ticker = ticker;
    }
}
