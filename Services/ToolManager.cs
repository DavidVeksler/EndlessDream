using Microsoft.Extensions.Logging;

/// <summary>
/// Manages registration and execution of tools available to the LLM.
/// Supports dynamic tool registration and discovery.
/// </summary>
public class ToolManager
{
    private readonly Dictionary<string, ITool> _tools;
    private readonly ILogger<ToolManager> _logger;

    public ToolManager(
        BitcoinPriceTool bitcoinPriceTool,
        WeatherTool weatherTool,
        WebScrapingTool webScrapingTool,
        ILogger<ToolManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _tools = new Dictionary<string, ITool>
        {
            { "get_bitcoin_price", bitcoinPriceTool ?? throw new ArgumentNullException(nameof(bitcoinPriceTool)) },
            { "get_weather", weatherTool ?? throw new ArgumentNullException(nameof(weatherTool)) },
            { "scrape_webpage", webScrapingTool ?? throw new ArgumentNullException(nameof(webScrapingTool)) }
        };

        _logger.LogInformation("ToolManager initialized with {ToolCount} tools", _tools.Count);
    }

    /// <summary>
    /// Checks if a tool is registered.
    /// </summary>
    public bool HasTool(string toolName)
    {
        var exists = _tools.ContainsKey(toolName);
        if (!exists)
        {
            _logger.LogWarning("Tool not found: {ToolName}", toolName);
        }
        return exists;
    }

    /// <summary>
    /// Gets all available tool names.
    /// </summary>
    public IEnumerable<string> GetAvailableTools() => _tools.Keys;

    /// <summary>
    /// Executes a tool with the provided parameters.
    /// </summary>
    public async Task<string> ExecuteToolAsync(string toolName, string[] parameters)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            var error = $"Tool not found: {toolName}";
            _logger.LogWarning(error);
            return error;
        }

        try
        {
            _logger.LogDebug("Executing tool: {ToolName}", toolName);
            var result = await tool.ExecuteAsync(parameters);
            _logger.LogDebug("Tool execution completed: {ToolName}", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool execution failed: {ToolName}", toolName);
            return $"Error executing tool {toolName}: {ex.Message}";
        }
    }
}
