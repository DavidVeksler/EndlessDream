using System.Net.Http.Json;
using System.Text.Json;
using EndlessDreamBlazorWeb.Data;

/// <summary>
/// Tool for fetching weather information from OpenWeatherMap API.
/// Provides real-time weather conditions for specified locations.
/// </summary>
public class WeatherTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;
    private readonly ILogger<WeatherTool> _logger;

    public WeatherTool(HttpClient httpClient, AppConfiguration config, ILogger<WeatherTool> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes weather lookup for a specified location.
    /// </summary>
    /// <param name="parameters">Parameters array where first element is location name</param>
    /// <remarks>
    /// BREAKING CHANGE (v2.0): Temperature units changed from Fahrenheit (°F) to Celsius (°C).
    /// If your code expects Fahrenheit values, conversion will be needed: °F = (°C * 9/5) + 32
    /// </remarks>
    public async Task<string> ExecuteAsync(string[] parameters)
    {
        if (parameters.Length == 0)
        {
            _logger.LogWarning("Weather tool called without location parameter");
            return "Error: Location parameter is missing";
        }

        var location = parameters[0].Replace("location=", "").Trim('"');
        var apiKey = _config.OpenWeatherMapApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("OpenWeatherMap API key not configured");
            return "Error: Weather API not configured";
        }

        try
        {
            _logger.LogDebug("Fetching weather for location: {Location}", location);
            var (lat, lon) = await GetCoordinatesAsync(location, apiKey);
            var weatherData = await FetchWeatherAsync(lat, lon, apiKey);
            return FormatWeatherResponse(weatherData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather for location: {Location}", location);
            return $"Error fetching weather data: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets geographic coordinates for a location using OpenWeatherMap Geocoding API.
    /// </summary>
    private async Task<(double Lat, double Lon)> GetCoordinatesAsync(string location, string apiKey)
    {
        var url = $"https://api.openweathermap.org/geo/1.0/direct?q={Uri.EscapeDataString(location)}&limit=1&appid={apiKey}";
        var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);

        var firstResult = response.EnumerateArray().FirstOrDefault();
        if (firstResult.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Location not found: {location}");
        }

        var lat = firstResult.GetProperty("lat").GetDouble();
        var lon = firstResult.GetProperty("lon").GetDouble();
        return (lat, lon);
    }

    /// <summary>
    /// Fetches current weather data for the specified coordinates.
    /// </summary>
    private async Task<JsonElement> FetchWeatherAsync(double lat, double lon, string apiKey)
    {
        var url = $"https://api.openweathermap.org/data/2.5/weather?" +
                  $"lat={lat}&lon={lon}&exclude=minutely,hourly,daily,alerts&units=metric&appid={apiKey}";
        var response = await _httpClient.GetFromJsonAsync<JsonElement>(url);
        if (response.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException("Failed to fetch weather data");
        }
        return response;
    }

    /// <summary>
    /// Formats the JSON weather response into a human-readable string.
    /// </summary>
    private static string FormatWeatherResponse(JsonElement response)
    {
        var main = response.GetProperty("main");
        var weather = response.GetProperty("weather")[0];
        var wind = response.GetProperty("wind");

        var temp = main.GetProperty("temp").GetDouble();
        var feelsLike = main.GetProperty("feels_like").GetDouble();
        var humidity = main.GetProperty("humidity").GetInt32();
        var description = weather.GetProperty("description").GetString() ?? "Unknown";
        var windSpeed = wind.GetProperty("speed").GetDouble();
        var cityName = response.GetProperty("name").GetString() ?? "Unknown";
        var country = response.GetProperty("sys").GetProperty("country").GetString() ?? "";

        return $"Current weather in {cityName}, {country}: " +
               $"{temp:F1}°C (feels like {feelsLike:F1}°C), {description}. " +
               $"Humidity: {humidity}%, Wind speed: {windSpeed:F1} m/s.";
    }
}
