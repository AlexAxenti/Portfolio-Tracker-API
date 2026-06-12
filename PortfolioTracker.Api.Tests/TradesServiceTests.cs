using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PortfolioTracker.Api.Auth;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.DTOs.Trades;
using PortfolioTracker.Api.Entities;
using PortfolioTracker.Api.Repositories;
using PortfolioTracker.Api.Services;

namespace PortfolioTracker.Api.Tests;

public sealed class TradesServiceTests
{
    [Fact]
    public async Task CreateTradeAsync_UpdatesHoldingAcrossBuyPartialSellAndFinalSell()
    {
        var databaseName = Guid.NewGuid().ToString();
        var databaseRoot = new InMemoryDatabaseRoot();
        var userId = Guid.NewGuid();
        var tradeDate = new DateTime(2026, 06, 12, 12, 0, 0, DateTimeKind.Utc);

        await using (var dbContext = CreateDbContext(databaseName, databaseRoot))
        {
            var tradesService = CreateTradesService(dbContext, userId);
            var buyTrade = await tradesService.CreateTradeAsync(new CreateTradeRequest(
                "OSCR",
                TradeType.Buy,
                10m,
                15m,
                tradeDate,
                Notes: null));

            Assert.NotEqual(Guid.Empty, buyTrade.Id);
        }

        await using (var dbContext = CreateDbContext(databaseName, databaseRoot))
        {
            var holdingAfterBuy = await dbContext.Holdings.AsNoTracking().SingleOrDefaultAsync(holding =>
                holding.UserId == userId && holding.Ticker == "OSCR");
            Assert.NotNull(holdingAfterBuy);
            Assert.Equal(10m, holdingAfterBuy.ShareCount);
            Assert.Equal(15m, holdingAfterBuy.AverageCost);
        }

        await using (var dbContext = CreateDbContext(databaseName, databaseRoot))
        {
            var tradesService = CreateTradesService(dbContext, userId);
            var partialSellTrade = await tradesService.CreateTradeAsync(new CreateTradeRequest(
                "OSCR",
                TradeType.Sell,
                5m,
                20m,
                tradeDate.AddDays(1),
                Notes: null));

            Assert.NotEqual(Guid.Empty, partialSellTrade.Id);
        }

        await using (var dbContext = CreateDbContext(databaseName, databaseRoot))
        {
            var holdingAfterPartialSell = await dbContext.Holdings.AsNoTracking().SingleOrDefaultAsync(holding =>
                holding.UserId == userId && holding.Ticker == "OSCR");
            Assert.NotNull(holdingAfterPartialSell);
            Assert.Equal(5m, holdingAfterPartialSell.ShareCount);
            Assert.Equal(15m, holdingAfterPartialSell.AverageCost);
        }

        await using (var dbContext = CreateDbContext(databaseName, databaseRoot))
        {
            var tradesService = CreateTradesService(dbContext, userId);
            var finalSellTrade = await tradesService.CreateTradeAsync(new CreateTradeRequest(
                "OSCR",
                TradeType.Sell,
                5m,
                20m,
                tradeDate.AddDays(2),
                Notes: null));

            Assert.NotEqual(Guid.Empty, finalSellTrade.Id);
        }

        await using (var dbContext = CreateDbContext(databaseName, databaseRoot))
        {
            var holdingAfterFinalSell = await dbContext.Holdings.AsNoTracking().SingleOrDefaultAsync(holding =>
                holding.UserId == userId && holding.Ticker == "OSCR");
            Assert.Null(holdingAfterFinalSell);
        }
    }

    private static AppDbContext CreateDbContext(string databaseName, InMemoryDatabaseRoot databaseRoot)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        return new AppDbContext(options);
    }

    private static TradesService CreateTradesService(AppDbContext dbContext, Guid userId)
    {
        var currentUserService = new TestCurrentUserService(userId);
        var holdingsRepository = new HoldingsRepository(dbContext);
        var tradesRepository = new TradesRepository(dbContext);
        var holdingsService = new HoldingsService(holdingsRepository, currentUserService);
        return new TradesService(tradesRepository, holdingsService, currentUserService);
    }

    private sealed class TestCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid UserId { get; } = userId;
    }
}
