using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public class LlmService
{
    private const string DEFAULT_LOCAL_URL = "http://dream.davidveksler.com:1234";
    private const string REMOTE_URL = "http://73.122.182.81:1234";
    private readonly Dictionary<string, AIEndpoint> _endpoints;
    private readonly HttpClient _httpClient;

    public LlmService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        _endpoints = new Dictionary<string, AIEndpoint>
        {
            //["goal-setting"] = AIEndpoint.CreateCustomService(
            //    "goal-setting",
            //    "Goal Setting AI",
            //    "Helps set and track your goals",
            //    "http://localhost:8001"
            //),
            //["coaching"] = AIEndpoint.CreateCustomService(
            //    "coaching",
            //    "Coaching AI",
            //    "Provides ongoing coaching and support",
            //    "http://localhost:8002"
            //)
        };
    }

    public async Task StreamCompletionAsync(
        List<Message> messages,
        string systemPrompt,
        Func<string, Task> onContent,
        float temperature = 0.7f,
        int maxTokens = 99999,
        string endpointId = "goal-setting"
    )
    {
        if (!_endpoints.TryGetValue(endpointId, out var endpoint))
            throw new ArgumentException($"Unknown endpoint ID: {endpointId}");

        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = messages.Select(m => new
            {
                role = m.IsUser ? "user" : "assistant",
                content = m.Content
            }).Prepend(new
            {
                role = "system",
                content = endpoint.IsCustomService
                    ? $"{systemPrompt}\nYou are the {endpoint.Name}. {endpoint.Description}"
                    : systemPrompt
            }).ToList(),
            ["temperature"] = temperature,
            ["stream"] = true
        };

        if (!endpoint.IsCustomService && !string.IsNullOrEmpty(endpoint.ModelId))
            requestBody["model"] = endpoint.ModelId;

        if (maxTokens > 0) 
            requestBody["max_tokens"] = endpoint.IsCustomService ? 15000 : maxTokens;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint.EndpointUrl}/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            )
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data:")) continue;
            if (line == "data: [DONE]") break;

            try
            {
                var data = line.Substring(5).Trim();
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(data);

                if (jsonElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("delta", out var delta) &&
                        delta.TryGetProperty("content", out var content))
                    {
                        var contentString = content.GetString();
                        if (!string.IsNullOrEmpty(contentString)) 
                            await onContent(contentString);
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}\nLine: {line}");
            }
        }
    }

    // Get only local models
    public IEnumerable<AIEndpoint> GetLocalModels()
    {
        return _endpoints.Values.Where(e => !e.IsCustomService && e.Source == "local");
    }

    // Get only remote models
    public IEnumerable<AIEndpoint> GetRemoteModels()
    {
        return _endpoints.Values.Where(e => !e.IsCustomService && e.Source == "remote");
    }

    // Get custom services
    public IEnumerable<AIEndpoint> GetCustomServices()
    {
        return _endpoints.Values.Where(e => e.IsCustomService);
    }

    // Get all endpoints
    public IEnumerable<AIEndpoint> GetAllEndpoints()
    {
        return _endpoints.Values;
    }

    public async Task LoadLocalModels()
    {
        await LoadModelsFromUrl(DEFAULT_LOCAL_URL, "local");
        await LoadModelsFromUrl(REMOTE_URL, "remote");
    }

    private async Task LoadModelsFromUrl(string baseUrl, string source)
    {
        try
        {
            using var tempClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var response = await tempClient.GetAsync("/v1/models");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ModelResponse>();
            if (result?.Data == null) return;

            foreach (var model in result.Data)
            {
                var endpoint = AIEndpoint.CreateModel(model, baseUrl, source);
                _endpoints[endpoint.Id] = endpoint;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load models from {baseUrl} ({source}): {ex.Message}");
        }
    }
}