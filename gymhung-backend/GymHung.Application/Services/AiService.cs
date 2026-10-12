using System.Security.Claims;
using System.Text.Json;
using GymManagement.Api.Data;
using GymManagement.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
namespace GymHung.Application;

public interface IAiProvider
{
    string Name
    {
        get;
    }
    Task<string> AnswerAsync(string message, string? goal, CancellationToken ct);
}
public sealed class DemoAiProvider : IAiProvider
{
    public string Name => "AI demo — rules-v1"; public Task<string> AnswerAsync(string message, string? goal, CancellationToken ct) => Task.FromResult($"[AI demo • rules-v1] Mục tiêu hiện tại: {goal ?? "chưa cập nhật"}. Đây là phản hồi mô phỏng. Bạn có thể theo dõi lịch tập, giáo án được PT xác nhận và ghi nhận tiến độ trong GYM HUNG. Với đề xuất giáo án, hãy xem lại cùng PT trước khi xác nhận lưu.");
}
public sealed class AiService(GymDbContext db, WorkspaceScope scope, AccessService access, IAiProvider provider, AiSqlContext sqlContext)
{
    public async Task<object> SendAsync(ClaimsPrincipal p, long id, string? message, CancellationToken ct)
    {
        await access.DemandAsync(p, "ai.write", ct);
        if (string.IsNullOrWhiteSpace(message) || message.Length > 4000)
            throw new ApiException(400, "Tin nhắn cần từ 1 đến 4000 ký tự.");
        var agent = "auto";
        foreach (var mode in new[] { "fitness", "database", "pdf" })
            if (message.StartsWith($"[agent:{mode}]", StringComparison.Ordinal))
            {
                agent = mode;
                message = message[($"[agent:{mode}]").Length..].Trim();
                break;
            }
        if (string.IsNullOrWhiteSpace(message)) throw new ApiException(400, "Cần nhập câu hỏi cho tác nhân.");
        if (agent == "auto")
        {
            var query = message.ToLowerInvariant();
            agent = query.Contains("pdf") || query.Contains("tài liệu") ? "pdf"
                : new[] { "giá", "gói", "doanh thu", "hội viên", "lịch", "buổi", "thanh toán", "check-in", "sql", "tài khoản", "tên tôi" }.Any(query.Contains) ? "database" : "fitness";
        }
        var conversation = await db.AIConversations.SingleOrDefaultAsync(x => x.ConversationId == id, ct) ?? throw new ApiException(404, "Không tìm thấy cuộc trò chuyện.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(conversation))
            throw new ApiException(403, "Cuộc trò chuyện ngoài phạm vi tài khoản.");
        if (conversation.Status != "Active")
            throw new ApiException(409, "Cuộc trò chuyện đã đóng.");
        var member = await db.Members.FindAsync(new object[] { conversation.MemberId }, ct);
        var history = await db.AIConversationMessages.Where(x => x.ConversationId == id)
            .OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync(ct);
        var context = string.Join("\n", history.OrderBy(x => x.CreatedAt).Select(x => $"{x.SenderType}: {x.Message}"));
        var sql = agent == "database" ? await sqlContext.ReadAsync(p, conversation.MemberId, message, ct) : "{}";
        // Keep database data and mixed-agent history out of fitness/PDF requests.
        var prompt = $"[agent:{agent}]\n<sql_data>{sql}</sql_data>\nMục tiêu của hội viên: {member?.Goal ?? "chưa cập nhật"}.\nLịch sử hội thoại (chỉ để tham khảo):\n{(agent == "database" ? context : "")}\nCâu hỏi mới cần trả lời: {message}";
        var question = message.Trim().ToLowerInvariant();
        string? direct = null;
        if (new[] { "hello", "hi", "chào", "xin chào" }.Contains(question))
            direct = "Chào bạn! Tôi có thể tra cứu gói tập, hướng dẫn tập luyện hoặc đọc PDF. Bạn cần hỗ trợ gì?";
        else if (question.Contains("tài khoản") && (question.Contains("tên") || question.Contains("ai")) || question.Contains("tên tôi"))
        {
            var userId = long.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var current = await db.Users.Where(x => x.UserId == userId).Select(x => new { x.FullName, x.Username }).SingleAsync(ct);
            direct = $"Bạn đang đăng nhập bằng tài khoản {current.Username}, tên hiển thị là {current.FullName}.";
        }
        else if (agent == "database" && question.Contains("giá") && !question.Contains("gợi ý") && !question.Contains("so sánh"))
        {
            using var data = System.Text.Json.JsonDocument.Parse(sql);
            if (data.RootElement.GetProperty("tables").TryGetProperty("Packages", out var catalog))
            {
                var rows = catalog.GetProperty("rows").EnumerateArray().ToList();
                var named = rows.Where(x => question.Contains(x.GetProperty("PackageName").GetString()!.ToLowerInvariant())).ToList();
                if (named.Count > 0)
                    direct = string.Join("\n", named.Select(x => $"Giá {x.GetProperty("PackageName").GetString()} là {x.GetProperty("Price").GetDecimal().ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))} đồng."));
            }
        }
        else if (agent == "database" && question.Contains("gói") && (question.Contains("bao nhiêu ngày") || question.Contains("hết hạn") || question.Contains("còn mấy ngày")))
        {
            using var data = System.Text.Json.JsonDocument.Parse(sql);
            if (data.RootElement.GetProperty("tables").TryGetProperty("MemberPackages", out var packages))
            {
                var today = DateOnly.FromDateTime(GymClock.Now);
                var lines = new List<string>();
                foreach (var row in packages.GetProperty("rows").EnumerateArray())
                {
                    if (row.GetProperty("MemberId").GetInt64() != conversation.MemberId) continue;
                    if (!DateOnly.TryParse(row.GetProperty("EndDate").GetString(), out var end)) continue;
                    var days = end.DayNumber - today.DayNumber;
                    lines.Add($"Gói #{row.GetProperty("PackageId")}: {(days < 0 ? $"đã hết hạn {Math.Abs(days)} ngày" : days == 0 ? "hết hạn hôm nay" : $"còn {days} ngày")}, ngày hết hạn {end:dd/MM/yyyy}.");
                }
                direct = lines.Count > 0 ? $"Gói của hội viên trong cuộc trò chuyện (ID {conversation.MemberId}):\n" + string.Join("\n", lines) : "Không tìm thấy gói của hội viên trong cuộc trò chuyện trong dữ liệu được phép xem.";
            }
            else direct = "Tài khoản hiện không có dữ liệu gói hội viên được phép xem.";
        }
        else if (new[] { "cái gì vậy", "là sao", "không hiểu" }.Contains(question))
            direct = "Xin lỗi, câu trả lời trước chưa rõ. Bạn muốn tôi giải thích giá gói, số ngày còn lại hay lịch tập? Tôi sẽ trả lời bằng tiếng Việt, không dùng JSON.";
        else if (agent == "database" && question.Contains("lịch") && (question.Contains("của tôi") || question.Contains("đã đặt"))
            && await access.HasAsync(p, "bookings.read", ct) && await access.HasAsync(p, "schedules.read", ct))
        {
            // Personal schedules come from confirmed bookings, not the open class catalog.
            var sessions = await db.Bookings.AsNoTracking().Where(x => x.MemberId == conversation.MemberId && x.Status == "Confirmed")
                .Select(x => new { x.WorkoutSchedule.ScheduleDate, x.WorkoutSchedule.StartTime, x.WorkoutSchedule.EndTime,
                    x.WorkoutSchedule.Title, x.WorkoutSchedule.Status, Trainer = x.WorkoutSchedule.Trainer.User.FullName })
                .Where(x => x.Status != "Canceled").OrderBy(x => x.ScheduleDate).ThenBy(x => x.StartTime).ToListAsync(ct);
            if (question.Contains("hôm nay")) sessions = sessions.Where(x => x.ScheduleDate == GymClock.Today).ToList();
            else sessions = sessions.Where(x => x.ScheduleDate >= GymClock.Today).ToList();
            direct = sessions.Count == 0 ? "Không có lịch đã xác nhận phù hợp trong dữ liệu của hội viên này. Bạn có thể xem lịch còn chỗ để đặt buổi mới."
                : "Lịch đã xác nhận của hội viên trong cuộc trò chuyện:\n" + string.Join("\n", sessions.Take(15).Select(x =>
                    $"- {x.ScheduleDate:dd/MM/yyyy}, {x.StartTime:HH\\:mm}–{x.EndTime:HH\\:mm}: {x.Title}; PT {x.Trainer}."));
        }
        prompt += "\nTrả lời bằng tiếng Việt. Khi gợi ý gói, chỉ sử dụng tên và giá trong dữ liệu được cung cấp, nêu thời hạn và số buổi. Phân biệt lịch đã đặt và lịch còn chỗ. Khi gợi ý tập luyện, đưa ra lộ trình tham khảo theo mục tiêu, nhắc người dùng trao đổi với PT trước khi áp dụng, không tự xác nhận đặt lịch hoặc thanh toán.";
        var response = direct ?? await provider.AnswerAsync(prompt, member?.Goal, ct);
        var userMessage = new AIConversationMessage { ConversationId = id, SenderType = "User", Message = message.Trim(), CreatedAt = GymClock.Now };
        var responseProvider = direct is null ? provider.Name : "Tra cứu dữ liệu có phân quyền";
        var reply = new AIConversationMessage { ConversationId = id, SenderType = "Assistant", Message = response,
            ContextData = JsonSerializer.Serialize(new { provider = responseProvider, modelVersion = direct is null ? "qwen3:4b" : "sql-lookup" }), CreatedAt = GymClock.Now };
        db.AddRange(userMessage, reply);
        if ((question.Contains("gợi ý") || question.Contains("đề xuất")) && (question.Contains("lộ trình") || question.Contains("giáo án") || question.Contains("kế hoạch tập")))
            db.AIRecommendations.Add(new AIRecommendation { MemberId = conversation.MemberId, RecommendationType = "Workout", Content = response,
                ReferenceData = JsonSerializer.Serialize(new { conversationId = id, provider = provider.Name, requiresTrainerReview = true }),
                IsRead = false, CreatedAt = GymClock.Now });
        conversation.UpdatedAt = GymClock.Now;
        await db.SaveChangesAsync(ct);
        var type = db.Model.FindEntityType(typeof(AIConversationMessage))!;
        return new
        {
            provider = responseProvider,
            agent,
            userMessage = ResourceDto.From(userMessage, type),
            assistantMessage = ResourceDto.From(reply, type)
        };
    }
    public async Task<object> AcceptAsync(ClaimsPrincipal p, long recommendationId, ConfirmedAiPlan request, CancellationToken ct)
    {
        await access.DemandAsync(p, "ai.write", ct);
        if (!request.Confirmed)
            throw new ApiException(400, "Cần xác nhận giáo án trước khi lưu.");
        if (string.IsNullOrWhiteSpace(request.PlanName) || request.PlanName.Length > 200 || request.Goal?.Length > 500 || request.EndDate < request.StartDate)
            throw new ApiException(400, "Thông tin giáo án không hợp lệ.");
        var recommendation = await db.AIRecommendations.FindAsync(new object[] { recommendationId }, ct) ?? throw new ApiException(404, "Khuyến nghị không tồn tại.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(recommendation))
            throw new ApiException(403, "Khuyến nghị ngoài phạm vi tài khoản.");
        if (request.TrainerId != null && !await db.MemberTrainers.AnyAsync(x => x.MemberId == recommendation.MemberId && x.TrainerId == request.TrainerId && x.Status == "Active", ct))
            throw new ApiException(400, "PT chưa được phân công cho hội viên.");
        var plan = new WorkoutPlan { MemberId = recommendation.MemberId, TrainerId = request.TrainerId, PlanName = request.PlanName, Goal = request.Goal, StartDate = request.StartDate, EndDate = request.EndDate, Note = "[AI demo đã được người dùng xác nhận] " + request.Note, Status = "Active", CreatedAt = GymClock.Now };
        db.Add(plan);
        db.Add(new AuditLog { UserId = long.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "ConfirmAiPlan", EntityName = "WorkoutPlans", CreatedAt = GymClock.Now });
        recommendation.IsRead = true;
        await db.SaveChangesAsync(ct);
        return ResourceDto.From(plan, db.Model.FindEntityType(typeof(WorkoutPlan))!);
    }
    public async Task<object> ChurnAsync(ClaimsPrincipal p, long memberId, CancellationToken ct)
    {
        await access.DemandAsync(p, "churn.write", ct);
        var member = await db.Members.FindAsync(new object[] { memberId }, ct) ?? throw new ApiException(404, "Không tìm thấy hội viên.");
        var allowed = await scope.CreateAsync(p, ct);
        if (!allowed(member))
            throw new ApiException(403, "Hội viên ngoài phạm vi tài khoản.");
        var last = await db.CheckIns.Where(x => x.MemberId == memberId).OrderByDescending(x => x.CheckInTime).Select(x => (DateTime?)x.CheckInTime).FirstOrDefaultAsync(ct);
        var days = last == null ? 30 : Math.Max(0, (GymClock.Now - last.Value).Days);
        var score = 100m * Math.Min(1m, days / 30m);
        var row = new ChurnPrediction { MemberId = memberId, PredictionDate = GymClock.Now, CreatedAt = GymClock.Now, RiskScore = decimal.Round(score, 4), RiskLevel = score >= 70m ? "High" : score >= 40m ? "Medium" : "Low", Factors = $"Demo: số ngày từ check-in gần nhất = {days}. Điểm (0–100) = 100 * min(1, ngày / 30). Không có check-in: 30 ngày.", RecommendedActions = "Liên hệ hội viên để thống nhất lịch tập và mục tiêu.", ModelVersion = "demo-rules-v1-no-ml" };
        db.Add(row);
        await db.SaveChangesAsync(ct);
        return ResourceDto.From(row, db.Model.FindEntityType(typeof(ChurnPrediction))!);
    }
}



public sealed record ConfirmedAiPlan(bool Confirmed, string PlanName, string? Goal, long? TrainerId, DateOnly? StartDate, DateOnly? EndDate, string? Note);


