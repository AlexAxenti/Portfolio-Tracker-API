using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.Api.DTOs.Trades;
using PortfolioTracker.Api.Services;

namespace PortfolioTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class TradesController(ITradesService tradesService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TradeDto>>> GetTrades()
    {
        var trades = await tradesService.GetTradesAsync();
        return Ok(trades);
    }

    [HttpPost]
    public async Task<ActionResult<TradeDto>> CreateTrade(CreateTradeRequest request)
    {
        try
        {
            var trade = await tradesService.CreateTradeAsync(request);
            return Created($"/api/trades/{trade.Id}", trade);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TradeDto>> UpdateTrade(Guid id, UpdateTradeRequest request)
    {
        try
        {
            var trade = await tradesService.UpdateTradeAsync(id, request);
            return trade is null ? NotFound() : Ok(trade);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTrade(Guid id)
    {
        try
        {
            var deleted = await tradesService.DeleteTradeAsync(id);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
