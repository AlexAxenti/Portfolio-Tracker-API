namespace PortfolioTracker.Api.DTOs.Holdings;

public sealed record HoldingDto(
    Guid Id,
    Guid UserId,
    Guid TickerId,
    string Ticker,
    string? CompanyName,
    decimal ShareCount,
    decimal AverageCost,
    decimal? CurrentPrice,
    DateTime? PriceLastUpdatedAt,
    string? Sector,
    IReadOnlyList<string> Categories,
    string? Notes,
    DateOnly? PurchaseDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal EffectiveCurrentPrice,
    decimal TotalCostInvested,
    decimal MarketValue,
    decimal UnrealizedPL,
    decimal UnrealizedPLPercent,
    decimal AllocationPercent);
