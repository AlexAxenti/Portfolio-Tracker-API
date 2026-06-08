using System.Text.Json;
using System.Text.Json.Serialization;
using PortfolioTracker.Api.Data;
using PortfolioTracker.Api.Repositories;
using PortfolioTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    });
builder.Services.AddOpenApi();

builder.Services.AddSingleton<AppDbContext>();
builder.Services.AddScoped<IHoldingsRepository, HoldingsRepository>();
builder.Services.AddScoped<ITradesRepository, TradesRepository>();
builder.Services.AddScoped<IHoldingsService, HoldingsService>();
builder.Services.AddScoped<ITradesService, TradesService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
