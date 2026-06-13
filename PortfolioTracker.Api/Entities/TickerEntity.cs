namespace PortfolioTracker.Api.Entities;

public sealed class TickerEntity
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal? CurrentPrice { get; set; }

    public DateTime? PriceLastUpdatedAt { get; set; }

    public bool IsValid { get; set; } = true;

    public DateTime? LastPriceFetchFailedAt { get; set; }

    public string? LastPriceFetchError { get; set; }

    public int ConsecutiveFailureCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<HoldingEntity> Holdings { get; set; } = [];

    public List<TradeEntity> Trades { get; set; } = [];
}
