using System.Net.Http.Json;
using System.Text.Json;
namespace GymHung.Application;

public sealed class OllamaAiProvider(HttpClient client) : IAiProvider
{
    public string Name => "Ollama · qwen3:4b";
    public async Task<string> AnswerAsync(string message, string? goal, CancellationToken ct)
    {
        try
        {
            using var threadResponse = await client.PostAsJsonAsync("threads", new { }, ct);
            threadResponse.EnsureSuccessStatusCode();
            var thread = await threadResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
            var threadId = thread.GetProperty("thread_id").GetString();
            using var response = await client.PostAsJsonAsync($"threads/{threadId}/runs/wait", new
            {
                assistant_id = "retrieval_graph",
                input = new { query = message },
                config = new { configurable = new { queryModel = "ollama/qwen3:4b", retrieverProvider = "local", k = 5 } }
            }, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (!result.TryGetProperty("messages", out var messages) || messages.GetArrayLength() == 0)
                throw new ApiException(502, "Ollama chưa trả về câu trả lời. Vui lòng thử lại.");
            var answer = messages[messages.GetArrayLength() - 1].GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(answer)) throw new ApiException(502, "Ollama trả về nội dung trống.");
            return answer;
        }
        catch (HttpRequestException)
        {
            throw new ApiException(503, "Không kết nối được chatbot local. Hãy chạy Ollama và backend chatbot ở cổng 2024.");
        }
    }
}

