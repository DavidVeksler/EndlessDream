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

public class LlmService
{
    private const string SystemPromptTemplate = @"You are an AI assistant.
IF NEEDED,use external tools:
- get_bitcoin_price: Retrieves the current price of Bitcoin in USD.
- get_weather(location): Retrieves the current weather for the specified location.

To use a tool, respond with the tool name followed by parameters in parentheses, if any. For example:
- get_bitcoin_price
- get_weather(location)

After using the necessary tools, you MUST provide a final answer to the user's query.
Your final answer should not be a tool invocation.

Respond ONLY with either:
1. A tool invocation
2. Your final answer to the user's query

The tool's response will be provided in the next message, wrapped in <tool_msg></tool_msg> tags. Only use a tool ONCE per interaction.";

    private readonly HttpClient _client;
    private readonly ToolManager _toolManager;

    public LlmService(HttpClient client = null)
    {
        _client = client ?? new HttpClient { BaseAddress = new Uri("http://localhost:1234") };
        _toolManager = new ToolManager();
    }

    public async Task<(int WordCount, int TokenCount, long ElapsedMs)> StreamCompletionAsync(
        string userPrompt,
        string systemPrompt = null,
        Action<string> onContent = null,
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

        const int maxInteractions = 5;
        var lastToolUsed = "";
        var repeatedToolUseCount = 0;

        for (var i = 0; i < maxInteractions; i++)
        {
            var (partialResponse, partialWordCount, partialTokenCount) =
                await GetLlmResponseAsync(messages, temperature, maxTokens);
            fullResponse.Append(partialResponse);
            wordCount += partialWordCount;
            tokenCount += partialTokenCount;

            var (toolName, parameters) = ParseToolInvocation(partialResponse.Trim());

            if (_toolManager.HasTool(toolName))
            {
                if (toolName == lastToolUsed)
                {
                    repeatedToolUseCount++;
                    if (repeatedToolUseCount >= 2)
                    {
                        messages.Add(new
                        {
                            role = "system",
                            content =
                                "You have used the same tool multiple times. Please provide a final answer based on the information you have."
                        });
                        continue;
                    }
                }
                else
                {
                    repeatedToolUseCount = 0;
                }

                lastToolUsed = toolName;
                var toolResponse = await _toolManager.ExecuteToolAsync(toolName, parameters);
                var wrappedToolResponse = $"<tool_msg>{toolResponse}</tool_msg>";
                messages.Add(new { role = "assistant", content = partialResponse.Trim() });
                messages.Add(new { role = "user", content = wrappedToolResponse });
            }
            else
            {
                onContent?.Invoke(partialResponse);
                break;
            }
        }

        if (fullResponse.Length == 0) onContent?.Invoke("Error: No valid response received from the LLM.");

        stopwatch.Stop();
        return (wordCount, tokenCount, stopwatch.ElapsedMilliseconds);
    }

    private (string ToolName, string[] Parameters) ParseToolInvocation(string input)
    {
        return Regex.Match(input.Trim(), @"^(\w+)(?:\((.*?)\))?$") is { Success: true } match
            ? (match.Groups[1].Value,
                match.Groups[2].Success
                    ? match.Groups[2].Value.Split(',').Select(p => p.Trim()).ToArray()
                    : Array.Empty<string>())
            : (string.Empty, Array.Empty<string>());
    }

    private async Task<(string Response, int WordCount, int TokenCount)> GetLlmResponseAsync(
        List<object> messages,
        float temperature,
        int maxTokens)
    {
        var request = new { messages, temperature, max_tokens = maxTokens, stream = false };
        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        var tokenCount = result.GetProperty("usage").GetProperty("total_tokens").GetInt32();

        return (content, content.Split().Length, tokenCount);
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