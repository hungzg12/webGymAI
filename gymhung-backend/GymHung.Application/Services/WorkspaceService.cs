using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
namespace GymHung.Application;

public sealed class WorkspaceService(GymDbContext db, WorkspaceScope scope, AccessService access, IPasswordHasher<User> hasher, PackageUsageService usage)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { NumberHandling = JsonNumberHandling.AllowReadingFromString };
    private static readonly HashSet<string> PrivateTables = new(StringComparer.OrdinalIgnoreCase) { "AuditLogs", "PaymentTransactions", "AIConversationMessages", "ChurnPredictions" };
    public IEntityType Entity(string table) => db.Model.GetEntityTypes().FirstOrDefault(x => string.Equals(x.GetTableName(), table.Replace("-", ""), StringComparison.OrdinalIgnoreCase)) ?? throw new ApiException(404, "Không tìm thấy tài nguyên.");
    public async Task<object> SaveAsync(ClaimsPrincipal principal, string table, string? id, MutationRequest request, CancellationToken ct)
    {
        var type = Entity(table);
        table = type.GetTableName()!;
        if (PrivateTables.Contains(table))
            throw new ApiException(403, "Tài nguyên hệ thống chỉ được tạo qua luồng nghiệp vụ.");
        var ownProfile = table == "Users" && id == principal.FindFirstValue(ClaimTypes.NameIdentifier);
        await access.DemandAsync(principal, table == "Packages" ? "packages.catalog.write" : ownProfile ? "profile.write" : AccessService.Module(table) + ".write", ct);
        if (request.Draft.ValueKind != JsonValueKind.Object)
            throw new ApiException(400, "Dữ liệu phải là đối tượng JSON.");
        if (string.IsNullOrEmpty(id) && table is "CheckIns" or "Attendance")
        {
            if (table == "Attendance" && TryValue(request.Draft, "Status", out var status) && status.GetString() == "Absent")
            {
                // An absence is recorded by the existing validation flow and does not consume PT sessions.
            }
            else
            {
                var keyField = table == "CheckIns" ? "MemberId" : "BookingId";
                if (!TryValue(request.Draft, keyField, out var selected)) throw new ApiException(400, "Cần chọn hội viên hoặc lịch tập.");
                long? registration; long selectedId;
                try
                {
                    registration = TryValue(request.Draft, "MemberPackageId", out var chosen) && chosen.ValueKind != JsonValueKind.Null
                        ? JsonSerializer.Deserialize<long>(chosen.GetRawText(), Json) : null;
                    selectedId = JsonSerializer.Deserialize<long>(selected.GetRawText(), Json);
                }
                catch (JsonException) { throw new ApiException(400, "Mã hội viên, lịch hoặc gói không hợp lệ."); }
                return table == "CheckIns" ? await usage.CheckInAsync(principal, selectedId, registration, ct)
                    : await usage.AttendAsync(principal, selectedId, registration, ct);
            }
        }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Transaction-owned application lock also covers cross-row range checks without changing schema.
        await LockAsync(table is "Bookings" or "WorkoutSchedules" ? "gym-scheduling" : table is "Payments" ? "gym-payments" : table, ct);
        var allowed = await scope.CreateAsync(principal, ct);
        var key = type.FindPrimaryKey()!;
        object entity;
        bool creating = string.IsNullOrEmpty(id);
        object? old = null;
        if (creating)
        {
            entity = Activator.CreateInstance(type.ClrType)!;
            Defaults(entity, type);
        }
        else
        {
            entity = await db.FindAsync(type.ClrType, Keys(id!, key), ct) ?? throw new ApiException(404, "Không tìm thấy bản ghi.");
            if (!allowed(entity))
                throw new ApiException(403, "Bản ghi ngoài phạm vi tài khoản.");
            old = JsonSerializer.Deserialize(JsonSerializer.Serialize(ResourceDto.From(entity, type)), type.ClrType, Json);
        }
        if (entity is Payment && !creating)
            throw new ApiException(409, "Thanh toán đã ghi nhận không được sửa; dùng luồng xác nhận thanh toán.");
        if (entity is Attendance attendance && !creating && await db.AuditLogs.AnyAsync(x => x.Action == "UsePackage" && x.EntityName == "Attendance" && x.EntityId == attendance.AttendanceId, ct)
            && TryValue(request.Draft, "Status", out var postedStatus) && postedStatus.GetString() is not ("Present" or "Late"))
            throw new ApiException(409, "Buổi PT đã sử dụng phải giữ lịch sử điểm danh.");
        if (entity is Attendance && !creating && old is Attendance previousAttendance && previousAttendance.Status == "Absent"
            && TryValue(request.Draft, "Status", out var changeStatus) && changeStatus.GetString() != "Absent")
            throw new ApiException(409, "Không chuyển buổi vắng thành buổi sử dụng qua chỉnh sửa chung.");
        if (entity is MemberPackage && !creating && request.Draft.EnumerateObject().Any(x => new[] { "price", "packageId", "memberId", "startDate", "endDate", "remainingSessions" }.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
            throw new ApiException(409, "Gia hạn phải tạo gói đăng ký mới để giữ lịch sử giá.");
        if (!creating)
        foreach (var fk in type.GetForeignKeys().SelectMany(x => x.Properties).Distinct())
        if (TryValue(request.Draft, fk.Name, out var posted) && posted.ValueKind != JsonValueKind.Null && !Equals(fk.PropertyInfo!.GetValue(entity), ConvertJson(posted, fk.ClrType)))
            throw new ApiException(409, "Không thể chuyển bản ghi sang đối tượng khác.");
        // Aggregate account creation derives UserId from the new account, never client input.
        var accountAggregate = creating && request.UserDraft is not null && entity is Member or Trainer or Staff;
        var editableFields = accountAggregate ? type.GetProperties().Where(p => p.Name != "UserId").Select(p => p.Name).ToHashSet() : null;
        Apply(entity, type, request.Draft, creating, ownProfile, editableFields);
        if (creating && entity is Member or Trainer or Staff)
        {
            if (request.UserDraft is { } userDraft)
            {
                var u = await CreateAccountAsync(userDraft, entity is Trainer ? "Trainer" : entity is Staff ? "Staff" : "Member", ct);
                entity.GetType().GetProperty("UserId")!.SetValue(entity, u.UserId);
            }
            else if (entity.GetType().GetProperty("UserId")!.GetValue(entity) is not long uid || uid == 0)
                throw new ApiException(400, "Cần thông tin tài khoản liên kết.");
        }
        if (!creating && request.UserDraft is { } accountDraft && entity is Member or Trainer or Staff)
        {
            var linkedId = (long)entity.GetType().GetProperty("UserId")!.GetValue(entity)!;
            var linked = await db.Users.FindAsync(new object[] { linkedId }, ct) ?? throw new ApiException(400, "Tài khoản liên kết không tồn tại.");
            Apply(linked, db.Model.FindEntityType(typeof(User))!, accountDraft, false, true);
            linked.UpdatedAt = GymClock.Now;
            ValidateMetadata(linked, db.Model.FindEntityType(typeof(User))!);
            await ValidateAsync(linked, null, false, principal, ct);
        }
        if (principal.IsInRole("Trainer") && !principal.IsInRole("Admin") && !principal.IsInRole("Staff") && entity.GetType().GetProperty("TrainerId") is { } trainerProperty)
        {
            var ownTrainer = await db.Trainers.Where(x => x.UserId == long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!)).Select(x => x.TrainerId).SingleAsync(ct);
            if (trainerProperty.GetValue(entity) is long assigned && assigned != ownTrainer)
                throw new ApiException(403, "Chỉ được quản lý lịch và giáo án do mình phụ trách.");
        }
        if (!creating && entity.GetType().GetProperty("UpdatedAt") is { } updated)
            updated.SetValue(entity, GymClock.Now);
        if (creating && entity is User)
            throw new ApiException(400, "Tạo tài khoản qua hồ sơ hội viên, PT hoặc nhân viên.");
        if (!allowed(entity))
            throw new ApiException(403, "Dữ liệu ngoài phạm vi tài khoản.");
        if (entity is Notification && !creating && !principal.IsInRole("Admin"))
        {
            var original = (Notification)old!;
            entity = original;
            Apply(entity, type, request.Draft, false, false, new HashSet<string> { "IsRead" });
            var tracked = await db.Notifications.FindAsync(new object[] { original.NotificationId }, ct) ?? throw new ApiException(404, "Thông báo không tồn tại.");
            db.Entry(tracked).CurrentValues.SetValues(entity);
        }
        ValidateMetadata(entity, type);
        await ValidateAsync(entity, old, creating, principal, ct);
        if (creating)
            db.Add(entity);
        await db.SaveChangesAsync(ct);
        var pk = key.Properties.First().PropertyInfo!.GetValue(entity);
        var uidParsed = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        db.AuditLogs.Add(new AuditLog { UserId = uidParsed, Action = creating ? "Create" : "Update", EntityName = table, EntityId = pk is long n ? n : null, CreatedAt = GymClock.Now, NewData = JsonSerializer.Serialize(new { fields = request.Draft.EnumerateObject().Select(x => x.Name).Where(x => !x.Contains("password", StringComparison.OrdinalIgnoreCase)).ToArray() }) });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return ResourceDto.From(entity, type);
    }
    public async Task RemoveAsync(ClaimsPrincipal p, string table, string id, CancellationToken ct)
    {
        var type = Entity(table);
        table = type.GetTableName()!;
        if (PrivateTables.Contains(table) || table is "Payments" or "MemberPackages" or "Bookings" or "Users")
            throw new ApiException(409, "Giữ lịch sử nghiệp vụ; dùng cập nhật trạng thái hoặc luồng hủy.");
        await access.DemandAsync(p, table == "Packages" ? "packages.catalog.write" : AccessService.Module(table) + ".write", ct);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LockAsync(table, ct);
        var row = await db.FindAsync(type.ClrType, Keys(id, type.FindPrimaryKey()!), ct) ?? throw new ApiException(404, "Không tìm thấy bản ghi.");
        if (row is Attendance att && await db.AuditLogs.AnyAsync(x => x.Action == "UsePackage" && x.EntityName == "Attendance" && x.EntityId == att.AttendanceId, ct))
            throw new ApiException(409, "Không xóa điểm danh đã sử dụng gói PT.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(row))
            throw new ApiException(403, "Bản ghi ngoài phạm vi tài khoản.");
        db.Remove(row);
        db.AuditLogs.Add(new AuditLog { UserId = long.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "Delete", EntityName = table, CreatedAt = GymClock.Now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    public async Task LockAsync(string resource, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 51000, 'Resource busy', 1;", ct);
    }
    public static object?[] Keys(string id, IKey key)
    {
        var parts = id.Split(':');
        if (parts.Length != key.Properties.Count)
            throw new ApiException(400, "Mã bản ghi không hợp lệ.");
        try
        {
            return parts.Select((s, i) => Convert.ChangeType(s, key.Properties[i].ClrType, CultureInfo.InvariantCulture)).ToArray();
        }
        catch (Exception e) when (e is FormatException or OverflowException) { throw new ApiException(400, "Mã bản ghi không hợp lệ."); }
    }
    private static bool TryValue(JsonElement json, string name, out JsonElement v) => json.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(name), out v) || json.TryGetProperty(name, out v);
    private static object? ConvertJson(JsonElement v, Type type) => JsonSerializer.Deserialize(v.GetRawText(), type, Json);
    private static void Apply(object entity, IEntityType type, JsonElement draft, bool creating, bool ownProfile, HashSet<string>? whitelist = null)
    {
        foreach (var p in type.GetProperties())
        {
            if (p.PropertyInfo is null || p.Name == "PasswordHash" || p.Name is "CreatedAt" or "UpdatedAt" or "AssignedAt" or "StartedAt" or "BookingTime" or "LastLoginAt" or "CurrentParticipants" or "PaidAt" or "Provider" or "TransactionCode")
                continue;
            if (type.FindPrimaryKey()!.Properties.Contains(p) && (!creating || p.ValueGenerated != ValueGenerated.Never))
                continue;
            if (ownProfile && !new[] { "FullName", "Phone", "AvatarUrl", "Gender", "DateOfBirth", "Email" }.Contains(p.Name))
                continue;
            if (whitelist is not null && !whitelist.Contains(p.Name))
                continue;
            if (!TryValue(draft, p.Name, out var value))
                continue;
            try
            {
                p.PropertyInfo.SetValue(entity, ConvertJson(value, p.ClrType));
            }
            catch (Exception e) when (e is JsonException or ArgumentException or FormatException) { throw new ApiException(400, $"Giá trị {p.Name} không hợp lệ."); }
        }
    }
    private static void Defaults(object entity, IEntityType type)
    {
        foreach (var p in type.GetProperties())
        {
            if (p.PropertyInfo is null)
                continue;
            if (p.GetDefaultValue() is { } val && val != DBNull.Value)
                p.PropertyInfo.SetValue(entity, val);
            if (p.ClrType == typeof(DateTime) && p.Name.EndsWith("At") || p.Name == "BookingTime" || p.Name == "CheckInTime")
                p.PropertyInfo.SetValue(entity, GymClock.Now);
        }
        if (entity is Member m)
            m.JoinDate = GymClock.Today;
    }
    private static void ValidateMetadata(object entity, IEntityType type)
    {
        foreach (var p in type.GetProperties())
        {
            var v = p.PropertyInfo?.GetValue(entity);
            if (!p.IsNullable && v is null)
                throw new ApiException(400, $"{p.Name} là bắt buộc.");
            if (v is string s)
            {
                if (!p.IsNullable && string.IsNullOrWhiteSpace(s))
                    throw new ApiException(400, $"{p.Name} là bắt buộc.");
                if (p.GetMaxLength() is int max && s.Length > max)
                    throw new ApiException(400, $"{p.Name} tối đa {max} ký tự.");
            }
            if (v is decimal d)
            {
                if (d < 0)
                    throw new ApiException(400, $"{p.Name} không được âm.");
                var precision = p.GetPrecision();
                var scale = p.GetScale();
                var sqlType = p.GetColumnType();
                if (precision is null && sqlType?.StartsWith("decimal(") == true)
                {
                    var split = sqlType[8..].TrimEnd(')').Split(',');
                    precision = int.Parse(split[0]);
                    scale = int.Parse(split[1]);
                }
                if (precision is int pr && scale is int sc && (d >= (decimal)Math.Pow(10, pr - sc) || decimal.Round(d, sc) != d))
                    throw new ApiException(400, $"{p.Name} vượt độ chính xác DECIMAL.");
            }
            if (v is int i && i < 0)
                throw new ApiException(400, $"{p.Name} không được âm.");
        }
        var start = entity.GetType().GetProperty("StartDate")?.GetValue(entity);
        var end = entity.GetType().GetProperty("EndDate")?.GetValue(entity);
        if (start is DateOnly a && end is DateOnly b && b < a)
            throw new ApiException(400, "Ngày kết thúc không được trước ngày bắt đầu.");
    }
    private async Task<User> CreateAccountAsync(JsonElement draft, string roleName, CancellationToken ct)
    {
        var user = new User { Status = "Active", CreatedAt = GymClock.Now };
        var type = db.Model.FindEntityType(typeof(User))!;
        Apply(user, type, draft, true, false);
        var password = TryValue(draft, "Password", out var value) ? value.GetString() : null;
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128)
            throw new ApiException(400, "Mật khẩu tài khoản mới cần từ 8 đến 128 ký tự.");
        user.PasswordHash = hasher.HashPassword(user, password);
        ValidateMetadata(user, type);
        if (!System.Net.Mail.MailAddress.TryCreate(user.Email, out _))
            throw new ApiException(400, "Email không hợp lệ.");
        if (await db.Users.AnyAsync(x => x.Username == user.Username || x.Email == user.Email, ct))
            throw new ApiException(409, "Username hoặc email đã tồn tại.");
        db.Add(user);
        await db.SaveChangesAsync(ct);
        var role = await db.Roles.SingleAsync(x => x.RoleName == roleName, ct);
        db.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId, AssignedAt = GymClock.Now });
        return user;
    }
    private async Task ValidateAsync(object entity, object? old, bool creating, ClaimsPrincipal p, CancellationToken ct)
    {
        switch (entity)
        {
            case User u:
                if (!System.Net.Mail.MailAddress.TryCreate(u.Email, out _))
                    throw new ApiException(400, "Email không hợp lệ.");
                if (await db.Users.AnyAsync(x => x.UserId != u.UserId && (x.Username == u.Username || x.Email == u.Email), ct))
                    throw new ApiException(409, "Username hoặc email đã tồn tại.");
                break;
            case Member m:
                if (m.Height is <= 0 || m.Weight is <= 0)
                    throw new ApiException(400, "Chiều cao và cân nặng phải dương.");
                break;
            case Trainer t:
                if (t.Rating is > 5)
                    throw new ApiException(400, "Đánh giá tối đa 5.");
                break;
            case Progress pr:
                if (pr.BodyFat is > 100 || pr.Weight is <= 0)
                    throw new ApiException(400, "Chỉ số cơ thể không hợp lệ.");
                break;
            case Package catalog:
                if (catalog.DurationDays is <= 0)
                    throw new ApiException(400, "Thời hạn gói tập phải từ 1 ngày trở lên.");
                break;
            case MemberPackage mp:
                if (creating)
                {
                    var package = await db.Packages.FindAsync(new object[] { mp.PackageId }, ct) ?? throw new ApiException(400, "Gói tập không tồn tại.");
                    if (package.Status != "Active")
                        throw new ApiException(409, "Gói tập không còn mở bán.");
                    mp.Price = package.Price;
                    mp.RemainingSessions = package.TotalSessions;
                    mp.EndDate = package.DurationDays is int days ? mp.StartDate.AddDays(days - 1) : null;
                }
                break;
            case Payment pay:
                var owned = await db.MemberPackages.FindAsync(new object[] { pay.MemberPackageId }, ct);
                if (owned is null || owned.MemberId != pay.MemberId)
                    throw new ApiException(400, "Gói tập không thuộc hội viên.");
                if (pay.Amount <= 0 || pay.Amount != owned.Price)
                    throw new ApiException(400, "Số tiền phải khớp giá gói đăng ký.");
                if (await db.Payments.AnyAsync(x => x.MemberPackageId == pay.MemberPackageId && x.Status == "Completed", ct))
                    throw new ApiException(409, "Gói đăng ký đã được thanh toán.");
                if (pay.PaymentMethod is not ("Cash" or "VNPay" or "Momo"))
                    throw new ApiException(400, "Phương thức thanh toán không hợp lệ.");
                if (pay.PaymentMethod != "Cash" && pay.Status == "Completed")
                    throw new ApiException(400, "Thanh toán điện tử phải được xác nhận từ backend.");
                if (pay.Status is not ("Pending" or "Completed"))
                    throw new ApiException(400, "Trạng thái thanh toán không hợp lệ.");
                if (pay.Status == "Completed")
                    pay.PaidAt = GymClock.Now;
                if (pay.PaymentMethod != "Cash")
                {
                    pay.Status = "Pending";
                    pay.Provider = "Demo-" + pay.PaymentMethod;
                }
                break;
            case WorkoutSchedule s:
                if (s.EndTime <= s.StartTime)
                    throw new ApiException(400, "Giờ kết thúc phải sau giờ bắt đầu.");
                if (s.Capacity is <= 0)
                    throw new ApiException(400, "Sức chứa phải dương.");
                if (s.RoomId is long roomId)
                {
                    var room = await db.Rooms.FindAsync(new object[] { roomId }, ct);
                    if (room is null || room.Capacity is int cap && (s.Capacity is null || s.Capacity > cap))
                        throw new ApiException(400, "Sức chứa vượt phòng tập.");
                }
                if (await db.WorkoutSchedules.AnyAsync(x => x.WorkoutScheduleId != s.WorkoutScheduleId && x.Status != "Canceled" && x.ScheduleDate == s.ScheduleDate && (x.TrainerId == s.TrainerId || s.RoomId != null && x.RoomId == s.RoomId) && x.StartTime < s.EndTime && s.StartTime < x.EndTime, ct))
                    throw new ApiException(409, "PT hoặc phòng tập đã có lịch trùng.");
                var count = await db.Bookings.CountAsync(x => x.WorkoutScheduleId == s.WorkoutScheduleId && x.Status == "Confirmed", ct);
                if (s.Capacity is int capacity && count > capacity)
                    throw new ApiException(409, "Sức chứa thấp hơn số chỗ đã đặt.");
                s.CurrentParticipants = count;
                if (s.Status == "Canceled")
                {
                    var bs = await db.Bookings.Where(x => x.WorkoutScheduleId == s.WorkoutScheduleId && x.Status == "Confirmed").ToListAsync(ct);
                    foreach (var b in bs)
                        b.Status = "Canceled";
                    s.CurrentParticipants = 0;
                }
                if (s.PlanId is long planId && !await db.WorkoutPlans.AnyAsync(x => x.PlanId == planId && (x.TrainerId == null || x.TrainerId == s.TrainerId), ct))
                    throw new ApiException(400, "Giáo án không phù hợp PT.");
                break;
            case TrainerSchedule ts:
                if (ts.EndTime <= ts.StartTime)
                    throw new ApiException(400, "Giờ kết thúc phải sau giờ bắt đầu.");
                if (await db.TrainerSchedules.AnyAsync(x => x.TrainerScheduleId != ts.TrainerScheduleId && x.TrainerId == ts.TrainerId && x.WorkDate == ts.WorkDate && x.StartTime < ts.EndTime && ts.StartTime < x.EndTime, ct))
                    throw new ApiException(409, "Lịch PT bị trùng.");
                break;
            case Booking b:
                if (b.Status is not ("Confirmed" or "Canceled"))
                    throw new ApiException(400, "Trạng thái đặt chỗ không hợp lệ.");
                var schedule = await db.WorkoutSchedules.FindAsync(new object[] { b.WorkoutScheduleId }, ct) ?? throw new ApiException(400, "Buổi tập không tồn tại.");
                if (b.Status == "Confirmed")
                {
                    if (schedule.Status != "Available")
                        throw new ApiException(409, "Buổi tập không mở đặt chỗ.");
                    if (await db.Bookings.AnyAsync(x => x.MemberId == b.MemberId && x.WorkoutScheduleId == b.WorkoutScheduleId && x.Status == "Confirmed" && x.BookingId != b.BookingId, ct))
                        throw new ApiException(409, "Đã đặt buổi tập này.");
                    var n = await db.Bookings.CountAsync(x => x.WorkoutScheduleId == b.WorkoutScheduleId && x.Status == "Confirmed" && x.BookingId != b.BookingId, ct);
                    if (schedule.Capacity is int cap && n >= cap)
                        throw new ApiException(409, "Buổi tập đã đủ chỗ.");
                    schedule.CurrentParticipants = n + 1;
                }
                else
                    schedule.CurrentParticipants = await db.Bookings.CountAsync(x => x.WorkoutScheduleId == b.WorkoutScheduleId && x.Status == "Confirmed" && x.BookingId != b.BookingId, ct);
                break;
            case Attendance a:
                var booking = await db.Bookings.FindAsync(new object[] { a.BookingId }, ct);
                if (booking?.Status != "Confirmed")
                    throw new ApiException(400, "Booking không còn hiệu lực.");
                if (await db.Attendances.AnyAsync(x => x.BookingId == a.BookingId && x.AttendanceId != a.AttendanceId, ct))
                    throw new ApiException(409, "Booking đã được điểm danh.");
                if (a.CheckOutTime != null && (a.CheckInTime == null || a.CheckOutTime < a.CheckInTime))
                    throw new ApiException(400, "Check-out phải sau check-in.");
                break;
            case CheckIn c:
                if (c.Status is not ("CheckedIn" or "CheckedOut"))
                    throw new ApiException(400, "Trạng thái check-in không hợp lệ.");
                if (c.Status == "CheckedIn" && await db.CheckIns.AnyAsync(x => x.MemberId == c.MemberId && x.CheckOutTime == null && x.CheckInId != c.CheckInId, ct))
                    throw new ApiException(409, "Hội viên chưa check-out.");
                if (c.Status == "CheckedIn" && !await db.MemberPackages.AnyAsync(x => x.MemberId == c.MemberId && x.Status == "Active" && x.StartDate <= GymClock.Today && (x.EndDate == null || x.EndDate >= GymClock.Today) && (x.RemainingSessions == null || x.RemainingSessions > 0), ct))
                    throw new ApiException(409, "Hội viên chưa có gói hiệu lực.");
                if (c.Status == "CheckedOut")
                    c.CheckOutTime ??= GymClock.Now;
                if (c.CheckOutTime < c.CheckInTime)
                    throw new ApiException(400, "Check-out phải sau check-in.");
                if (old is CheckIn prev && prev.CheckOutTime != null)
                    throw new ApiException(409, "Check-in đã kết thúc.");
                break;
            case WorkoutResult r:
                if (r.PlanId != null && !await db.WorkoutPlans.AnyAsync(x => x.PlanId == r.PlanId && x.MemberId == r.MemberId, ct))
                    throw new ApiException(400, "Giáo án không thuộc hội viên.");
                if (r.PlanId != null && !await db.WorkoutPlanDetails.AnyAsync(x => x.PlanId == r.PlanId && x.ExerciseId == r.ExerciseId, ct))
                    throw new ApiException(400, "Bài tập không thuộc giáo án.");
                break;
        }
    }
}




