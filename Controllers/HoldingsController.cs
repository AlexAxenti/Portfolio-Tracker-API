using Microsoft.AspNetCore.Mvc;
using PortfolioTracker.Api.DTOs.Holdings;
using PortfolioTracker.Api.Services;

namespace PortfolioTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HoldingsController(IHoldingsService holdingsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HoldingDto>>> GetHoldings()
    {
        var holdings = await holdingsService.GetHoldingsAsync();
        return Ok(holdings);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HoldingDto>> GetHolding(Guid id)
    {
        var holding = await holdingsService.GetHoldingAsync(id);
        return holding is null ? NotFound() : Ok(holding);
    }

    [HttpPost]
    public async Task<ActionResult<HoldingDto>> CreateHolding(CreateHoldingRequest request)
    {
        try
        {
            var holding = await holdingsService.CreateHoldingAsync(request);
            return CreatedAtAction(nameof(GetHolding), new { id = holding.Id }, holding);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<HoldingDto>> UpdateHolding(Guid id, UpdateHoldingRequest request)
    {
        try
        {
            var holding = await holdingsService.UpdateHoldingAsync(id, request);
            return holding is null ? NotFound() : Ok(holding);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteHolding(Guid id)
    {
        var deleted = await holdingsService.DeleteHoldingAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
