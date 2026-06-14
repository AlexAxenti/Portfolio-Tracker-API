using PortfolioTracker.Api.DTOs.Messaging;

namespace PortfolioTracker.Api.Services.Messaging;

public interface IMessagePublisher
{
    Task PublishPriceRefreshRequestedAsync(
        PriceRefreshRequestedMessage message,
        CancellationToken cancellationToken = default);
}