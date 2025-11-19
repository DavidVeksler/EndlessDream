using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using EndlessDreamBlazorWeb.Data;

namespace EndlessDreamBlazorWeb.Services;

/// <summary>
/// Service for fetching cryptocurrency pricing data from CoinGecko API.
/// Implements caching and proper async/await patterns for performance.
/// </summary>
public class CoinGeckoPriceService
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoPriceService> _logger;
    private readonly AppConfiguration _config;
    private readonly TimeSpan _cacheDuration;

    public CoinGeckoPriceService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<CoinGeckoPriceService> logger,
        AppConfiguration config)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        _cacheDuration = TimeSpan.FromSeconds(_config.CoinGeckoCacheDurationSeconds);
    }

    /// <summary>
    /// Gets list of all supported fiat currencies.
    /// </summary>
    public async Task<string[]> GetSupportedVsCurrenciesAsync()
    {
        return await FetchAsync<string[]>("/api/v3/simple/supported_vs_currencies");
    }

    /// <summary>
    /// Gets cryptocurrency market information with caching.
    /// </summary>
    public async Task<CoinInfo[]> GetCurrencyInfoAsync(string vsCurrency, string ids)
    {
        var cacheKey = ComputeCacheKey(vsCurrency, ids);

        if (_cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse))
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return JsonConvert.DeserializeObject<CoinInfo[]>(cachedResponse!.Content) ?? Array.Empty<CoinInfo>();
        }

        var requestUri = $"/api/v3/coins/markets?vs_currency={vsCurrency.ToLower()}&ids={ids}";
        if (!string.IsNullOrEmpty(_config.CoinGeckoApiKey))
        {
            requestUri += $"&x_cg_demo_api_key={_config.CoinGeckoApiKey}";
        }

        try
        {
            _logger.LogDebug("Fetching cryptocurrency info: {RequestUri}", requestUri);
            var result = await FetchAsync<CoinInfo[]>(requestUri);
            var content = JsonConvert.SerializeObject(result);
            var etag = ComputeETag(content);
            _cache.Set(cacheKey, new CachedResponse { Content = content, ETag = etag }, _cacheDuration);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "CoinGecko request failed for {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// Converts amount from crypto asset to USD asynchronously.
    /// </summary>
    public async Task<decimal> ConvertToUsdAsync(decimal amount, string asset = "bitcoin")
    {
        var prices = await GetCurrencyInfoAsync("usd", asset);
        if (prices.Length == 0)
        {
            _logger.LogWarning("No price data available for {Asset}", asset);
            return 0;
        }
        return amount * prices[0].CurrentPrice;
    }

    /// <summary>
    /// Converts amount from fiat currency to BTC asynchronously.
    /// </summary>
    public async Task<decimal> ConvertToBtcAsync(decimal amount, string asset = "bitcoin")
    {
        try
        {
            var prices = await GetCurrencyInfoAsync("usd", asset);
            if (prices.Length == 0)
            {
                _logger.LogWarning("No price data available for {Asset}", asset);
                return 0;
            }
            return amount / prices[0].CurrentPrice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to BTC for {Asset}", asset);
            return amount / 100000; // Fallback: satoshi price approximation
        }
    }

    private async Task<T> FetchAsync<T>(string requestUri)
    {
        _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(
            new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

        var response = await _httpClient.GetAsync(requestUri);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CoinGecko API error - Status: {StatusCode}, Response: {Content}",
                response.StatusCode, content);
            throw new HttpRequestException(
                $"Request failed with status code: {response.StatusCode}");
        }

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        return JsonConvert.DeserializeObject<T>(content, settings)
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    private static string ComputeCacheKey(string vsCurrency, string ids) =>
        $"coingecko_{vsCurrency.ToLower()}_{ids}";

    private static string ComputeETag(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Cached API response wrapper.
    /// </summary>
    public class CachedResponse
    {
        public required string Content { get; set; }
        public required string ETag { get; set; }
    }
}

/// <summary>
/// Cryptocurrency market information from CoinGecko API.
/// </summary>
public class CoinInfo
{
    [JsonProperty("id")] public required string Id { get; set; }
    [JsonProperty("symbol")] public required string Symbol { get; set; }
    [JsonProperty("name")] public required string Name { get; set; }
    [JsonProperty("image")] public required string Image { get; set; }
    [JsonProperty("current_price")] public decimal CurrentPrice { get; set; }
    [JsonProperty("market_cap")] public long MarketCap { get; set; }
    [JsonProperty("market_cap_rank")] public int MarketCapRank { get; set; }
    [JsonProperty("fully_diluted_valuation")] public long? FullyDilutedValuation { get; set; }
    [JsonProperty("total_volume")] public long TotalVolume { get; set; }
    [JsonProperty("high_24h")] public decimal High24h { get; set; }
    [JsonProperty("low_24h")] public decimal Low24h { get; set; }
    [JsonProperty("price_change_24h")] public decimal PriceChange24h { get; set; }
    [JsonProperty("price_change_percentage_24h")] public decimal PriceChangePercentage24h { get; set; }
    [JsonProperty("market_cap_change_24h")] public long MarketCapChange24h { get; set; }
    [JsonProperty("market_cap_change_percentage_24h")] public decimal MarketCapChangePercentage24h { get; set; }
    [JsonProperty("circulating_supply")] public double CirculatingSupply { get; set; }
    [JsonProperty("total_supply")] public double? TotalSupply { get; set; }
    [JsonProperty("max_supply")] public double? MaxSupply { get; set; }
    [JsonProperty("ath")] public decimal Ath { get; set; }
    [JsonProperty("ath_change_percentage")] public decimal AthChangePercentage { get; set; }
    [JsonProperty("ath_date")] public DateTime AthDate { get; set; }
    [JsonProperty("atl")] public decimal Atl { get; set; }
    [JsonProperty("atl_change_percentage")] public decimal AtlChangePercentage { get; set; }
    [JsonProperty("atl_date")] public DateTime AtlDate { get; set; }
    [JsonProperty("roi")] public object? Roi { get; set; }
    [JsonProperty("last_updated")] public DateTime LastUpdated { get; set; }
}
