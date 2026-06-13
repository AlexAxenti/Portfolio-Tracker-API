using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Common;
using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Services;

public sealed class HoldingsService(
    IHoldingsRepository holdingsRepository,
    ITickersService tickersService,
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

        var ticker = await tickersService.GetOrCreateTickerAsync(request.Ticker);
        await EnsureTickerIsAvailableAsync(ticker);
        ApplyTickerPriceFromRequest(ticker, request.CurrentPrice, request.PriceLastUpdatedAt);

        var now = DateTime.UtcNow;
        var holding = new HoldingEntity
        {
            Id = Guid.NewGuid(),
            UserId = currentUserService.UserId,
            TickerId = ticker.Id,
            Ticker = ticker,
            CompanyName = CleanOptionalText(request.CompanyName),
            ShareCount = request.ShareCount,
            AverageCost = DecimalHelpers.RoundToThreeDecimals(request.AverageCost),
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

        var ticker = await tickersService.GetOrCreateTickerAsync(request.Ticker);
        await EnsureTickerIsAvailableAsync(ticker, id);
        ApplyTickerPriceFromRequest(ticker, request.CurrentPrice, request.PriceLastUpdatedAt);

        holding.TickerId = ticker.Id;
        holding.Ticker = ticker;
        holding.CompanyName = CleanOptionalText(request.CompanyName);
        holding.ShareCount = request.ShareCount;
        holding.AverageCost = DecimalHelpers.RoundToThreeDecimals(request.AverageCost);
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

    private async Task EnsureTickerIsAvailableAsync(TickerEntity ticker, Guid? ignoredHoldingId = null)
    {
        var duplicate = await holdingsRepository.GetByTickerIdAsync(ticker.Id, currentUserService.UserId);

        if (duplicate is not null && duplicate.Id != ignoredHoldingId)
        {
            throw new InvalidOperationException($"You already own {ticker.Symbol}.");
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

    private static void ApplyTickerPriceFromRequest(TickerEntity ticker, decimal? currentPrice, DateTime? priceLastUpdatedAt)
    {
        if (currentPrice is null && priceLastUpdatedAt is null)
        {
            return;
        }

        ticker.CurrentPrice = currentPrice is null ? null : DecimalHelpers.RoundToThreeDecimals(currentPrice.Value);
        ticker.PriceLastUpdatedAt = DateTimeHelpers.NormalizeUtcOrNull(priceLastUpdatedAt);
        ticker.UpdatedAt = DateTime.UtcNow;
    }
}
