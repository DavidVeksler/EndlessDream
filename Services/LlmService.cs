using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EndlessDreamBlazorWeb.Data;

/// <summary>
/// Service for managing LLM endpoint communication with streaming support.
/// Handles multiple AI model endpoints (local, remote, custom services).
/// </summary>
public class LlmService
{
    private readonly Dictionary<string, AIEndpoint> _endpoints;
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;
    private readonly ILogger<LlmService> _logger;

    public LlmService(HttpClient httpClient, AppConfiguration config, ILogger<LlmService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _endpoints = new Dictionary<string, AIEndpoint>();
    }

    /// <summary>
    /// Streams completion response from the specified LLM endpoint.
    /// </summary>
    public async Task StreamCompletionAsync(
        List<Message> messages,
        string systemPrompt,
        Func<string, Task> onContent,
        float temperature = 0.7f,
        int maxTokens = 99999,
        string endpointId = "goal-setting")
    {
        if (!_endpoints.TryGetValue(endpointId, out var endpoint))
        {
            throw new ArgumentException($"Unknown endpoint ID: {endpointId}");
        }

        var requestBody = BuildRequestBody(messages, systemPrompt, endpoint, temperature, maxTokens);
        await StreamResponseAsync(endpoint.EndpointUrl, requestBody, onContent);
    }

    /// <summary>
    /// Gets all local models from configured endpoints.
    /// </summary>
    public IEnumerable<AIEndpoint> GetLocalModels() =>
        _endpoints.Values.Where(e => !e.IsCustomService && e.Source == "local");

    /// <summary>
    /// Gets all remote models from configured endpoints.
    /// </summary>
    public IEnumerable<AIEndpoint> GetRemoteModels() =>
        _endpoints.Values.Where(e => !e.IsCustomService && e.Source == "remote");

    /// <summary>
    /// Gets all custom service endpoints.
    /// </summary>
    public IEnumerable<AIEndpoint> GetCustomServices() =>
        _endpoints.Values.Where(e => e.IsCustomService);

    /// <summary>
    /// Gets all registered endpoints.
    /// </summary>
    public IEnumerable<AIEndpoint> GetAllEndpoints() =>
        _endpoints.Values;

    /// <summary>
    /// Loads available models from configured LLM endpoints.
    /// </summary>
    public async Task LoadLocalModels()
    {
        await LoadModelsFromUrlAsync(_config.LlmLocalEndpoint, "local");
        await LoadModelsFromUrlAsync(_config.LlmRemoteEndpoint, "remote");
    }

    /// <summary>
    /// Loads models from a specific endpoint URL.
    /// </summary>
    private async Task LoadModelsFromUrlAsync(string baseUrl, string source)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("Endpoint URL is not configured for source: {Source}", source);
            return;
        }

        try
        {
            _logger.LogDebug("Loading models from {Source} endpoint: {Url}", source, baseUrl);
            using var tempClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var response = await tempClient.GetAsync("/v1/models");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ModelResponse>();
            if (result?.Data == null || result.Data.Count == 0)
            {
                _logger.LogWarning("No models returned from {Source} endpoint", source);
                return;
            }

            foreach (var model in result.Data)
            {
                var endpoint = AIEndpoint.CreateModel(model, baseUrl, source);
                _endpoints[endpoint.Id] = endpoint;
                _logger.LogDebug("Registered model: {ModelId} from {Source}", endpoint.Id, source);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models from {Source} endpoint: {Url}", source, baseUrl);
        }
    }

    /// <summary>
    /// Builds the request body for an LLM API call.
    /// </summary>
    private static Dictionary<string, object> BuildRequestBody(
        List<Message> messages,
        string systemPrompt,
        AIEndpoint endpoint,
        float temperature,
        int maxTokens)
    {
        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = messages
                .Select(m => new
                {
                    role = m.IsUser ? "user" : "assistant",
                    content = m.Content
                })
                .Prepend(new
                {
                    role = "system",
                    content = endpoint.IsCustomService
                        ? $"{systemPrompt}\nYou are the {endpoint.Name}. {endpoint.Description}"
                        : systemPrompt
                })
                .ToList(),
            ["temperature"] = temperature,
            ["stream"] = true
        };

        if (!endpoint.IsCustomService && !string.IsNullOrEmpty(endpoint.ModelId))
        {
            requestBody["model"] = endpoint.ModelId;
        }

        if (maxTokens > 0)
        {
            requestBody["max_tokens"] = endpoint.IsCustomService ? 15000 : maxTokens;
        }

        return requestBody;
    }

    /// <summary>
    /// Streams response data from an LLM endpoint, parsing SSE format.
    /// </summary>
    private async Task StreamResponseAsync(
        string endpointUrl,
        Dictionary<string, object> requestBody,
        Func<string, Task> onContent)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpointUrl}/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            )
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead);

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
                        {
                            await onContent(contentString);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Error parsing JSON from LLM stream");
            }
        }
    }
}
