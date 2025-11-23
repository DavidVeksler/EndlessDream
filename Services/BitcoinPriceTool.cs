using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tool for fetching current Bitcoin price from CoinDesk API.
/// Provides real-time BTC/USD pricing without authentication.
/// </summary>
public class BitcoinPriceTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BitcoinPriceTool> _logger;
    private const string ApiUrl = "https://api.coindesk.com/v1/bpi/currentprice.json";

    public BitcoinPriceTool(HttpClient httpClient, ILogger<BitcoinPriceTool> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes bitcoin price lookup.
    /// </summary>
    public async Task<string> ExecuteAsync(string[] parameters)
    {
        try
        {
            _logger.LogDebug("Fetching Bitcoin price from CoinDesk API");
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(ApiUrl);

            if (response.TryGetProperty("bpi", out var bpi) &&
                bpi.TryGetProperty("USD", out var usd) &&
                usd.TryGetProperty("rate", out var rate))
            {
                var price = rate.GetString();
                _logger.LogDebug("Bitcoin price fetched: {Price}", price);
                return $"${price}";
            }

            _logger.LogWarning("Unexpected API response structure");
            throw new InvalidOperationException("Failed to parse Bitcoin price from API response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Bitcoin price");
            return $"Error fetching Bitcoin price: {ex.Message}";
        }
    }
}
