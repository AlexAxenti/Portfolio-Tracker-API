using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using PortfolioTracker.Api.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Options;
using PortfolioTracker.Api.Configuration;
using PortfolioTracker.Api.DTOs.Messaging;
using PortfolioTracker.Api.Repositories;

namespace PortfolioTracker.Api.Workers;

public sealed class PriceRefreshWorker(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory serviceScopeFactory,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PriceRefreshWorker> logger) : BackgroundService
{
    private readonly RabbitMqOptions rabbitMqOptions = options.Value;

    private readonly TokenBucketRateLimiter perSecondLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 30,
        TokensPerPeriod = 30,
        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
        AutoReplenishment = true,
        QueueLimit = 1,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

    private readonly TokenBucketRateLimiter perMinuteLimiter = new(new TokenBucketRateLimiterOptions
    {
        TokenLimit = 60,
        TokensPerPeriod = 60,
        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
        AutoReplenishment = true,
        QueueLimit = 1,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
    });

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

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                PriceRefreshRequestedMessage? message;

                try
                {
                    message = JsonSerializer.Deserialize<PriceRefreshRequestedMessage>(json);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Received invalid price refresh message JSON.");
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                if (message is null ||
                    message.TickerId == Guid.Empty ||
                    string.IsNullOrWhiteSpace(message.Ticker))
                {
                    logger.LogWarning("Received invalid price refresh message.");
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                logger.LogInformation(
                    "Received price refresh request for user {UserId}, ticker {Ticker}.",
                    message.UserId,
                    message.Ticker);

                await ProcessMessageAsync(message, stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process price refresh message.");

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

    private async Task ProcessMessageAsync(
        PriceRefreshRequestedMessage message,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var tickersRepository = scope.ServiceProvider.GetRequiredService<ITickersRepository>();
        var ticker = await tickersRepository.GetByIdAsync(message.TickerId);

        if (ticker is null)
        {
            logger.LogWarning(
                "Skipping price refresh for missing ticker {TickerId} ({Ticker}).",
                message.TickerId,
                message.Ticker);
            return;
        }

        var now = DateTime.UtcNow;

        if (!PriceRefreshStaleness.IsStale(ticker.PriceLastUpdatedAt, now))
        {
            logger.LogInformation(
                "Skipping price refresh for {Ticker} because it was already refreshed.",
                ticker.Symbol);
            return;
        }

        await AcquireFinnhubRequestPermitAsync(cancellationToken);

        var apiKey = configuration["Finnhub:ApiKey"]
            ?? throw new InvalidOperationException("Finnhub API key is not configured.");
        var currentPrice = await GetCurrentPriceAsync(ticker.Symbol, apiKey, cancellationToken);
        now = DateTime.UtcNow;

        if (currentPrice is > 0)
        {
            ticker.CurrentPrice = DecimalHelpers.RoundToThreeDecimals(currentPrice.Value);
            ticker.PriceLastUpdatedAt = now;
            ticker.IsValid = true;
            ticker.LastPriceFetchFailedAt = null;
            ticker.LastPriceFetchError = null;
            ticker.ConsecutiveFailureCount = 0;
            ticker.UpdatedAt = now;

            await tickersRepository.SaveChangesAsync();
            return;
        }

        ticker.LastPriceFetchFailedAt = now;
        ticker.LastPriceFetchError = "Finnhub did not return a positive current price.";
        ticker.ConsecutiveFailureCount += 1;
        ticker.UpdatedAt = now;

        await tickersRepository.SaveChangesAsync();
    }

    private async Task AcquireFinnhubRequestPermitAsync(CancellationToken cancellationToken)
    {
        await AcquireFinnhubRequestPermitAsync(
            perSecondLimiter,
            "per-second",
            cancellationToken);

        await AcquireFinnhubRequestPermitAsync(
            perMinuteLimiter,
            "per-minute",
            cancellationToken);
    }

    private async Task AcquireFinnhubRequestPermitAsync(
        TokenBucketRateLimiter limiter,
        string limitName,
        CancellationToken cancellationToken)
    {
        using var immediateLease = limiter.AttemptAcquire(1);

        if (immediateLease.IsAcquired)
        {
            return;
        }

        var waitingMessage = $"Finnhub {limitName} rate limit exhausted. Price refresh worker is waiting for a permit.";
        logger.LogInformation("{WaitingMessage}", waitingMessage);

        using var queuedLease = await limiter.AcquireAsync(1, cancellationToken);

        if (!queuedLease.IsAcquired)
        {
            throw new InvalidOperationException($"Could not acquire Finnhub {limitName} rate limit permit.");
        }
    }

    private async Task<decimal?> GetCurrentPriceAsync(
        string ticker,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        var requestUrl =
            $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(ticker)}&token={Uri.EscapeDataString(apiKey)}";

        try
        {
            var quote = await httpClient.GetFromJsonAsync<FinnhubQuoteResponse>(
                requestUrl,
                cancellationToken);
            return quote?.CurrentPrice;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    public override void Dispose()
    {
        perSecondLimiter.Dispose();
        perMinuteLimiter.Dispose();
        base.Dispose();
    }

    private sealed record FinnhubQuoteResponse(
        [property: JsonPropertyName("c")] decimal? CurrentPrice);
}
