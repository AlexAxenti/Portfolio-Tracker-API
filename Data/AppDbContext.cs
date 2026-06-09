using Microsoft.EntityFrameworkCore;
using PortfolioTracker.Api.Entities;

namespace PortfolioTracker.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<HoldingEntity> Holdings => Set<HoldingEntity>();

    public DbSet<TradeEntity> Trades => Set<TradeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HoldingEntity>(entity =>
        {
            entity.HasKey(holding => holding.Id);

            entity.HasIndex(holding => new { holding.UserId, holding.Ticker })
                .IsUnique();

            entity.Property(holding => holding.Ticker)
                .HasMaxLength(16);

            entity.Property(holding => holding.CompanyName)
                .HasMaxLength(200);

            entity.Property(holding => holding.ShareCount)
                .HasPrecision(18, 6);

            entity.Property(holding => holding.AverageCost)
                .HasPrecision(18, 3);

            entity.Property(holding => holding.CurrentPrice)
                .HasPrecision(18, 3);

            entity.Property(holding => holding.PriceLastUpdatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(holding => holding.Sector)
                .HasMaxLength(100);

            entity.Property(holding => holding.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(holding => holding.UpdatedAt)
                .HasColumnType("timestamp with time zone");
        });

        modelBuilder.Entity<TradeEntity>(entity =>
        {
            entity.HasKey(trade => trade.Id);

            entity.HasIndex(trade => new { trade.UserId, trade.TradeDate });

            entity.Property(trade => trade.Ticker)
                .HasMaxLength(16);

            entity.Property(trade => trade.Quantity)
                .HasPrecision(18, 6);

            entity.Property(trade => trade.Price)
                .HasPrecision(18, 3);

            entity.Property(trade => trade.TradeDate)
                .HasColumnType("timestamp with time zone");

            entity.Property(trade => trade.CreatedAt)
                .HasColumnType("timestamp with time zone");
        });
    }
}
