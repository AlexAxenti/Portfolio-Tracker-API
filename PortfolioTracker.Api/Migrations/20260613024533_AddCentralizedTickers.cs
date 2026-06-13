using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCentralizedTickers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tickers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    PriceLastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    LastPriceFetchFailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPriceFetchError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickers_Symbol",
                table: "Tickers",
                column: "Symbol",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TickerId",
                table: "Trades",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TickerId",
                table: "Holdings",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Tickers" (
                    "Id",
                    "Symbol",
                    "CurrentPrice",
                    "PriceLastUpdatedAt",
                    "IsValid",
                    "LastPriceFetchFailedAt",
                    "LastPriceFetchError",
                    "ConsecutiveFailureCount",
                    "CreatedAt",
                    "UpdatedAt")
                WITH symbols AS (
                    SELECT DISTINCT upper(btrim("Ticker")) AS "Symbol"
                    FROM "Holdings"
                    WHERE btrim("Ticker") <> ''
                    UNION
                    SELECT DISTINCT upper(btrim("Ticker")) AS "Symbol"
                    FROM "Trades"
                    WHERE btrim("Ticker") <> ''
                ),
                latest_prices AS (
                    SELECT DISTINCT ON (upper(btrim("Ticker")))
                        upper(btrim("Ticker")) AS "Symbol",
                        "CurrentPrice",
                        "PriceLastUpdatedAt"
                    FROM "Holdings"
                    WHERE btrim("Ticker") <> ''
                    ORDER BY
                        upper(btrim("Ticker")),
                        "PriceLastUpdatedAt" DESC NULLS LAST,
                        "UpdatedAt" DESC
                )
                SELECT
                    gen_random_uuid(),
                    symbols."Symbol",
                    latest_prices."CurrentPrice",
                    latest_prices."PriceLastUpdatedAt",
                    TRUE,
                    NULL,
                    NULL,
                    0,
                    now(),
                    now()
                FROM symbols
                LEFT JOIN latest_prices ON latest_prices."Symbol" = symbols."Symbol"
                ON CONFLICT ("Symbol") DO NOTHING;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Holdings"
                SET "TickerId" = "Tickers"."Id"
                FROM "Tickers"
                WHERE upper(btrim("Holdings"."Ticker")) = "Tickers"."Symbol";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Trades"
                SET "TickerId" = "Tickers"."Id"
                FROM "Tickers"
                WHERE upper(btrim("Trades"."Ticker")) = "Tickers"."Symbol";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TickerId",
                table: "Trades",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TickerId",
                table: "Holdings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Holdings_UserId_Ticker",
                table: "Holdings");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_TickerId",
                table: "Trades",
                column: "TickerId");

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_TickerId",
                table: "Holdings",
                column: "TickerId");

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_UserId_TickerId",
                table: "Holdings",
                columns: new[] { "UserId", "TickerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Holdings_Tickers_TickerId",
                table: "Holdings",
                column: "TickerId",
                principalTable: "Tickers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trades_Tickers_TickerId",
                table: "Trades",
                column: "TickerId",
                principalTable: "Tickers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "Holdings");

            migrationBuilder.DropColumn(
                name: "PriceLastUpdatedAt",
                table: "Holdings");

            migrationBuilder.DropColumn(
                name: "Ticker",
                table: "Holdings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holdings_Tickers_TickerId",
                table: "Holdings");

            migrationBuilder.DropForeignKey(
                name: "FK_Trades_Tickers_TickerId",
                table: "Trades");

            migrationBuilder.DropTable(
                name: "Tickers");

            migrationBuilder.DropIndex(
                name: "IX_Trades_TickerId",
                table: "Trades");

            migrationBuilder.DropIndex(
                name: "IX_Holdings_TickerId",
                table: "Holdings");

            migrationBuilder.DropIndex(
                name: "IX_Holdings_UserId_TickerId",
                table: "Holdings");

            migrationBuilder.DropColumn(
                name: "TickerId",
                table: "Trades");

            migrationBuilder.DropColumn(
                name: "TickerId",
                table: "Holdings");

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "Trades",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "Holdings",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PriceLastUpdatedAt",
                table: "Holdings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ticker",
                table: "Holdings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_UserId_Ticker",
                table: "Holdings",
                columns: new[] { "UserId", "Ticker" },
                unique: true);
        }
    }
}
