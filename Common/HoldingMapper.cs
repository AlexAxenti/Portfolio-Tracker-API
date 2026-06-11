using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Common;

public static class HoldingMapper
{
    public static IReadOnlyList<HoldingDto> MapHoldings(IReadOnlyList<HoldingEntity> holdings)
    {
        var totalPortfolioValue = CalculateTotalPortfolioValue(holdings);
        return holdings.Select(holding => MapHolding(holding, totalPortfolioValue)).ToList();
    }

    public static HoldingDto MapHolding(HoldingEntity holding, decimal totalPortfolioValue)
    {
        var effectiveCurrentPrice = holding.CurrentPrice ?? holding.AverageCost;
        var totalCostInvested = holding.ShareCount * holding.AverageCost;
        var marketValue = holding.ShareCount * effectiveCurrentPrice;
        var unrealizedPL = marketValue - totalCostInvested;
        var unrealizedPLPercent = totalCostInvested == 0 ? 0 : (unrealizedPL / totalCostInvested) * 100;
        var allocationPercent = totalPortfolioValue == 0 ? 0 : (marketValue / totalPortfolioValue) * 100;

        return new HoldingDto(
            holding.Id,
            holding.UserId,
            holding.Ticker,
            holding.CompanyName,
            holding.ShareCount,
            holding.AverageCost,
            holding.CurrentPrice,
            holding.PriceLastUpdatedAt,
            holding.Sector,
            holding.Categories,
            holding.Notes,
            holding.PurchaseDate,
            holding.CreatedAt,
            holding.UpdatedAt,
            effectiveCurrentPrice,
            totalCostInvested,
            marketValue,
            unrealizedPL,
            unrealizedPLPercent,
            allocationPercent);
    }

    public static decimal CalculateTotalPortfolioValue(IEnumerable<HoldingEntity> holdings)
    {
        return holdings.Sum(holding => holding.ShareCount * (holding.CurrentPrice ?? holding.AverageCost));
    }
}
