namespace PortfolioTracker.Api.DTOs.Holdings;

public sealed record CreateHoldingRequest(
    string Ticker,
    string? CompanyName,
    decimal ShareCount,
    decimal AverageCost,
    string? Sector,
    List<string>? Categories,
    string? Notes,
    DateOnly? PurchaseDate);
