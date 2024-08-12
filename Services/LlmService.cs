using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public class LlmService
{
    private readonly HttpClient _client;

    public LlmService(string baseUrl = "http://localhost:1234")
    {
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

   public async Task<(int WordCount, int TokenCount, long ElapsedMs)> StreamCompletionAsync(
        string userPrompt,
        string? systemPrompt = null,
        Action<string>? onContent = null,
        float temperature = 0.7f,
        int maxTokens = -1)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(new { role = "system", content = systemPrompt });
        }
        messages.Add(new { role = "user", content = userPrompt });

        var request = new
        {
            messages,
            temperature,
            max_tokens = maxTokens,
            stream = true,
            stream_options = new { include_usage = true }
        };

        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var wordCount = 0;
        var tokenCount = 0;
        var sb = new StringBuilder();

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!line.StartsWith("data: ")) continue;
            var data = line[6..];
            if (data == "[DONE]") break;

            try
            {
                var chunk = JsonSerializer.Deserialize<JsonElement>(data);
                var delta = chunk.GetProperty("choices")[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var content))
                {
                    var text = content.GetString() ?? string.Empty;
                    sb.Append(text);
                    onContent?.Invoke(text);
                    wordCount += text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                }
                if (chunk.TryGetProperty("usage", out var usage) && usage.ValueKind != JsonValueKind.Null)
                {
                    tokenCount = usage.GetProperty("total_tokens").GetInt32();
                }
            }
            catch (JsonException) { }
        }

        stopwatch.Stop();
        return (wordCount, tokenCount, stopwatch.ElapsedMilliseconds);
    }
    public async Task<List<Model>> GetModelsAsync()
    {
        var response = await _client.GetAsync("/v1/models");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ModelResponse>();
        return result?.Data ?? new List<Model>();
    }

    public async Task<EmbeddingResponse> CreateEmbeddingAsync(string input)
    {
        var request = new { input, model = "text-embedding-ada-002" };
        var response = await _client.PostAsJsonAsync("/v1/embeddings", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EmbeddingResponse>() ?? new EmbeddingResponse();
    }

    public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request)
    {
        var response = await _client.PostAsJsonAsync("/v1/completions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CompletionResponse>() ?? new CompletionResponse();
    }
}

public class Model
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public string OwnedBy { get; set; } = "";
    public List<object> Permission { get; set; } = new();
}

public class ModelResponse
{
    public List<Model> Data { get; set; } = new();
    public string Object { get; set; } = "";
}

public class EmbeddingResponse
{
    public List<Embedding> Data { get; set; } = new();
    public string Model { get; set; } = "";
    public Usage Usage { get; set; } = new();
}

public class Embedding
{
    public List<float> embedding { get; set; } = new();
    public int Index { get; set; }
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class CompletionRequest
{
    public string Model { get; set; } = "";
    public string Prompt { get; set; } = "";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 16;
    public float TopP { get; set; } = 1;
    public float FrequencyPenalty { get; set; } = 0;
    public float PresencePenalty { get; set; } = 0;
}

public class CompletionResponse
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public long Created { get; set; }
    public string Model { get; set; } = "";
    public List<Choice> Choices { get; set; } = new();
    public Usage Usage { get; set; } = new();
}

public class Choice
{
    public string Text { get; set; } = "";
    public int Index { get; set; }
    public object Logprobs { get; set; } = new();
    public string FinishReason { get; set; } = "";
}