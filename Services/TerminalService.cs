﻿using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public class TerminalService
{
    private const string DEFAULT_ENDPOINT = "http://dream.davidveksler.com:1234";
    private readonly HttpClient _httpClient;
    private readonly string _systemPrompt;

    public TerminalService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _systemPrompt = @"You are an emulator for an Ubuntu 22.04 LTS terminal session. You must behave exactly like a Linux terminal, maintaining working directory state and environment variables between commands. 

Every response must be wrapped in <terminal_output> tags and contain:
1. The current working directory path
2. The username (default: user)
3. The hostname (default: ubuntu)
4. The command output with proper formatting

Key behaviors:
- Maintain state between commands including working directory, environment variables, and file system
- Support all standard bash commands (cd, ls, pwd, mkdir, etc.)
- Show command output exactly as it would appear in a real terminal
- Handle errors with proper error messages and return codes
- Support basic text editing commands
- Support command history
- Support tab completion hints
- Support pipe operations and redirections
- Initialize with home directory at /home/user

Critical system events:
- For destructive commands like `rm -rf /`, respond with realistic system failure messages
- On kernel panics, display the characteristic panic dump and hang
- When filesystem corruption occurs, show realistic ext4 error messages
- For memory overflow, display appropriate OOM killer messages
- On segmentation faults, show the classic crash dump with register states
- Buffer overflow attempts should trigger realistic protection mechanisms";
    }

    public async Task StreamCommandAsync(
        List<Message> history,
        Func<string, Task> onContent,
        float temperature = 0.2f,
        int maxTokens = 2000
    )
    {
        var requestBody = new Dictionary<string, object>
        {
            ["messages"] = history.Select(m => new
            {
                role = m.IsUser ? "user" : "assistant",
                content = m.Content
            }).Prepend(new
            {
                role = "system",
                content = _systemPrompt
            }).ToList(),
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens,
            ["stream"] = true
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{DEFAULT_ENDPOINT}/v1/chat/completions")
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

        var terminalOutputStarted = false;
        var fullResponse = new StringBuilder();

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
                            fullResponse.Append(contentString);
                            var currentResponse = fullResponse.ToString();

                            // Check for terminal output tags
                            if (!terminalOutputStarted && !currentResponse.Contains("<terminal_output>"))
                            {
                                await onContent("STICK TO THE SCRIPT: Please provide output in <terminal_output> tags\n\n");
                                continue;
                            }

                            // Track if we've started terminal output
                            if (contentString.Contains("<terminal_output>"))
                            {
                                terminalOutputStarted = true;
                            }

                            await onContent(contentString);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}\nLine: {line}");
                await onContent($"Error: Failed to parse terminal output - {ex.Message}");
            }
        }

        // If no terminal output tags were found in the entire response
        if (!terminalOutputStarted)
        {
            await onContent("\nSTICK TO THE SCRIPT: Response must be wrapped in <terminal_output> tags");
        }
    }

    // Environment tracking methods
    private Dictionary<string, string> Environment { get; set; } = new()
    {
        ["HOME"] = "/home/user",
        ["PWD"] = "/home/user",
        ["USER"] = "user",
        ["SHELL"] = "/bin/bash",
        ["TERM"] = "xterm-256color",
        ["PATH"] = "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
    };

    public void UpdateEnvironmentVariable(string key, string value)
    {
        Environment[key] = value;
    }

    public string GetEnvironmentVariable(string key)
    {
        return Environment.TryGetValue(key, out var value) ? value : "";
    }

    // Virtual filesystem state
    private Dictionary<string, VirtualFile> FileSystem { get; set; } = new();

    public class VirtualFile
    {
        public bool IsDirectory { get; set; }
        public string Content { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Owner { get; set; }
        public string Group { get; set; }
        public string Permissions { get; set; }
    }

    public void CreateFile(string path, VirtualFile file)
    {
        FileSystem[path] = file;
    }

    public VirtualFile GetFile(string path)
    {
        return FileSystem.TryGetValue(path, out var file) ? file : null;
    }

    public void DeleteFile(string path)
    {
        FileSystem.Remove(path);
    }

    public IEnumerable<string> ListDirectory(string path)
    {
        return FileSystem.Keys.Where(k => k.StartsWith(path));
    }
}