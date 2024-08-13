public class ToolManager 
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolManager()
    {
        _tools = new Dictionary<string, ITool>
        {
            { "get_bitcoin_price", new BitcoinPriceTool() },
            { "get_weather", new WeatherTool() },
            { "scrape_webpage", new WebScrapingTool() },
            
        };
    }

    public bool HasTool(string toolName) => _tools.ContainsKey(toolName);

    public async Task<string> ExecuteToolAsync(string toolName, string[] parameters) =>
        _tools.TryGetValue(toolName, out var tool) ? await tool.ExecuteAsync(parameters) : "Tool not found";
}