using GymHung.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace GymManagement.Api.Controllers;

[ApiController, Authorize, Route("api/ai")]
public sealed class AiAssistantController(AiService service, WorkspaceService workspace, IConfiguration configuration) : ControllerBase
{
    [HttpPost("documents"), Authorize(Policy = "ai.write"), RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> UploadDocuments(List<IFormFile> files, CancellationToken ct)
    {
        if (files.Count is < 1 or > 5 || files.Any(f => f.Length > 10 * 1024 * 1024 || !f.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { message = "Chọn tối đa 5 PDF, mỗi file không quá 10 MB." });
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var content = new StreamContent(file.OpenReadStream());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            form.Add(content, "files", Path.GetFileName(file.FileName));
        }
        try
        {
            using var response = await http.PostAsync(configuration["Ai:PdfIngestUrl"] ?? "http://127.0.0.1:3000/api/ingest", form, ct);
            if (!response.IsSuccessStatusCode) return StatusCode(502, new { message = "Không xử lý được PDF. Kiểm tra file có văn bản và dịch vụ chatbot đang chạy." });
            return Ok(new { message = "Đã đọc PDF. Bạn có thể hỏi về tài liệu ngay trong cuộc trò chuyện này." });
        }
        catch (HttpRequestException) { return StatusCode(503, new { message = "Không kết nối được dịch vụ đọc PDF local." }); }
    }
    [HttpPost("conversations/{conversationId:long}/messages")] public async Task<IActionResult> SendMessage(long conversationId, ChatRequest request, CancellationToken ct) => Ok(await service.SendAsync(User, conversationId, request.Message, ct));
    [HttpPost("churn/{memberId:long}")] public async Task<IActionResult> Churn(long memberId, CancellationToken ct) => Ok(await service.ChurnAsync(User, memberId, ct));
    [HttpPost("recommendations/{id:long}/accept-plan")] public async Task<IActionResult> Accept(long id, ConfirmedAiPlan request, CancellationToken ct) => Ok(await service.AcceptAsync(User, id, request, ct));
    [HttpPost("plans/confirm")]
    public async Task<IActionResult> Confirm(ConfirmPlanRequest request, CancellationToken ct)
    {
        if (!request.Confirmed)
            throw new ApiException(400, "Cần xác nhận giáo án trước khi lưu.");
        return Ok(await workspace.SaveAsync(User, "WorkoutPlans", null, new MutationRequest(request.Draft), ct));
    }
    public sealed record ChatRequest(string? Message); public sealed record ConfirmPlanRequest(bool Confirmed, System.Text.Json.JsonElement Draft);
}

