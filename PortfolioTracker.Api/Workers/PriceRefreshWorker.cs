using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using PortfolioTracker.Api.Configuration;
using PortfolioTracker.Api.DTOs.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PortfolioTracker.Api.Workers;

public sealed class PriceRefreshWorker(IOptions<RabbitMqOptions> options) : BackgroundService
{
    private readonly RabbitMqOptions rabbitMqOptions = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: rabbitMqOptions.PriceRefreshQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var message = JsonSerializer.Deserialize<PriceRefreshRequestedMessage>(json);

                if (message is null)
                {
                    Console.WriteLine("Received invalid price refresh message.");
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                Console.WriteLine(
                    $"Received price refresh request for user {message.UserId}. " +
                    $"Tickers: {string.Join(", ", message.Tickers.Select(t => t.Symbol))}");

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process price refresh message: {ex.Message}");

                await channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: rabbitMqOptions.PriceRefreshQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}