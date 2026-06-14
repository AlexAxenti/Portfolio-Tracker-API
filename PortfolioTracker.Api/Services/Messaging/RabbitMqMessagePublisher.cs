using System.Text;
using Microsoft.Extensions.Options;
using PortfolioTracker.Api.Configuration;
using PortfolioTracker.Api.DTOs.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace PortfolioTracker.Api.Services.Messaging;

public sealed class RabbitMqMessagePublisher(IOptions<RabbitMqOptions> options) : IMessagePublisher
{
    private readonly RabbitMqOptions rabbitMqOptions = options.Value;

    public async Task PublishPriceRefreshRequestedAsync(PriceRefreshRequestedMessage message, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password
        };

        try
        {
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: rabbitMqOptions.PriceRefreshQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var json = System.Text.Json.JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: rabbitMqOptions.PriceRefreshQueueName,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (BrokerUnreachableException ex)
        {
            throw new MessageBrokerUnavailableException(
                "RabbitMQ is unavailable. Price refresh messages could not be queued.",
                ex);
        }
    }
}
