using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace EndlessDreamBlazorWeb.Services;

public static class ConfigSettings
{
    private static IConfigurationRoot? _configuration;

    static ConfigSettings()
    {
        InitializeConfiguration();
    }


    public static string CoinGeckoKey => GetConfigValue("CoinGecko:ApiKey");
    public static string PricesToCheck => GetConfigValue("PricesToCheck");
    public static string InfuraKey => GetConfigValue("Infura:ApiKey");
    public static string EtherscanKey => GetConfigValue("Etherscan:ApiKey");

    private static void InitializeConfiguration()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var appSettingsPath = FindAppSettingsPath(basePath);

        _configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(appSettingsPath))
            .AddJsonFile("appsettings.json", true, true)
            .Build();
    }

    private static string FindAppSettingsPath(string basePath)
    {
        string[] potentialPaths = { basePath, Directory.GetParent(basePath)?.Parent?.Parent?.Parent?.FullName };

        foreach (var path in potentialPaths)
        {
            var appSettingsFilePath = Path.Combine(path, "appsettings.json");
            if (File.Exists(appSettingsFilePath)) return appSettingsFilePath;
        }

        throw new FileNotFoundException("appsettings.json is required");
    }

    private static string GetConfigValue(string key)
    {
        return _configuration[key];
    }
}

public class CoinInfo
{
    [JsonProperty("id")] public required string Id { get; set; }

    [JsonProperty("symbol")] public required string Symbol { get; set; }

    [JsonProperty("name")] public required string Name { get; set; }

    [JsonProperty("image")] public required string Image { get; set; }

    [JsonProperty("current_price")] public decimal CurrentPrice { get; set; }

    [JsonProperty("market_cap")] public long MarketCap { get; set; }

    [JsonProperty("market_cap_rank")] public int MarketCapRank { get; set; }

    [JsonProperty("fully_diluted_valuation")]
    public long? FullyDilutedValuation { get; set; }

    [JsonProperty("total_volume")] public long TotalVolume { get; set; }

    [JsonProperty("high_24h")] public decimal High24h { get; set; }

    [JsonProperty("low_24h")] public decimal Low24h { get; set; }

    [JsonProperty("price_change_24h")] public decimal PriceChange24h { get; set; }

    [JsonProperty("price_change_percentage_24h")]
    public decimal PriceChangePercentage24h { get; set; }

    [JsonProperty("market_cap_change_24h")]
    public long MarketCapChange24h { get; set; }

    [JsonProperty("market_cap_change_percentage_24h")]
    public decimal MarketCapChangePercentage24h { get; set; }

    [JsonProperty("circulating_supply")] public double CirculatingSupply { get; set; }

    [JsonProperty("total_supply")] public double? TotalSupply { get; set; }

    [JsonProperty("max_supply")] public double? MaxSupply { get; set; }

    [JsonProperty("ath")] public decimal Ath { get; set; }

    [JsonProperty("ath_change_percentage")]
    public decimal AthChangePercentage { get; set; }

    [JsonProperty("ath_date")] public DateTime AthDate { get; set; }

    [JsonProperty("atl")] public decimal Atl { get; set; }

    [JsonProperty("atl_change_percentage")]
    public decimal AtlChangePercentage { get; set; }

    [JsonProperty("atl_date")] public DateTime AtlDate { get; set; }

    [JsonProperty("roi")] public required object Roi { get; set; } // Adjust as needed.

    [JsonProperty("last_updated")] public DateTime LastUpdated { get; set; }
}

public class CoinGeckoPriceService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(9);
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoPriceService> _logger;

    public CoinGeckoPriceService(
        IMemoryCache cache,
        ILogger<CoinGeckoPriceService> logger)
    {
        _cache = cache;
        _logger = logger;

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.coingecko.com/")
        };
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
    }


    public async Task<string[]> GetSupportedVsCurrenciesAsync()
    {
        return await FetchAsync<string[]>("/api/v3/simple/supported_vs_currencies");
    }

    public async Task<CoinInfo[]> GetCurrencyInfoAsync(string vsCurrency, string ids)
    {
        var cacheKey = ComputeCacheKey(vsCurrency, ids);

        if (_cache.TryGetValue(cacheKey, out CachedResponse cachedResponse))
            return JsonConvert.DeserializeObject<CoinInfo[]>(cachedResponse.Content);

        var requestUri = $"/api/v3/coins/markets?vs_currency={vsCurrency.ToLower()}&ids={ids}";
        if (!string.IsNullOrEmpty(ConfigSettings.CoinGeckoKey))
            requestUri += $"&x_cg_demo_api_key={ConfigSettings.CoinGeckoKey}";

        try
        {
            var result = await FetchAsync<CoinInfo[]>(requestUri);
            var content = JsonConvert.SerializeObject(result);
            var etag = ComputeETag(content);
            _cache.Set(cacheKey, new CachedResponse { Content = content, ETag = etag }, CacheDuration);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "CoinGecko request failed");
            throw;
        }
    }

    private async Task<T> FetchAsync<T>(string requestUri)
    {
        _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
        var response = await _httpClient.GetAsync(requestUri);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("CoinGecko request failed with status code: {StatusCode}", response.StatusCode);
            throw new HttpRequestException(
                $"Request failed with status code: {response.StatusCode} and response: {content}");
        }

        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        return JsonConvert.DeserializeObject<T>(content, settings);;
    }

    public decimal ConvertToUSD(decimal amount, string asset = "bitcoin")
    {
        var price = GetCurrencyInfoAsync("usd", asset).Result[0].CurrentPrice;
        return amount * price;
    }

    public decimal ConvertToBTC(decimal amount, string asset = "bitcoin")
    {
        try
        {
            var price = GetCurrencyInfoAsync("usd", asset).Result[0].CurrentPrice;
            return amount / price;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error converting to BTC");
            return amount / 100000; // Temporary fix for the price of satoshis
        }
    }

    private string ComputeCacheKey(string vsCurrency, string ids)
    {
        return $"coingecko_{vsCurrency.ToLower()}_{ids}";
    }

    private string ComputeETag(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    public class CachedResponse
    {
        public string Content { get; set; }
        public string ETag { get; set; }
    }
}