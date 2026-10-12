using System.Security.Claims;
using System.Text.Json;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace GymHung.Application;

public sealed record LoginRequest(string Username, string Password);
public sealed record MemberRegistrationRequest(string Username, string Email, string FullName, string Password, string? Phone = null);
public sealed record PasswordChangeRequest(string CurrentPassword, string NewPassword);
public sealed class AccountService(GymDbContext db, IPasswordHasher<User> hasher, WorkspaceService workspace)
{
    public async Task<User> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ApiException(400, "Tên đăng nhập và mật khẩu là bắt buộc.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Username == request.Username || x.Email == request.Username, ct);
        if (user is null || user.Status != "Active" || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new ApiException(401, "Thông tin đăng nhập không đúng.");
        user.LastLoginAt = GymClock.Now;
        await db.SaveChangesAsync(ct);
        return user;
    }
    public async Task<object> SessionAsync(long id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id && x.Status == "Active", ct) ?? throw new ApiException(401, "Tài khoản không hoạt động.");
        var roles = await db.UserRoles.Where(x => x.UserId == id).Select(x => x.Role.RoleName).ToListAsync(ct);
        var role = roles.OrderBy(x => Array.IndexOf(new[] { "Admin", "Staff", "Trainer", "Member" }, x)).FirstOrDefault() ?? "Member";
        var perms = await db.UserRoles.Where(x => x.UserId == id).SelectMany(x => x.Role.RolePermissions).Select(x => x.Permission.PermissionCode).Distinct().ToListAsync(ct);
        return new
        {
            userId = id.ToString(),
            fullName = user.FullName,
            role,
            roles,
            permissions = perms,
            memberId = (await db.Members.Where(x => x.UserId == id).Select(x => (long?)x.MemberId).SingleOrDefaultAsync(ct))?.ToString(),
            trainerId = (await db.Trainers.Where(x => x.UserId == id).Select(x => (long?)x.TrainerId).SingleOrDefaultAsync(ct))?.ToString(),
            profile = new
            {
                user.Username,
                user.Email,
                user.FullName,
                user.Phone,
                user.AvatarUrl,
                user.Gender,
                user.DateOfBirth
            }
        };
    }
    public async Task<IReadOnlyList<string>> RolesAsync(long id, CancellationToken ct) => await db.UserRoles.Where(x => x.UserId == id).Select(x => x.Role.RoleName).ToListAsync(ct);
    public async Task<object> RegisterAsync(MemberRegistrationRequest req, CancellationToken ct)
    {
        if (req.Password.Length < 8 || req.Password.Length > 128)
            throw new ApiException(400, "Mật khẩu cần từ 8 đến 128 ký tự.");
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length > 100 || req.Email.Length > 255 || req.FullName.Length > 150 || string.IsNullOrWhiteSpace(req.FullName) || !System.Net.Mail.MailAddress.TryCreate(req.Email, out _) || req.Phone?.Length > 30)
            throw new ApiException(400, "Thông tin tài khoản không hợp lệ.");
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await workspace.LockAsync("Users", ct);
        if (await db.Users.AnyAsync(x => x.Username == req.Username || x.Email == req.Email, ct))
            throw new ApiException(409, "Username hoặc email đã tồn tại.");
        var u = new User { Username = req.Username.Trim(), Email = req.Email.Trim(), FullName = req.FullName.Trim(), Phone = req.Phone, Status = "Active", CreatedAt = GymClock.Now };
        u.PasswordHash = hasher.HashPassword(u, req.Password);
        db.Add(u);
        await db.SaveChangesAsync(ct);
        var role = await db.Roles.SingleAsync(x => x.RoleName == "Member", ct);
        db.Add(new UserRole { UserId = u.UserId, RoleId = role.RoleId, AssignedAt = GymClock.Now });
        db.Add(new Member { UserId = u.UserId, Status = "Active", JoinDate = GymClock.Today, CreatedAt = GymClock.Now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new
        {
            userId = u.UserId.ToString(),
            username = u.Username,
            role = "Member"
        };
    }
    public async Task ChangePasswordAsync(long id, PasswordChangeRequest req, CancellationToken ct)
    {
        if (req.NewPassword.Length < 8 || req.NewPassword.Length > 128)
            throw new ApiException(400, "Mật khẩu cần từ 8 đến 128 ký tự.");
        var u = await db.Users.SingleOrDefaultAsync(x => x.UserId == id && x.Status == "Active", ct) ?? throw new ApiException(401, "Tài khoản không hoạt động.");
        if (hasher.VerifyHashedPassword(u, u.PasswordHash, req.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new ApiException(400, "Mật khẩu hiện tại không đúng.");
        u.PasswordHash = hasher.HashPassword(u, req.NewPassword);
        u.UpdatedAt = GymClock.Now;
        db.Add(new AuditLog { UserId = id, Action = "ChangePassword", EntityName = "Users", EntityId = id, CreatedAt = GymClock.Now });
        await db.SaveChangesAsync(ct);
    }
}

