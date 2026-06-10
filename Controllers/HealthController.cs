using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioTracker.Api.Data;

namespace PortfolioTracker.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "portfolio-tracker-api"
        });
    }

    [HttpGet("ready")]
    [EnableRateLimiting("health")]
    public async Task<IActionResult> GetReadiness()
    {
        var databaseHealthy = await dbContext.Database.CanConnectAsync(HttpContext.RequestAborted);

        if (!databaseHealthy)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "portfolio-tracker-api",
                checks = new
                {
                    database = "Unhealthy"
                }
            });
        }

        return Ok(new
        {
            status = "Healthy",
            service = "portfolio-tracker-api",
            checks = new
            {
                database = "Healthy"
            }
        });
    }
}
