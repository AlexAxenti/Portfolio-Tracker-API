using System.Text;
using Microsoft.Extensions.Options;
using PortfolioTracker.Api.Configuration;
using PortfolioTracker.Api.DTOs.Messaging;
using RabbitMQ.Client;

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

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: rabbitMqOptions.PriceRefreshQueueName,
            body: body,
            cancellationToken: cancellationToken);

        Console.WriteLine($"Published: {message}");
    }
}