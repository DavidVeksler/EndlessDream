using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:1234") };
var request = new
{
    messages = new[] { new { role = "user", content = "How do I init and update a git submodule?" } },
    temperature = 0.7,
    max_tokens = -1,
    stream = true,
    stream_options = new { include_usage = true }
};

Console.WriteLine("🚀 Sending request to LLM...");
var stopwatch = Stopwatch.StartNew();
var response = await client.PostAsJsonAsync("/v1/chat/completions", request);
response.EnsureSuccessStatusCode();

Console.WriteLine("📡 Receiving stream...\n");
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
            Console.Write(text);
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
Console.WriteLine($"\n\n📊 Stats: {wordCount} words, {tokenCount} tokens, {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine("✅ Response complete!");