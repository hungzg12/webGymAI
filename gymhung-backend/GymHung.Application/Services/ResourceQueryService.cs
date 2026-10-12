using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;
using System.Security.Claims;
using GymManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
namespace GymHung.Application;

public sealed class ResourceQueryService(GymDbContext db, WorkspaceScope scope, AccessService access, WorkspaceService workspace)
{
    public async Task<Dictionary<string, object?>> WorkspaceAsync(ClaimsPrincipal p, CancellationToken ct)
    {
        var result = new Dictionary<string, object?>();
        foreach (var type in db.Model.GetEntityTypes().OrderBy(x => x.GetTableName()))
        {
            var table = type.GetTableName()!;
            var permission = await access.HasAsync(p, AccessService.Module(table) + ".read", ct);
            var own = table is "Users" or "UserRoles";
            if (permission || own)
            {
                var method = GetType().GetMethod(nameof(All), BindingFlags.NonPublic | BindingFlags.Instance)!;
                var rows = await (Task<List<object>>)method.MakeGenericMethod(type.ClrType).Invoke(this, new object[] { p, ct })!;
                result[table] = rows.Select(r => ResourceDto.From(r, type)).ToArray();
            }
            else
                result[table] = Array.Empty<object>();
        }
        return result;
    }
    private async Task<IQueryable<T>> Scoped<T>(ClaimsPrincipal p, CancellationToken ct) where T : class
    {
        var lambda = await scope.QueryExpressionAsync(typeof(T), p, ct);
        var predicate = Expression.Lambda<Func<T, bool>>(lambda.Body, (ParameterExpression)lambda.Parameters[0]);
        return db.Set<T>().AsNoTracking().Where(predicate);
    }
    private async Task<List<object>> All<T>(ClaimsPrincipal p, CancellationToken ct) where T : class => (await (await Scoped<T>(p, ct)).ToListAsync(ct)).Cast<object>().ToList();
    public async Task<object> ListAsync(ClaimsPrincipal p, string resource, int page, int pageSize, string? search, string? sort, string? direction, string? status, string? memberId, string? from, string? to, IReadOnlyDictionary<string, string> filters, CancellationToken ct)
    {
        var type = workspace.Entity(resource);
        await access.DemandAsync(p, AccessService.Module(type.GetTableName()!) + ".read", ct);
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw new ApiException(400, "Page cần >=1; pageSize từ1 đến100.");
        if (direction != null && direction is not ("asc" or "desc"))
            throw new ApiException(400, "Hướng sắp xếp không hợp lệ.");
        var method = GetType().GetMethod(nameof(Page), BindingFlags.NonPublic | BindingFlags.Instance)!;
        try
        {
            return await (Task<object>)method.MakeGenericMethod(type.ClrType).Invoke(this, new object?[] { p, type, page, pageSize, search, sort, direction, from, to, filters, ct })!;
        }
        catch (TargetInvocationException e) when (e.InnerException != null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
    }
    private async Task<object> Page<T>(ClaimsPrincipal p, IEntityType type, int page, int pageSize, string? search, string? sort, string? direction, string? from, string? to, IReadOnlyDictionary<string, string> filters, CancellationToken ct) where T : class
    {
        var query = await Scoped<T>(p, ct);
        var row = Expression.Parameter(typeof(T), "r");
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (type.GetTableName() is "Members" or "Trainers" or "Staffs")
                query = query.Where(r => db.Users.Any(u => u.UserId == EF.Property<long>(r, "UserId") && (u.FullName.Contains(search) || u.Email.Contains(search) || (u.Phone != null && u.Phone.Contains(search)))));
            else
            {
                Expression body = Expression.Constant(false);
                foreach (var prop in type.GetProperties().Where(x => x.ClrType == typeof(string) && x.Name != "PasswordHash" && x.Name != "ContextData" && x.Name != "RequestData" && x.Name != "ResponseData" && x.Name != "OldData" && x.Name != "NewData"))
                {
                    var member = Expression.Property(row, prop.Name);
                    body = Expression.OrElse(body, Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, typeof(string))), Expression.Call(member, nameof(string.Contains), Type.EmptyTypes, Expression.Constant(search))));
                }
                query = query.Where(Expression.Lambda<Func<T, bool>>(body, row));
            }
        }
        foreach (var filter in filters)
        {
            var prop = type.GetProperties().FirstOrDefault(x => string.Equals(x.Name, filter.Key, StringComparison.OrdinalIgnoreCase));
            if (prop == null || prop.Name == "PasswordHash" || string.IsNullOrEmpty(filter.Value) || filter.Key.Equals("sort", StringComparison.OrdinalIgnoreCase))
                continue;
            var member = Expression.Property(row, prop.Name);
            var clr = Nullable.GetUnderlyingType(prop.ClrType) ?? prop.ClrType;
            object? parsed;
            try
            {
                parsed = clr == typeof(DateOnly) ? DateOnly.Parse(filter.Value, CultureInfo.InvariantCulture) : clr == typeof(TimeOnly) ? TimeOnly.Parse(filter.Value, CultureInfo.InvariantCulture) : clr == typeof(DateTime) ? DateTime.Parse(filter.Value, CultureInfo.InvariantCulture) : Convert.ChangeType(filter.Value, clr, CultureInfo.InvariantCulture);
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException) { throw new ApiException(400, $"Bộ lọc {filter.Key} không hợp lệ."); }
            var constant = Expression.Convert(Expression.Constant(parsed, clr), prop.ClrType);
            query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.Equal(member, constant), row));
        }
        var dateProperty = new[] { "PaidAt", "CheckInTime", "RecordDate", "MaintenanceDate", "ScheduleDate", "WorkoutDate", "WorkDate", "JoinDate", "HireDate", "StartDate", "CreatedAt" }.Select(n => type.FindProperty(n)).FirstOrDefault(x => x != null);
        if (dateProperty != null && (from != null || to != null))
        {
            var member = Expression.Property(row, dateProperty.Name);
            var clr = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
            if (from != null)
            {
                if (!DateOnly.TryParse(from, CultureInfo.InvariantCulture, out var date))
                    throw new ApiException(400, "Ngày bắt đầu không hợp lệ.");
                var value = clr == typeof(DateOnly) ? (object)date : date.ToDateTime(TimeOnly.MinValue);
                query = query.Where(Expression.Lambda<Func<T, bool>>(Expression.GreaterThanOrEqual(member, Expression.Convert(Expression.Constant(value, clr), member.Type)), row));
            }
            if (to != null)
            {
                if (!DateOnly.TryParse(to, CultureInfo.InvariantCulture, out var date))
                    throw new ApiException(400, "Ngày kết thúc không hợp lệ.");
                var value = clr == typeof(DateOnly) ? (object)date : date.AddDays(1).ToDateTime(TimeOnly.MinValue);
                var constant = Expression.Convert(Expression.Constant(value, clr), member.Type);
                Expression comparison = clr == typeof(DateOnly) ? Expression.LessThanOrEqual(member, constant) : Expression.LessThan(member, constant);
                query = query.Where(Expression.Lambda<Func<T, bool>>(comparison, row));
            }
        }
        var field = sort == null ? type.FindPrimaryKey()!.Properties[0] : type.GetProperties().FirstOrDefault(x => string.Equals(x.Name, sort, StringComparison.OrdinalIgnoreCase) && x.Name != "PasswordHash" && x.Name != "ContextData" && x.Name != "OldData" && x.Name != "NewData" && x.Name != "RequestData" && x.Name != "ResponseData");
        if (field == null)
            throw new ApiException(400, "Trường sắp xếp không được phép.");
        var keySelector = Expression.Lambda(Expression.Property(row, field.Name), row);
        var order = typeof(Queryable).GetMethods().Single(m => m.Name == (direction == "desc" ? "OrderByDescending" : "OrderBy") && m.GetParameters().Length == 2).MakeGenericMethod(typeof(T), field.ClrType);
        query = (IQueryable<T>)order.Invoke(null, new object[] { query, keySelector })!;
        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(checked((page - 1) * pageSize)).Take(pageSize).ToListAsync(ct);
        return new
        {
            items = items.Select(x => ResourceDto.From(x, type)),
            page,
            pageSize,
            totalCount
        };
    }
}
