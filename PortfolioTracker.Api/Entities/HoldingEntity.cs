namespace PortfolioTracker.Api.Entities;

public sealed class HoldingEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TickerId { get; set; }

    public TickerEntity Ticker { get; set; } = null!;

    public string? CompanyName { get; set; }

    public decimal ShareCount { get; set; }

    public decimal AverageCost { get; set; }

    public string? Sector { get; set; }

    public List<string> Categories { get; set; } = [];

    public string? Notes { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
