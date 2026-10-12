using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GymManagement.Api.Controllers;

[ApiController, Authorize, Route("api/workspace")]
public sealed class WorkspaceController(ResourceQueryService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await service.WorkspaceAsync(User, ct));
}
[ApiController, Authorize, Route("api/resources")]
public sealed class ResourcesController(ResourceQueryService query, WorkspaceService mutations) : ControllerBase
{
    [HttpGet("{table}")][HttpGet("/api/{table}")] public async Task<IActionResult> List(string table, int page = 1, int pageSize = 20, string? search = null, string? sort = null, string? direction = null, string? status = null, string? memberId = null, string? from = null, string? to = null, string? dateFrom = null, string? dateTo = null, CancellationToken ct = default) => Ok(await query.ListAsync(User, table, page, pageSize, search, sort, direction, status, memberId, dateFrom ?? from, dateTo ?? to, Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()), ct));
    [HttpPost("{table}")][HttpPost("/api/{table}")] public async Task<IActionResult> Create(string table, MutationRequest request, CancellationToken ct) => Ok(await mutations.SaveAsync(User, table, null, request, ct));
    [HttpPut("{table}/{id}")][HttpPut("/api/{table}/{id}")] public async Task<IActionResult> Update(string table, string id, MutationRequest request, CancellationToken ct) => Ok(await mutations.SaveAsync(User, table, id, request, ct));
    [HttpDelete("{table}/{id}")]
    [HttpDelete("/api/{table}/{id}")]
    public async Task<IActionResult> Delete(string table, string id, CancellationToken ct)
    {
        await mutations.RemoveAsync(User, table, id, ct);
        return NoContent();
    }
}

