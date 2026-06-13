namespace PortfolioTracker.Api.Configuration;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = string.Empty;
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string PriceRefreshQueueName { get; init; } = string.Empty;
}