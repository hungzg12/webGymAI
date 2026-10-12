using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GymManagement.Api.Controllers;

[ApiController, Authorize(Policy = "payments.write"), Route("api/payments")]
public sealed class PaymentsController(PaymentService service, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("{id:long}/confirm-cash")] public async Task<IActionResult> Cash(long id, PaymentConfirmationRequest request, CancellationToken ct) => Ok(await service.ConfirmAsync(User, id, request, true, ct));
    [HttpPost("{id:long}/demo-confirm")]
    public async Task<IActionResult> Demo(long id, PaymentConfirmationRequest request, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();
        return Ok(await service.ConfirmAsync(User, id, request, false, ct));
    }
}

