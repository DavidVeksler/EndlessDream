namespace EndlessDreamBlazorWeb.Data
{
    /// <summary>
    /// Centralized application configuration service.
    /// Replaces Settings.cs and ConfigSettings for unified access to all configuration.
    /// </summary>
    public class AppConfiguration
    {
        private readonly IConfiguration _configuration;

        public AppConfiguration(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // API Keys
        public string OpenAIApiKey => _configuration["ApiKeys:OpenAI"] ?? string.Empty;
        public string CoinGeckoApiKey => _configuration["ApiKeys:CoinGecko"] ?? string.Empty;
        public string InfuraApiKey => _configuration["ApiKeys:Infura"] ?? string.Empty;
        public string EtherscanApiKey => _configuration["ApiKeys:Etherscan"] ?? string.Empty;
        public string OpenWeatherMapApiKey => _configuration["ApiKeys:OpenWeatherMap"] ?? string.Empty;

        // External Service Endpoints
        public string LlmLocalEndpoint => _configuration["ExternalServices:LlmEndpoints:Local"] ?? string.Empty;
        public string LlmRemoteEndpoint => _configuration["ExternalServices:LlmEndpoints:Remote"] ?? string.Empty;
        public string ImageGeneratorBaseUrl => _configuration["ExternalServices:ImageGenerator:BaseUrl"] ?? string.Empty;
        public string TerminalBaseUrl => _configuration["ExternalServices:Terminal:BaseUrl"] ?? string.Empty;
        public string CoinGeckoBaseUrl => _configuration["ExternalServices:CoinGecko:BaseUrl"] ?? string.Empty;

        // Service Defaults
        public int CoinGeckoCacheDurationSeconds =>
            int.TryParse(_configuration["ServiceDefaults:CoinGeckoCacheDurationSeconds"], out var duration)
                ? duration
                : 300;

        public float DefaultTemperature =>
            float.TryParse(_configuration["ServiceDefaults:DefaultTemperature"], out var temp)
                ? temp
                : 0.7f;

        public int DefaultMaxTokens =>
            int.TryParse(_configuration["ServiceDefaults:DefaultMaxTokens"], out var tokens)
                ? tokens
                : 99999;

        /// <summary>
        /// Validates that all required API keys are configured.
        /// </summary>
        public bool ValidateConfiguration(out List<string> missingKeys)
        {
            missingKeys = new List<string>();

            var apiKeys = new Dictionary<string, string>
            {
                ["OpenAI"] = OpenAIApiKey,
                ["CoinGecko"] = CoinGeckoApiKey,
                ["OpenWeatherMap"] = OpenWeatherMapApiKey
            };

            foreach (var key in apiKeys)
            {
                if (string.IsNullOrWhiteSpace(key.Value))
                {
                    missingKeys.Add(key.Key);
                }
            }

            return missingKeys.Count == 0;
        }
    }
}
