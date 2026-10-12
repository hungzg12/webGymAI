using System.Security.Claims;
using GymManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
namespace GymHung.Application;

public sealed class AccessService(GymDbContext db)
{
    public static string Module(string table) => table.ToLowerInvariant() switch
    {
        "users" or "userroles" or "roles" or "permissions" or "rolepermissions" => "system",
        "members" or "emergencycontacts" or "membertrainers" => "members",
        "trainers" => "trainers",
        "staffs" => "staff",
        "packages" or "memberpackages" => "packages",
        "payments" or "paymenttransactions" => "payments",
        "rooms" or "trainerschedules" or "workoutschedules" => "schedules",
        "bookings" => "bookings",
        "attendance" or "checkins" => "checkins",
        "exercises" or "workoutplans" or "workoutplandetails" => "workouts",
        "workoutresults" => "results",
        "progress" => "progress",
        "equipmentcategories" or "equipment" or "maintenancerecords" => "equipment",
        "notifications" => "notifications",
        "aiconversations" or "aiconversationmessages" or "airecommendations" => "ai",
        "churnpredictions" => "churn",
        "auditlogs" => "audit",
        _ => "system"
    };
    public async Task<bool> HasAsync(ClaimsPrincipal p, string permission, CancellationToken ct)
    {
        if (!long.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return false;
        if (!await db.Users.AnyAsync(u => u.UserId == id && u.Status == "Active", ct))
            return false;
        if (await db.UserRoles.AnyAsync(x => x.UserId == id && x.Role.RoleName == "Admin", ct))
            return true;
        if (permission == "packages.catalog.write") return false;
        return await db.UserRoles.Where(x => x.UserId == id).SelectMany(x => x.Role.RolePermissions).AnyAsync(x => x.Permission.PermissionCode == permission, ct);
    }
    public async Task DemandAsync(ClaimsPrincipal p, string permission, CancellationToken ct)
    {
        if (!await HasAsync(p, permission, ct))
            throw new ApiException(403, "Bạn không có quyền thực hiện thao tác này.");
    }
}

