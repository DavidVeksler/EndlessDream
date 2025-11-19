using System.Text;
using System.Text.Json;
using EndlessDreamBlazorWeb.Data;

/// <summary>
/// Service for emulating an Ubuntu terminal using an LLM backend.
/// Maintains state between commands including working directory and environment variables.
/// </summary>
public class TerminalService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;
    private readonly ILogger<TerminalService> _logger;
    private readonly Dictionary<string, string> _environment;
    private readonly Dictionary<string, VirtualFile> _fileSystem;

    private const string SystemPrompt = @"You are an emulator for an Ubuntu 22.04 LTS terminal session. Your responses MUST BE EXACTLY like a real terminal:

user@ubuntu:/current/path$  command output goes here

Return errors for any prompt which is not a valid Ubuntu terminal command.";

    public TerminalService(HttpClient httpClient, AppConfiguration config, ILogger<TerminalService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _environment = InitializeEnvironment();
        _fileSystem = new Dictionary<string, VirtualFile>();
    }

    /// <summary>
    /// Streams terminal command execution response from the LLM backend.
    /// </summary>
    public async Task StreamCommandAsync(
        List<Message> history,
        Func<string, Task> onContent,
        float temperature = 0.2f,
        int maxTokens = 2000)
    {
        var requestBody = BuildRequestBody(history, temperature, maxTokens);
        await StreamResponseAsync(requestBody, onContent);
    }

    /// <summary>
    /// Updates an environment variable.
    /// </summary>
    public void UpdateEnvironmentVariable(string key, string value)
    {
        _environment[key] = value;
        _logger.LogDebug("Updated environment variable: {Key}={Value}", key, value);
    }

    /// <summary>
    /// Gets an environment variable value.
    /// </summary>
    public string GetEnvironmentVariable(string key) =>
        _environment.TryGetValue(key, out var value) ? value : string.Empty;

    /// <summary>
    /// Creates a virtual file.
    /// </summary>
    public void CreateFile(string path, VirtualFile file)
    {
        _fileSystem[path] = file;
        _logger.LogDebug("Created virtual file: {Path}", path);
    }

    /// <summary>
    /// Gets a virtual file.
    /// </summary>
    public VirtualFile? GetFile(string path) =>
        _fileSystem.TryGetValue(path, out var file) ? file : null;

    /// <summary>
    /// Deletes a virtual file.
    /// </summary>
    public void DeleteFile(string path)
    {
        _fileSystem.Remove(path);
        _logger.LogDebug("Deleted virtual file: {Path}", path);
    }

    /// <summary>
    /// Lists files in a directory.
    /// </summary>
    public IEnumerable<string> ListDirectory(string path) =>
        _fileSystem.Keys.Where(k => k.StartsWith(path));

    private static Dictionary<string, string> InitializeEnvironment() =>
        new()
        {
            ["HOME"] = "/home/user",
            ["PWD"] = "/home/user",
            ["USER"] = "user",
            ["SHELL"] = "/bin/bash",
            ["TERM"] = "xterm-256color",
            ["PATH"] = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
        };

    private static Dictionary<string, object> BuildRequestBody(
        List<Message> history,
        float temperature,
        int maxTokens)
    {
        return new Dictionary<string, object>
        {
            ["messages"] = history
                .Select(m => new
                {
                    role = m.IsUser ? "user" : "assistant",
                    content = m.Content
                })
                .Prepend(new
                {
                    role = "system",
                    content = SystemPrompt
                })
                .ToList(),
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens,
            ["stream"] = true
        };
    }

    private async Task StreamResponseAsync(
        Dictionary<string, object> requestBody,
        Func<string, Task> onContent)
    {
        var endpointUrl = _config.TerminalBaseUrl;
        if (string.IsNullOrWhiteSpace(endpointUrl))
        {
            _logger.LogError("Terminal endpoint URL not configured");
            throw new InvalidOperationException("Terminal endpoint URL not configured");
        }

        try
        {
            _logger.LogDebug("Streaming terminal command to: {Endpoint}", endpointUrl);
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

            await StreamLinesAsync(reader, onContent);

            // Ensure prompt appears if no content was received
            await onContent("\nuser@ubuntu:~$ ");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Terminal endpoint request failed");
            throw;
        }
    }

    private static async Task StreamLinesAsync(StreamReader reader, Func<string, Task> onContent)
    {
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
                // Log JSON parsing errors but continue
                System.Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Represents a virtual file in the terminal filesystem.
    /// </summary>
    public class VirtualFile
    {
        public bool IsDirectory { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Modified { get; set; } = DateTime.Now;
        public string Owner { get; set; } = "user";
        public string Group { get; set; } = "user";
        public string Permissions { get; set; } = "644";
    }
}
