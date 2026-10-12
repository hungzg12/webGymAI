using System.Linq.Expressions;
using System.Security.Claims;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace GymManagement.Api.Data;

public sealed partial class WorkspaceScope
{
    public async Task<LambdaExpression> QueryExpressionAsync(Type type, ClaimsPrincipal p, CancellationToken ct)
    {
        var parameter = Expression.Parameter(type, "row");
        Expression body = Expression.Constant(false);
        var uid = long.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : 0;
        var active = await db.Users.AnyAsync(x => x.UserId == uid && x.Status == "Active", ct);
        if (!active)
            return Expression.Lambda(body, parameter);
        var roles = await db.UserRoles.Where(x => x.UserId == uid).Select(x => x.Role.RoleName).ToListAsync(ct);
        var admin = roles.Contains("Admin");
        var staff = roles.Contains("Staff");
        var isTrainer = roles.Contains("Trainer");
        if (admin)
            return Expression.Lambda(Expression.Constant(true), parameter);
        var trainerId = await db.Trainers.Where(x => x.UserId == uid).Select(x => (long?)x.TrainerId).SingleOrDefaultAsync(ct);
        var members = await db.Members.Where(x => x.UserId == uid).Select(x => x.MemberId).ToListAsync(ct);
        if (isTrainer && trainerId != null)
            members.AddRange(await db.MemberTrainers.Where(x => x.TrainerId == trainerId && x.Status == "Active").Select(x => x.MemberId).ToListAsync(ct));
        Expression Eq(string name, long id) => Expression.Equal(Expression.Property(parameter, name), Expression.Constant(id));
        Expression In(string name, List<long> ids) => Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(long) }, Expression.Constant(ids), Expression.Property(parameter, name));
        Expression StaffOr(Expression expression) => staff ? Expression.Constant(true) : expression;
        if (type == typeof(User))
        {
            var ids = await db.Members.Where(x => members.Contains(x.MemberId)).Select(x => x.UserId).ToListAsync(ct);
            ids.Add(uid);
            body = StaffOr(In("UserId", ids));
        }
        else if (type == typeof(UserRole) || type == typeof(Notification) || type == typeof(Staff))
            body = Eq("UserId", uid);
        else if (type == typeof(Member))
            body = StaffOr(In("MemberId", members));
        else if (new[] { typeof(EmergencyContact), typeof(MemberTrainer), typeof(MemberPackage), typeof(Payment), typeof(Booking), typeof(CheckIn) }.Contains(type))
            body = StaffOr(In("MemberId", members));
        else if (new[] { typeof(WorkoutPlan), typeof(WorkoutResult), typeof(Progress), typeof(AIConversation), typeof(AIRecommendation) }.Contains(type))
            body = In("MemberId", members);
        else if (type == typeof(ChurnPrediction))
            body = isTrainer ? In("MemberId", members) : Expression.Constant(false);
        else if (type == typeof(WorkoutPlanDetail))
        {
            var ids = await db.WorkoutPlans.Where(x => members.Contains(x.MemberId)).Select(x => x.PlanId).ToListAsync(ct);
            body = In("PlanId", ids);
        }
        else if (type == typeof(Attendance))
        {
            var ids = await db.Bookings.Where(x => members.Contains(x.MemberId)).Select(x => x.BookingId).ToListAsync(ct);
            body = StaffOr(In("BookingId", ids));
        }
        else if (type == typeof(AIConversationMessage))
        {
            var ids = await db.AIConversations.Where(x => members.Contains(x.MemberId)).Select(x => x.ConversationId).ToListAsync(ct);
            body = In("ConversationId", ids);
        }
        else if (type == typeof(TrainerSchedule))
            body = staff ? Expression.Constant(true) : Eq("TrainerId", trainerId ?? 0);
        else if (type == typeof(WorkoutSchedule))
            body = staff || !isTrainer ? Expression.Constant(true) : Eq("TrainerId", trainerId ?? 0);
        else if (new[] { typeof(Trainer), typeof(Package), typeof(Room), typeof(Exercise) }.Contains(type))
            body = Expression.Constant(true);
        else if (new[] { typeof(Equipment), typeof(EquipmentCategory), typeof(MaintenanceRecord), typeof(PaymentTransaction) }.Contains(type))
            body = Expression.Constant(staff);
        return Expression.Lambda(body, parameter);
    }
}
