using System.Security.Claims;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace GymHung.Application;

public sealed class DashboardService(GymDbContext db, WorkspaceScope scope, AccessService access)
{
    private static (DateOnly, DateOnly) Range(string? from, string? to)
    {
        var start = from == null ? GymClock.Today.AddDays(-29) : DateOnly.TryParse(from, out var a) ? a : throw new ApiException(400, "Ngày bắt đầu không hợp lệ.");
        var end = to == null ? GymClock.Today : DateOnly.TryParse(to, out var b) ? b : throw new ApiException(400, "Ngày kết thúc không hợp lệ.");
        if (end < start || end.DayNumber - start.DayNumber > 3660)
            throw new ApiException(400, "Khoảng ngày không hợp lệ.");
        return (start, end);
    }
    public async Task<object> DashboardAsync(ClaimsPrincipal p, string? from, string? to, CancellationToken ct)
    {
        await access.DemandAsync(p, "dashboard.read", ct);
        var (start, end) = Range(from, to);
        var allowed = await scope.CreateAsync(p, ct);
        var members = (await db.Members.AsNoTracking().ToListAsync(ct)).Where(x => allowed(x)).ToList();
        var packages = (await db.MemberPackages.AsNoTracking().Include(x => x.Package).ToListAsync(ct)).Where(x => allowed(x)).ToList();
        var payments = (await db.Payments.AsNoTracking().Where(x => x.Status == "Completed" && x.PaidAt >= start.ToDateTime(TimeOnly.MinValue) && x.PaidAt < end.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToListAsync(ct)).Where(x => allowed(x)).ToList();
        var checks = (await db.CheckIns.AsNoTracking().Where(x => x.CheckInTime >= start.ToDateTime(TimeOnly.MinValue) && x.CheckInTime < end.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToListAsync(ct)).Where(x => allowed(x)).ToList();
        var schedules = (await db.WorkoutSchedules.AsNoTracking().Where(x => x.ScheduleDate == GymClock.Today && x.Status != "Canceled").ToListAsync(ct)).Where(x => allowed(x)).ToList();
        var churn = (await db.ChurnPredictions.AsNoTracking().ToListAsync(ct)).Where(x => allowed(x)).GroupBy(x => x.MemberId).Select(g => g.OrderByDescending(x => x.PredictionDate).ThenByDescending(x => x.ChurnPredictionId).First()).ToList();
        return new
        {
            totals = new
            {
                members = members.Count,
                activeMembers = members.Count(x => x.Status == "Active"),
                newMembers = members.Count(x => x.JoinDate >= start && x.JoinDate <= end),
                revenue = payments.Sum(x => x.Amount),
                periodCheckins = checks.Count,
                todaySchedules = schedules.Count,
                expiringPackages = packages.Count(x => x.Status == "Active" && x.EndDate >= GymClock.Today && x.EndDate <= GymClock.Today.AddDays(7)),
                highRiskMembers = churn.Count(x => x.RiskLevel == "High"),
                activePackages = packages.Count(x => x.Status == "Active"),
                bookings = (await db.Bookings.AsNoTracking().ToListAsync(ct)).Count(allowed),
                checkins = checks.Count
            },
            revenue = payments.GroupBy(x => DateOnly.FromDateTime(x.PaidAt!.Value)).OrderBy(x => x.Key).Select(g => new { date = g.Key, amount = g.Sum(x => x.Amount) }),
            checkins = checks.GroupBy(x => DateOnly.FromDateTime(x.CheckInTime)).OrderBy(g => g.Key).Select(g => new { date = g.Key, count = g.Count() }),
            packages = packages.GroupBy(x => x.Package.PackageName).Select(g => new { name = g.Key, count = g.Count() }),
            members = members.Where(x => x.JoinDate >= start && x.JoinDate <= end).GroupBy(x => x.JoinDate).Select(g => new { date = g.Key, count = g.Count() }),
            recentPayments = payments.OrderByDescending(x => x.PaidAt).Take(8).Select(x => ResourceDto.From(x, db.Model.FindEntityType(typeof(Payment))!))
        };
    }
    public async Task<object> ReportAsync(ClaimsPrincipal p, string type, string? from, string? to, CancellationToken ct)
    {
        await access.DemandAsync(p, "dashboard.read", ct);
        var (start, end) = Range(from, to);
        var allowed = await scope.CreateAsync(p, ct);
        IEnumerable<(string label, decimal value)> items;
        switch (type)
        {
            case "revenue":
                await access.DemandAsync(p, "payments.read", ct);
                items = (await db.Payments.AsNoTracking().Where(x => x.Status == "Completed" && x.PaidAt >= start.ToDateTime(TimeOnly.MinValue) && x.PaidAt < end.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToListAsync(ct)).Where(x => allowed(x)).GroupBy(x => DateOnly.FromDateTime(x.PaidAt!.Value)).OrderBy(g => g.Key).Select(g => (g.Key.ToString("yyyy-MM-dd"), g.Sum(x => x.Amount)));
                break;
            case "members":
                items = (await db.Members.AsNoTracking().Where(x => x.JoinDate >= start && x.JoinDate <= end).ToListAsync(ct)).Where(x => allowed(x)).GroupBy(x => x.JoinDate).Select(g => (g.Key.ToString("yyyy-MM-dd"), (decimal)g.Count()));
                break;
            case "packages":
                await access.DemandAsync(p, "packages.read", ct);
                items = (await db.MemberPackages.AsNoTracking().Include(x => x.Package).Where(x => x.StartDate >= start && x.StartDate <= end).ToListAsync(ct)).Where(x => allowed(x)).GroupBy(x => x.Package.PackageName).Select(g => (g.Key, g.Sum(x => x.Price)));
                break;
            case "checkins":
                await access.DemandAsync(p, "checkins.read", ct);
                items = (await db.CheckIns.AsNoTracking().Where(x => x.CheckInTime >= start.ToDateTime(TimeOnly.MinValue) && x.CheckInTime < end.AddDays(1).ToDateTime(TimeOnly.MinValue)).ToListAsync(ct)).Where(x => allowed(x)).GroupBy(x => DateOnly.FromDateTime(x.CheckInTime)).Select(g => (g.Key.ToString("yyyy-MM-dd"), (decimal)g.Count()));
                break;
            case "maintenance":
                await access.DemandAsync(p, "equipment.read", ct);
                items = (await db.MaintenanceRecords.AsNoTracking().Where(x => x.MaintenanceDate >= start && x.MaintenanceDate <= end).ToListAsync(ct)).Where(x => allowed(x)).GroupBy(x => x.MaintenanceDate).Select(g => (g.Key.ToString("yyyy-MM-dd"), g.Sum(x => x.Cost ?? 0)));
                break;
            default:
                throw new ApiException(404, "Báo cáo không tồn tại.");
        }
        var rows = items.ToList();
        return new
        {
            items = rows.Select(x => new { x.label, x.value }),
            total = rows.Sum(x => x.value)
        };
    }
}

