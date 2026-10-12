using System.Security.Claims;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Api.Data;

// Scope is always derived from the authenticated account, never client IDs.
public sealed partial class WorkspaceScope(GymDbContext db)
{
    public async Task<Func<object, bool>> CreateAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return _ => false;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (user?.Status != "Active")
            return _ => false;
        var roles = await db.UserRoles.Where(x => x.UserId == userId).Select(x => x.Role.RoleName).ToListAsync(ct);
        if (roles.Contains("Admin"))
            return _ => true;
        var staff = roles.Contains("Staff");
        var trainer = await db.Trainers.Where(x => x.UserId == userId).Select(x => (long?)x.TrainerId).SingleOrDefaultAsync(ct);
        var members = await db.Members.Where(x => x.UserId == userId).Select(x => x.MemberId).ToListAsync(ct);
        if (roles.Contains("Trainer") && trainer is not null)
            members.AddRange(await db.MemberTrainers.Where(x => x.TrainerId == trainer && x.Status == "Active").Select(x => x.MemberId).ToListAsync(ct));
        var memberIds = members.ToHashSet();
        var userIds = (await db.Members.Where(x => memberIds.Contains(x.MemberId)).Select(x => x.UserId).ToListAsync(ct)).ToHashSet();
        userIds.Add(userId);
        var plans = (await db.WorkoutPlans.Where(x => memberIds.Contains(x.MemberId)).Select(x => x.PlanId).ToListAsync(ct)).ToHashSet();
        var bookings = (await db.Bookings.Where(x => memberIds.Contains(x.MemberId)).Select(x => x.BookingId).ToListAsync(ct)).ToHashSet();
        var conversations = (await db.AIConversations.Where(x => memberIds.Contains(x.MemberId)).Select(x => x.ConversationId).ToListAsync(ct)).ToHashSet();
        return row => row switch
        {
            AuditLog or Role or Permission or RolePermission => false,
            User u => staff || userIds.Contains(u.UserId),
            UserRole r => r.UserId == userId,
            Notification n => n.UserId == userId,
            Staff s => s.UserId == userId,
            Member m => staff || memberIds.Contains(m.MemberId),
            EmergencyContact c => staff || memberIds.Contains(c.MemberId),
            MemberTrainer mt => staff || memberIds.Contains(mt.MemberId),
            MemberPackage mp => staff || memberIds.Contains(mp.MemberId),
            Payment p => staff || memberIds.Contains(p.MemberId),
            PaymentTransaction => staff,
            Booking b => staff || memberIds.Contains(b.MemberId),
            Attendance a => staff || bookings.Contains(a.BookingId),
            CheckIn c => staff || memberIds.Contains(c.MemberId),
            WorkoutPlan p => memberIds.Contains(p.MemberId),
            WorkoutPlanDetail d => plans.Contains(d.PlanId),
            WorkoutResult r => memberIds.Contains(r.MemberId),
            Progress p => memberIds.Contains(p.MemberId),
            AIConversation c => memberIds.Contains(c.MemberId),
            AIConversationMessage m => conversations.Contains(m.ConversationId),
            AIRecommendation r => memberIds.Contains(r.MemberId),
            ChurnPrediction c => roles.Contains("Trainer") && memberIds.Contains(c.MemberId),
            TrainerSchedule s => staff || (trainer is not null && s.TrainerId == trainer),
            WorkoutSchedule s => staff || !roles.Contains("Trainer") || s.TrainerId == trainer,
            Trainer or Package or Room or Exercise => true,
            Equipment or EquipmentCategory or MaintenanceRecord => staff,
            _ => false
        };
    }
}


