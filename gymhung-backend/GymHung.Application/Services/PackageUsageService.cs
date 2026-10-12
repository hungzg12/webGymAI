using System.Data;
using System.Security.Claims;
using System.Text.Json;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymHung.Application;

// Each admission/attendance is linked to its selected registration in the immutable audit ledger.
// No guess is made when multiple registrations qualify. Money is charged at purchase, never twice.
public sealed class PackageUsageService(GymDbContext db, AccessService access, WorkspaceScope scope)
{
    public static bool IsTrainerPackage(string name) => name.Contains("PERSONAL TRAINING", StringComparison.OrdinalIgnoreCase)
        || name.Contains("huấn luyện", StringComparison.OrdinalIgnoreCase) || name.Equals("PT", StringComparison.OrdinalIgnoreCase);
    public static int? AfterUse(int? remaining)
    {
        if (remaining is <= 0) throw new ApiException(409, "Gói đã hết buổi.");
        return remaining is null ? null : remaining - 1;
    }
    private async Task LockAsync(CancellationToken ct) => await db.Database.ExecuteSqlRawAsync(
        "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'gym-package-usage',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51000,'Resource busy',1;", ct);
    private async Task<MemberPackage> SelectAsync(ClaimsPrincipal p, long memberId, long? registrationId, bool trainer, DateOnly day, CancellationToken ct)
    {
        var member = await db.Members.FindAsync(new object[] { memberId }, ct) ?? throw new ApiException(404, "Không tìm thấy hội viên.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(member)) throw new ApiException(403, "Hội viên ngoài phạm vi tài khoản.");
        if (member.Status != "Active") throw new ApiException(409, "Hội viên đang ngừng hoạt động.");
        var candidates = await db.MemberPackages.Include(x => x.Package).Where(x => x.MemberId == memberId
            && x.Status == "Active" && x.StartDate <= day && (x.EndDate == null || x.EndDate >= day)
            && (x.RemainingSessions == null || x.RemainingSessions > 0)
            && x.Payments.Any(pay => pay.Status == "Completed" && pay.Amount == x.Price)).ToListAsync(ct);
        candidates = candidates.Where(x => IsTrainerPackage(x.Package.PackageName) == trainer).ToList();
        if (registrationId is not null) candidates = candidates.Where(x => x.MemberPackageId == registrationId).ToList();
        if (candidates.Count == 0) throw new ApiException(409, trainer ? "Cần gói PT đã thanh toán, còn hạn và còn buổi." : "Cần gói vào phòng đã thanh toán, còn hạn và còn buổi.");
        if (candidates.Count > 1) throw new ApiException(409, "Hội viên có nhiều gói hợp lệ. Chọn gói đăng ký để sử dụng.");
        return candidates.Single();
    }
    private void Consume(ClaimsPrincipal p, MemberPackage registration, string eventTable, long eventId)
    {
        var before = registration.RemainingSessions;
        registration.RemainingSessions = AfterUse(before);
        registration.UpdatedAt = GymClock.Now;
        if (registration.RemainingSessions == 0) registration.Status = "Expired";
        db.AuditLogs.Add(new AuditLog { UserId = long.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Action = "UsePackage", EntityName = eventTable, EntityId = eventId, CreatedAt = GymClock.Now,
            OldData = JsonSerializer.Serialize(new { remainingSessions = before }),
            NewData = JsonSerializer.Serialize(new { memberPackageId = registration.MemberPackageId,
                memberId = registration.MemberId, packageId = registration.PackageId,
                remainingSessions = registration.RemainingSessions, chargedAgain = false }) });
    }
    public async Task<object> CheckInAsync(ClaimsPrincipal p, long memberId, long? registrationId, CancellationToken ct)
    {
        await access.DemandAsync(p, "checkins.write", ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(ct);
        // The visit itself provides idempotence: retries cannot debit another registration.
        var existing = await db.CheckIns.SingleOrDefaultAsync(x => x.MemberId == memberId && x.CheckOutTime == null, ct);
        if (existing is not null)
        {
            var allowed = await scope.CreateAsync(p, ct);
            if (!allowed(existing)) throw new ApiException(403, "Lượt vào ngoài phạm vi tài khoản.");
            throw new ApiException(409, "Hội viên chưa check-out; không trừ thêm buổi.");
        }
        var registration = await SelectAsync(p, memberId, registrationId, false, GymClock.Today, ct);
        var row = new CheckIn { MemberId = memberId, CheckInTime = GymClock.Now, Status = "CheckedIn",
            Method = "Manual", SourceDevice = "Quầy lễ tân", CreatedAt = GymClock.Now };
        db.CheckIns.Add(row); await db.SaveChangesAsync(ct);
        Consume(p, registration, "CheckIns", row.CheckInId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return ResourceDto.From(row, db.Model.FindEntityType(typeof(CheckIn))!);
    }
    public async Task<object> AttendAsync(ClaimsPrincipal p, long bookingId, long? registrationId, CancellationToken ct)
    {
        await access.DemandAsync(p, "checkins.write", ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(ct);
        var booking = await db.Bookings.Include(x => x.WorkoutSchedule).SingleOrDefaultAsync(x => x.BookingId == bookingId, ct)
            ?? throw new ApiException(404, "Không tìm thấy đặt lịch.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(booking)) throw new ApiException(403, "Đặt lịch ngoài phạm vi tài khoản.");
        if (booking.Status != "Confirmed" || booking.WorkoutSchedule.Status == "Canceled") throw new ApiException(409, "Lịch không còn hiệu lực.");
        if (booking.WorkoutSchedule.ScheduleDate != GymClock.Today) throw new ApiException(409, "Chỉ điểm danh PT vào ngày của buổi tập.");
        if (await db.Attendances.AnyAsync(x => x.BookingId == bookingId, ct)) throw new ApiException(409, "Buổi đã được điểm danh; không trừ thêm buổi.");
        var registration = await SelectAsync(p, booking.MemberId, registrationId, true, GymClock.Today, ct);
        if (!await db.MemberTrainers.AnyAsync(x => x.MemberId == booking.MemberId && x.TrainerId == booking.WorkoutSchedule.TrainerId
            && x.Status == "Active" && (x.StartDate == null || x.StartDate <= GymClock.Today) && (x.EndDate == null || x.EndDate >= GymClock.Today), ct))
            throw new ApiException(409, "PT chưa được phân công cho hội viên.");
        if (p.IsInRole("Trainer") && !p.IsInRole("Admin") && !p.IsInRole("Staff")
            && !await db.Trainers.AnyAsync(x => x.TrainerId == booking.WorkoutSchedule.TrainerId && x.UserId == long.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!), ct))
            throw new ApiException(403, "Chỉ được điểm danh buổi do mình phụ trách.");
        var row = new Attendance { BookingId = bookingId, Status = "Present", CheckInTime = GymClock.Now, CreatedAt = GymClock.Now };
        db.Attendances.Add(row); await db.SaveChangesAsync(ct);
        Consume(p, registration, "Attendance", row.AttendanceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return ResourceDto.From(row, db.Model.FindEntityType(typeof(Attendance))!);
    }
}
