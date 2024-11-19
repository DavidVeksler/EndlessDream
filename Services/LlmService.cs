using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
//Supported payload parameters
//For an explanation for each parameter, see https://platform.openai.com/docs/api-reference/chat/create.
//model
//top_p
//top_k
//messages
//temperature
//max_tokens
//stream
//stop
//presence_penalty
//frequency_penalty
//logit_bias
//repeat_penalty
//seed

//You are an AI assistant with access to external tools:
//- get_bitcoin_price: Retrieves the current price of Bitcoin

//To use a tool, you MUST respond with the tool name (e.g., 'get_bitcoin_price') ONLY.  
//    The tool response will be provided to you in the next message, wrapped in <tool_msg></tool_msg> tags. 

//    VERY VERY IMPORTANT:
//1. DO NOT GUESS THE TOOL'S RESPONSE.  DO NOT RESPOND WITH THE TOOL'S RESPONSE.  DO NOT RESPOND WITH <tool_msg>.  DO NOT RESPOND WITH <tool_msg>
//2. Do not repeat or mention the <tool_msg> tags in your responses to the user.
//3. After receiving tool information, incorporate it naturally into your response without explicitly stating that you used a tool.
//4: DO NOT GBUESS OR SPECULATE
//5. If you don't need to use a tool, simply respond to the user's query directly.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public class LlmService
{
    private const string SystemPromptTemplate = @"You are an AI assistant.
IF NEEDED,use external tools:
- get_bitcoin_price: Retrieves the current price of Bitcoin in USD.
- get_weather(location): Retrieves the current weather for the specified location.
- scrape_webpage(url): Scrapes the webpage at the given URL and returns its title, description, and a preview of the body content.

To use a tool, respond with the tool name followed by parameters in parentheses, if any. 
- get_bitcoin_price
- get_weather(London)
- scrape_webpage(https://example.com)

After tools, you MUST provide a final answer to the user's query.
Your final answer should not be a tool invocation.

Respond ONLY with either:
1. A tool invocation
2. Your final answer to the user's query

The tool's response will be provided in the next message, wrapped in <tool_msg></tool_msg> tags. Only use a tool ONCE per interaction.";

    private readonly HttpClient _client;
    private readonly ToolManager _toolManager;

    public LlmService(HttpClient client = null)
    {
        _client = client ?? new HttpClient { BaseAddress = new Uri("http://192.168.1.250:1234") };
        _toolManager = new ToolManager();
    }

    public async Task<(int WordCount, int TokenCount, long ElapsedMs)> StreamCompletionAsync(
        string userPrompt,
        string systemPrompt = null,
        Func<string, Task> onContentAsync = null,
        float temperature = 0.7f,
        int maxTokens = -1)
    {
        var messages = new List<object>
        {
            new { role = "system", content = SystemPromptTemplate + systemPrompt },
            new { role = "user", content = userPrompt }
        };

        var wordCount = 0;
        var tokenCount = 0;
        var stopwatch = Stopwatch.StartNew();
        var fullResponse = new StringBuilder();

        await GetLlmResponseAsync(messages, temperature, maxTokens, async content =>
        {
            fullResponse.Append(content);
            wordCount += content.Split().Length;
            if (onContentAsync != null)
            {
                await onContentAsync(content);
            }
        });

        stopwatch.Stop();
        return (wordCount, tokenCount, stopwatch.ElapsedMilliseconds);
    }

    private async Task GetLlmResponseAsync(
        List<object> messages,
        float temperature,
        int maxTokens,
        Func<string, Task> onContentAsync)
    {
        var request = new { messages, temperature, max_tokens = maxTokens, stream = true };

        // Create an HttpRequestMessage
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(request)
        };

        // Send the request with HttpCompletionOption.ResponseHeadersRead
        var response = await _client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);

                if (json.Trim() == "[DONE]")
                    break;

                var parsed = JsonSerializer.Deserialize<JsonElement>(json);

                if (parsed.TryGetProperty("choices", out var choices))
                {
                    var delta = choices[0].GetProperty("delta");

                    if (delta.TryGetProperty("content", out var contentElement))
                    {
                        var content = contentElement.GetString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            if (onContentAsync != null)
                            {
                                await onContentAsync(content);
                            }
                        }
                    }
                }
            }
        }
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
