using System.Security.Claims;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace GymHung.Application;

public interface IPaymentProvider
{
    string Mode
    {
        get;
    }
    bool Verify(decimal expected, decimal received, string code);
}
public sealed class DemoPaymentProvider : IPaymentProvider
{
    public string Mode => "Demo"; public bool Verify(decimal expected, decimal received, string code) => expected == received && code.StartsWith("DEMO-", StringComparison.Ordinal) && code.Length <= 200;
}
public sealed record PaymentConfirmationRequest(decimal Amount, string TransactionCode);
public sealed class PaymentService(GymDbContext db, AccessService access, WorkspaceScope scope, WorkspaceService workspace)
{
    public async Task<object> ConfirmAsync(ClaimsPrincipal p, long id, PaymentConfirmationRequest req, bool cash, CancellationToken ct)
    {
        await access.DemandAsync(p, "payments.write", ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        await workspace.LockAsync("gym-payments", ct);
        var pay = await db.Payments.FindAsync(new object[] { id }, ct) ?? throw new ApiException(404, "Không tìm thấy thanh toán.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(pay))
            throw new ApiException(403, "Thanh toán ngoài phạm vi tài khoản.");
        if (pay.PaymentMethod == "Cash" != cash)
            throw new ApiException(400, "Phương thức không khớp luồng xác nhận.");
        if (cash)
        {
            if (req.Amount != pay.Amount)
                throw new ApiException(400, "Số tiền không khớp.");
        }
        else if (!new DemoPaymentProvider().Verify(pay.Amount, req.Amount, req.TransactionCode))
            throw new ApiException(400, "Dữ liệu xác nhận demo không hợp lệ.");
        if (pay.Status == "Completed")
        {
            if (pay.Amount == req.Amount && (cash || pay.TransactionCode == req.TransactionCode))
            {
                await tx.CommitAsync(ct);
                return new
                {
                    mode = cash ? "Cash" : "Demo",
                    replayed = true,
                    payment = ResourceDto.From(pay, db.Model.FindEntityType(typeof(Payment))!)
                };
            }
            throw new ApiException(409, "Thanh toán đã hoàn tất với giao dịch khác.");
        }
        if (await db.Payments.AnyAsync(x => x.MemberPackageId == pay.MemberPackageId && x.Status == "Completed" && x.PaymentId != id, ct))
            throw new ApiException(409, "Gói đăng ký đã được thanh toán.");
        if (await db.PaymentTransactions.AnyAsync(x => x.TransactionCode == req.TransactionCode && x.PaymentId != id, ct))
            throw new ApiException(409, "Mã giao dịch đã được dùng.");
        var package = await db.MemberPackages.FindAsync(new object[] { pay.MemberPackageId }, ct);
        if (package == null || package.MemberId != pay.MemberId || package.Price != pay.Amount)
            throw new ApiException(409, "Gói đăng ký không khớp thanh toán.");
        pay.Status = "Completed";
        pay.PaidAt = GymClock.Now;
        pay.TransactionCode = cash ? "CASH-" + id : req.TransactionCode;
        pay.Provider = cash ? "Cash" : "Demo-" + pay.PaymentMethod;
        db.Add(new PaymentTransaction { PaymentId = id, Provider = pay.Provider, TransactionCode = pay.TransactionCode, Amount = pay.Amount, TransactionStatus = "Completed", ResponseData = "{\"mode\":\"demo-or-cash\",\"verified\":true}", CreatedAt = GymClock.Now });
        db.Add(new AuditLog { UserId = long.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = cash ? "CashPayment" : "DemoPaymentConfirmed", EntityName = "Payments", EntityId = id, CreatedAt = GymClock.Now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new
        {
            mode = cash ? "Cash" : "Demo",
            replayed = false,
            payment = ResourceDto.From(pay, db.Model.FindEntityType(typeof(Payment))!)
        };
    }
}

