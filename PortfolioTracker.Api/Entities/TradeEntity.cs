namespace PortfolioTracker.Api.Entities;

public sealed class TradeEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public TradeType Type { get; set; }

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public DateTime TradeDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
