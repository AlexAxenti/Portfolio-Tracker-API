namespace PortfolioTracker.Api.Entities;

public sealed class HoldingEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public decimal ShareCount { get; set; }

    public decimal AverageCost { get; set; }

    public decimal? CurrentPrice { get; set; }

    public DateTime? PriceLastUpdatedAt { get; set; }

    public string? Sector { get; set; }

    public List<string> Categories { get; set; } = [];

    public string? Notes { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
