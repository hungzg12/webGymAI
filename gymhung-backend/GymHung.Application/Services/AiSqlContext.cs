using System.Security.Claims;
using System.Linq.Expressions;
using System.Text.Json;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymHung.Application;

// Only explicit business fields are sent to the local model. Never serialize entities/navigation properties.
public sealed class AiSqlContext(GymDbContext db, AccessService access, WorkspaceScope scope)
{
    public async Task<string> ReadAsync(ClaimsPrincipal principal, long memberId, string question, CancellationToken ct)
    {
        var tables = new Dictionary<string, object>();
        var selected = new HashSet<string>();
        void Select(string[] words, params string[] names) { if (words.Any(w => question.Contains(w, StringComparison.OrdinalIgnoreCase))) selected.UnionWith(names); }
        Select(["gói", "giá"], "Packages", "MemberPackages");
        Select(["lịch", "đặt", "buổi"], "WorkoutSchedules", "Bookings", "Trainers");
        Select(["PT", "huấn luyện", "trainer"], "Trainers", "MemberTrainers");
        Select(["hội viên", "thành viên", "member"], "Members");
        Select(["tiến độ", "cân", "mỡ", "chiều cao"], "Members", "Progress");
        Select(["giáo án", "kế hoạch"], "WorkoutPlans");
        Select(["check", "điểm danh"], "CheckIns");
        var personal = question.Contains("của tôi", StringComparison.OrdinalIgnoreCase) || selected.Count == 0;
        if (selected.Count == 0) selected.UnionWith(["Members", "MemberTrainers", "Trainers", "MemberPackages", "Packages"]);
        async Task Add<T>(string name, string fields) where T : class
        {
            if (!selected.Contains(name)) return;
            if (!await access.HasAsync(principal, AccessService.Module(name) + ".read", ct)) return;
            var lambda = await scope.QueryExpressionAsync(typeof(T), principal, ct);
            var predicate = Expression.Lambda<Func<T, bool>>(lambda.Body, (ParameterExpression)lambda.Parameters[0]);
            var query = db.Set<T>().AsNoTracking().Where(predicate);
            // Apply the requested date before taking a sample, so older rows cannot hide today's sessions.
            if (typeof(T) == typeof(WorkoutSchedule) && question.Contains("hôm nay", StringComparison.OrdinalIgnoreCase))
                query = query.Where(x => EF.Property<DateOnly>(x, "ScheduleDate") == GymClock.Today);
            if (personal && typeof(T).GetProperty("MemberId") != null)
                query = query.Where(x => EF.Property<long>(x, "MemberId") == memberId);
            var total = await query.CountAsync(ct);
            if (typeof(T).GetProperty("MemberId") != null)
                query = query.OrderByDescending(x => EF.Property<long>(x, "MemberId") == memberId);
            var visible = await query.Take(15).ToListAsync(ct);
            var properties = fields.Split(',').Select(f => typeof(T).GetProperty(f) ?? throw new InvalidOperationException($"Unknown AI field {name}.{f}")).ToArray();
            var memberProperty = typeof(T).GetProperty("MemberId");
            var rows = visible.OrderByDescending(x => memberProperty?.GetValue(x) is long id && id == memberId)
                .Take(15).Select(x => properties.ToDictionary(f => f.Name, f => f.GetValue(x))).ToList();
            tables[name] = new { totalAuthorizedRows = total, truncated = total > rows.Count, rows };
        }
        await Add<Member>("Members", "MemberId,UserId,Goal,FitnessLevel,JoinDate,Status");
        // Names are projected separately; credentials/contact information are never loaded into the prompt.
        var memberLambda = await scope.QueryExpressionAsync(typeof(Member), principal, ct);
        var memberPredicate = Expression.Lambda<Func<Member, bool>>(memberLambda.Body, (ParameterExpression)memberLambda.Parameters[0]);
        var members = db.Members.AsNoTracking().Where(memberPredicate).Select(x => x.UserId);
        if (selected.Contains("Members") && await access.HasAsync(principal, "members.read", ct))
            tables["MemberNames"] = await db.Users.AsNoTracking().Where(x => members.Contains(x.UserId))
                .Select(x => new { x.UserId, x.FullName }).Take(15).ToListAsync(ct);
        await Add<Trainer>("Trainers", "TrainerId,UserId,Specialization,ExperienceYears,Status");
        if (selected.Contains("Trainers") && await access.HasAsync(principal, "trainers.read", ct))
            tables["TrainerNames"] = await db.Trainers.AsNoTracking().Select(x => new { x.TrainerId, x.User.FullName }).Take(15).ToListAsync(ct);
        await Add<MemberTrainer>("MemberTrainers", "MemberId,TrainerId,Status");
        await Add<Package>("Packages", "PackageId,PackageName,DurationDays,TotalSessions,Price,Status");
        await Add<MemberPackage>("MemberPackages", "MemberId,PackageId,StartDate,EndDate,RemainingSessions,Price,Status");
        await Add<WorkoutSchedule>("WorkoutSchedules", "WorkoutScheduleId,TrainerId,Title,ScheduleDate,StartTime,EndTime,Location,Capacity,CurrentParticipants,Status");
        await Add<Booking>("Bookings", "BookingId,MemberId,WorkoutScheduleId,BookingTime,Status");
        await Add<WorkoutPlan>("WorkoutPlans", "PlanId,MemberId,TrainerId,PlanName,Goal,StartDate,EndDate,Status");
        await Add<Progress>("Progress", "MemberId,RecordDate,Weight,BodyFat,MuscleMass");
        await Add<CheckIn>("CheckIns", "MemberId,CheckInTime,CheckOutTime");
        return Serialize();
        string Serialize() => JsonSerializer.Serialize(new { source = "SQL Server Webgym", capturedAt = GymClock.Now, conversationMemberId = memberId, tables });
    }
}
