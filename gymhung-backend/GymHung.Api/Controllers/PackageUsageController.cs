using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Api.Controllers;

[ApiController, Authorize, Route("api/package-usage")]
public sealed class PackageUsageController(PackageUsageService usage) : ControllerBase
{
    public sealed record AdmissionRequest(long MemberId, long? MemberPackageId);
    public sealed record AttendanceRequest(long BookingId, long? MemberPackageId);
    [HttpPost("check-in")]
    public async Task<IActionResult> CheckIn(AdmissionRequest request, CancellationToken ct) => Ok(await usage.CheckInAsync(User, request.MemberId, request.MemberPackageId, ct));
    [HttpPost("attendance")]
    public async Task<IActionResult> Attend(AttendanceRequest request, CancellationToken ct) => Ok(await usage.AttendAsync(User, request.BookingId, request.MemberPackageId, ct));
}
