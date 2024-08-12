using System.Diagnostics;
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

    public async Task<(int WordCount, int TokenCount, long ElapsedMs)> StreamCompletionAsync(string prompt, Action<string> onContent)
    {
        var request = new
        {
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7,
            max_tokens = -1,
            stream = true,
            stream_options = new { include_usage = true }
        };

        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();

        var stopwatch = Stopwatch.StartNew();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var wordCount = 0;
        var tokenCount = 0;

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
                    onContent(text);
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
}