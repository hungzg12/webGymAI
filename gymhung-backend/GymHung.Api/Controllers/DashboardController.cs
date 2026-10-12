using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GymManagement.Api.Controllers;

[ApiController, Authorize(Policy = "dashboard.read")]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("api/dashboard")] public async Task<IActionResult> Dashboard(string? dateFrom, string? dateTo, CancellationToken ct) => Ok(await service.DashboardAsync(User, dateFrom, dateTo, ct));
    [HttpGet("api/reports/{type}")] public async Task<IActionResult> Report(string type, string? dateFrom, string? dateTo, CancellationToken ct) => Ok(await service.ReportAsync(User, type, dateFrom, dateTo, ct));
}

