using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class HoldingsService(
    IHoldingsRepository holdingsRepository,
    ICurrentUserService currentUserService) : IHoldingsService
{
    public async Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync()
    {
        var holdings = await holdingsRepository.GetAllAsync(currentUserService.UserId);
        return HoldingMapper.MapHoldings(holdings);
    }

    public async Task<HoldingDto?> GetHoldingAsync(Guid id)
    {
        var holding = await holdingsRepository.GetByIdAsync(id, currentUserService.UserId);

        if (holding is null)
        {
            return null;
        }

        var holdings = await holdingsRepository.GetAllAsync(currentUserService.UserId);
        var totalPortfolioValue = HoldingMapper.CalculateTotalPortfolioValue(holdings);
        return HoldingMapper.MapHolding(holding, totalPortfolioValue);
    }

    public async Task<HoldingDto> CreateHoldingAsync(CreateHoldingRequest request)
    {
        ValidateHolding(request.Ticker, request.ShareCount, request.AverageCost);

        var ticker = NormalizeTicker(request.Ticker);
        await EnsureTickerIsAvailableAsync(ticker);

        var now = DateTime.UtcNow;
        var holding = new HoldingEntity
        {
            Id = Guid.NewGuid(),
            UserId = currentUserService.UserId,
            Ticker = ticker,
            CompanyName = CleanOptionalText(request.CompanyName),
            ShareCount = request.ShareCount,
            AverageCost = DecimalHelpers.RoundToThreeDecimals(request.AverageCost),
            CurrentPrice = request.CurrentPrice is null ? null : DecimalHelpers.RoundToThreeDecimals(request.CurrentPrice.Value),
            PriceLastUpdatedAt = NormalizeUtc(request.PriceLastUpdatedAt),
            Sector = CleanOptionalText(request.Sector),
            Categories = NormalizeCategories(request.Categories),
            Notes = CleanOptionalText(request.Notes),
            PurchaseDate = request.PurchaseDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        await holdingsRepository.AddAsync(holding);
        var holdings = await holdingsRepository.GetAllAsync(currentUserService.UserId);
        return HoldingMapper.MapHolding(holding, HoldingMapper.CalculateTotalPortfolioValue(holdings));
    }

    public async Task<HoldingDto?> UpdateHoldingAsync(Guid id, UpdateHoldingRequest request)
    {
        ValidateHolding(request.Ticker, request.ShareCount, request.AverageCost);

        var holding = await holdingsRepository.GetByIdAsync(id, currentUserService.UserId);

        if (holding is null)
        {
            return null;
        }

        var ticker = NormalizeTicker(request.Ticker);
        await EnsureTickerIsAvailableAsync(ticker, id);

        holding.Ticker = ticker;
        holding.CompanyName = CleanOptionalText(request.CompanyName);
        holding.ShareCount = request.ShareCount;
        holding.AverageCost = DecimalHelpers.RoundToThreeDecimals(request.AverageCost);
        holding.CurrentPrice = request.CurrentPrice is null ? null : DecimalHelpers.RoundToThreeDecimals(request.CurrentPrice.Value);
        holding.PriceLastUpdatedAt = NormalizeUtc(request.PriceLastUpdatedAt);
        holding.Sector = CleanOptionalText(request.Sector);
        holding.Categories = NormalizeCategories(request.Categories);
        holding.Notes = CleanOptionalText(request.Notes);
        holding.PurchaseDate = request.PurchaseDate;
        holding.UpdatedAt = DateTime.UtcNow;

        await holdingsRepository.UpdateAsync(holding);
        var holdings = await holdingsRepository.GetAllAsync(currentUserService.UserId);
        return HoldingMapper.MapHolding(holding, HoldingMapper.CalculateTotalPortfolioValue(holdings));
    }

    public async Task<bool> DeleteHoldingAsync(Guid id)
    {
        var holding = await holdingsRepository.GetByIdAsync(id, currentUserService.UserId);

        if (holding is null)
        {
            return false;
        }

        await holdingsRepository.DeleteAsync(holding);
        return true;
    }

    public async Task ApplyTradeAsync(TradeEntity trade)
    {
        if (trade.Type == TradeType.Buy)
        {
            await ApplyBuyTradeAsync(trade);
            return;
        }

        await ApplySellTradeAsync(trade);
    }

    public async Task ReverseTradeAsync(TradeEntity trade)
    {
        if (trade.Type == TradeType.Buy)
        {
            await ReverseBuyTradeAsync(trade);
            return;
        }

        await ReverseSellTradeAsync(trade);
    }

    private async Task ApplyBuyTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerAsync(trade.Ticker, currentUserService.UserId);
        var now = DateTime.UtcNow;

        if (holding is null)
        {
            await holdingsRepository.AddAsync(new HoldingEntity
            {
                Id = Guid.NewGuid(),
                UserId = currentUserService.UserId,
                Ticker = trade.Ticker,
                ShareCount = trade.Quantity,
                AverageCost = DecimalHelpers.RoundToThreeDecimals(trade.Price),
                CurrentPrice = DecimalHelpers.RoundToThreeDecimals(trade.Price),
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
        holding.CurrentPrice ??= DecimalHelpers.RoundToThreeDecimals(trade.Price);
        holding.UpdatedAt = now;

        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task ApplySellTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerAsync(trade.Ticker, currentUserService.UserId);

        if (holding is null)
        {
            throw new InvalidOperationException($"You do not own {trade.Ticker}.");
        }

        if (trade.Quantity > holding.ShareCount)
        {
            throw new InvalidOperationException($"You only own {holding.ShareCount} shares of {trade.Ticker}.");
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
        var holding = await holdingsRepository.GetByTickerAsync(trade.Ticker, currentUserService.UserId);

        if (holding is null || holding.ShareCount < trade.Quantity)
        {
            throw new InvalidOperationException($"Cannot safely reverse the {trade.Ticker} buy trade.");
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
            throw new InvalidOperationException($"Cannot safely recalculate {trade.Ticker} average cost.");
        }

        holding.ShareCount = remainingShares;
        holding.AverageCost = DecimalHelpers.RoundToThreeDecimals(remainingCost / remainingShares);
        holding.UpdatedAt = DateTime.UtcNow;
        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task ReverseSellTradeAsync(TradeEntity trade)
    {
        var holding = await holdingsRepository.GetByTickerAsync(trade.Ticker, currentUserService.UserId);
        var now = DateTime.UtcNow;

        if (holding is null)
        {
            throw new InvalidOperationException($"Cannot safely reverse the {trade.Ticker} sell trade.");
        }

        holding.ShareCount += trade.Quantity;
        holding.UpdatedAt = now;
        await holdingsRepository.UpdateAsync(holding);
    }

    private async Task EnsureTickerIsAvailableAsync(string ticker, Guid? ignoredHoldingId = null)
    {
        var duplicate = await holdingsRepository.GetByTickerAsync(ticker, currentUserService.UserId);

        if (duplicate is not null && duplicate.Id != ignoredHoldingId)
        {
            throw new InvalidOperationException($"You already own {ticker}.");
        }
    }

    private static void ValidateHolding(string ticker, decimal shareCount, decimal averageCost)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new InvalidOperationException("Ticker is required.");
        }

        if (shareCount <= 0)
        {
            throw new InvalidOperationException("Share count must be greater than zero.");
        }

        if (averageCost <= 0)
        {
            throw new InvalidOperationException("Average cost must be greater than zero.");
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

    private static List<string> NormalizeCategories(List<string>? categories)
    {
        return categories?
            .Select(CleanOptionalText)
            .Where(category => category is not null)
            .Select(category => category!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        return value is null ? null : NormalizeUtc(value.Value);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
