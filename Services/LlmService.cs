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
    private const string OpenWeatherApiKey = "7cc6fd58de105f026ceb58c4c702fa80";

    private readonly HttpClient _client;
    private readonly Dictionary<string, Func<string[], Task<string>>> _tools;
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

The tool's response will be provided in the next message, wrapped in <tool_msg></tool_msg> tags.  You can only use a tool ONCE per interaction.";

    public LlmService(HttpClient client = null)
    {
        if (client == null)
            client = new HttpClient { BaseAddress = new Uri("http://localhost:1234") };

        _client = client;
        _tools = new Dictionary<string, Func<string[], Task<string>>>
        {
            { "get_bitcoin_price", _ => GetBitcoinPriceAsync() },
            { "get_weather", args => GetWeather(args[0]) }
        };
    }

    private async Task<string> GetBitcoinPriceAsync()
    {
        var response = await _client.GetFromJsonAsync<JsonElement>("https://api.coindesk.com/v1/bpi/currentprice.json");
        var price = response.GetProperty("bpi").GetProperty("USD").GetProperty("rate").GetString();
        return $"The current price of Bitcoin is ${price}";
    }

    private async Task<(double Lat, double Lon)> GetCoordinates(string location)
    {
        location = location.Replace("location=", "").Trim('"');
        var response = await _client.GetFromJsonAsync<JsonElement>($"http://api.openweathermap.org/geo/1.0/direct?q={location}&limit=1&appid={OpenWeatherApiKey}");
        var firstResult = response.EnumerateArray().FirstOrDefault();
        if (firstResult.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"Location not found: {location}");

        return (firstResult.GetProperty("lat").GetDouble(), firstResult.GetProperty("lon").GetDouble());
    }

    private async Task<string> GetWeather(string location)
    {
        try
        {
            var (lat, lon) = await GetCoordinates(location);
            var response = await _client.GetFromJsonAsync<JsonElement>($"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&exclude=minutely,hourly,daily,alerts&units=metric&appid={OpenWeatherApiKey}");

            var main = response.GetProperty("main");
            var weather = response.GetProperty("weather")[0];
            var wind = response.GetProperty("wind");

            var temp = main.GetProperty("temp").GetDouble();
            var feelsLike = main.GetProperty("feels_like").GetDouble();
            var humidity = main.GetProperty("humidity").GetInt32();
            var description = weather.GetProperty("description").GetString();
            var windSpeed = wind.GetProperty("speed").GetDouble();
            var cityName = response.GetProperty("name").GetString();
            var country = response.GetProperty("sys").GetProperty("country").GetString();

            return $"Current weather in {cityName}, {country}: " +
                   $"{temp:F1}°C (feels like {feelsLike:F1}°C), {description}. " +
                   $"Humidity: {humidity}%, Wind speed: {windSpeed} m/s.";
        }
        catch (Exception ex)
        {
            return $"Error fetching weather data: {ex.Message}";
        }
    }


public async Task<(int WordCount, int TokenCount, long ElapsedMs)> StreamCompletionAsync(
    string userPrompt,
    string systemPrompt = null,
    Action<string>? onContent = null,
    float temperature = 0.7f,
    int maxTokens = -1)
{
    Debug.WriteLine($"Starting StreamCompletionAsync with prompt: {userPrompt}");
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

    for (int i = 0; i < maxInteractions; i++)
    {
        Debug.WriteLine($"Interaction {i + 1} started");
        var (partialResponse, partialWordCount, partialTokenCount) = await GetLlmResponseAsync(messages, temperature, maxTokens);
        Debug.WriteLine($"Received partial response: {partialResponse}");
        fullResponse.Append(partialResponse);
        wordCount += partialWordCount;
        tokenCount += partialTokenCount;

        var (toolName, parameters) = ParseToolInvocation(partialResponse.Trim());
        Debug.WriteLine($"Parsed tool invocation: Tool={toolName}, Parameters={string.Join(", ", parameters)}");

        if (_tools.ContainsKey(toolName))
        {
            if (toolName == lastToolUsed)
            {
                repeatedToolUseCount++;
                if (repeatedToolUseCount >= 2)
                {
                    Debug.WriteLine("Detected repeated tool use. Forcing final answer.");
                    messages.Add(new { role = "system", content = "You have used the same tool multiple times. Please provide a final answer based on the information you have." });
                    continue;
                }
            }
            else
            {
                repeatedToolUseCount = 0;
            }

            lastToolUsed = toolName;
            Debug.WriteLine($"Executing tool: {toolName}");
            var toolResponse = await _tools[toolName](parameters);
            var wrappedToolResponse = $"<tool_msg>{toolResponse}</tool_msg>";
            Debug.WriteLine($"Tool Response: {wrappedToolResponse}");
            messages.Add(new { role = "assistant", content = partialResponse.Trim() });
            messages.Add(new { role = "user", content = wrappedToolResponse });
        }
        else
        {
            Debug.WriteLine("Received final response");
            onContent?.Invoke(partialResponse);
            break;
        }
    }

    if (fullResponse.Length == 0)
    {
        Debug.WriteLine("Error: No valid response received from the LLM.");
        onContent?.Invoke("Error: No valid response received from the LLM.");
    }

    stopwatch.Stop();
    Debug.WriteLine($"StreamCompletionAsync completed. WordCount={wordCount}, TokenCount={tokenCount}, ElapsedMs={stopwatch.ElapsedMilliseconds}");
    return (wordCount, tokenCount, stopwatch.ElapsedMilliseconds);
}
    private (string ToolName, string[] Parameters) ParseToolInvocation(string input)
    {
        var match = Regex.Match(input.Trim(), @"^(\w+)(?:\((.*?)\))?$");
        if (match.Success)
        {
            var toolName = match.Groups[1].Value;
            var parameters = match.Groups[2].Success
                ? match.Groups[2].Value.Split(',').Select(p => p.Trim()).ToArray()
                : Array.Empty<string>();
            return (toolName, parameters);
        }
        return (string.Empty, Array.Empty<string>());
    }

    private async Task<(string Response, int WordCount, int TokenCount)> GetLlmResponseAsync(
        List<object> messages,
        float temperature,
        int maxTokens)
    {
        var request = new
        {
            messages,
            temperature,
            max_tokens = maxTokens,
            stream = false
        };

        var response = await _client.PostAsJsonAsync("/v1/chat/completions", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        var usage = result.GetProperty("usage");
        var tokenCount = usage.GetProperty("total_tokens").GetInt32();
        
        return (content, content.Split().Length, tokenCount);
    }

    public async Task<List<Model>> GetModelsAsync()
    {
        var response = await _client.GetAsync("/v1/models");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ModelResponse>();
        return result?.Data ?? new List<Model>();
    }



    #region  Other Methods

   
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

    #endregion


   
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