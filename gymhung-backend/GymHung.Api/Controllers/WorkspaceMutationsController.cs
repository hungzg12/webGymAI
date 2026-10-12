using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GymManagement.Api.Controllers;

[ApiController, Authorize, Route("api/workspace")]
public sealed class WorkspaceMutationsController(WorkspaceService service) : ControllerBase
{
    [HttpPut("{table}/{id?}")] public async Task<IActionResult> Save(string table, string? id, [FromBody] MutationRequest request, CancellationToken ct) => Ok(await service.SaveAsync(User, table, id, request, ct));
    [HttpDelete("{table}/{id}")]
    public async Task<IActionResult> Remove(string table, string id, CancellationToken ct)
    {
        await service.RemoveAsync(User, table, id, ct);
        return NoContent();
    }
}
