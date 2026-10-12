using GymManagement.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(GymDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var connected = await db.Database.CanConnectAsync(cancellationToken);
        if (!connected)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "degraded",
                database = "unavailable"
            });
        }

        var tables = db.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Where(name => name is not null).Distinct().Count();
        return Ok(new
        {
            status = "ok",
            database = "gymhung_demo",
            tables
        });
    }
}
