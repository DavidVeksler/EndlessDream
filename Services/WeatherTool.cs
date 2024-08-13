using System.Net.Http.Json;
using System.Text.Json;


public class WeatherTool : ITool
{
    private const string OpenWeatherApiKey = "7cc6fd58de105f026ceb58c4c702fa80";

    public async Task<string> ExecuteAsync(string[] parameters)
    {
        if (parameters.Length == 0) return "Error: Location parameter is missing";

        var location = parameters[0].Replace("location=", "").Trim('"');
        using var client = new HttpClient();

        try
        {
            var (lat, lon) = await GetCoordinates(client, location);
            var response = await client.GetFromJsonAsync<JsonElement>($"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&exclude=minutely,hourly,daily,alerts&units=imperial&appid={OpenWeatherApiKey}");

            var main = response.GetProperty("main");
            var weather = response.GetProperty("weather")[0];
            var wind = response.GetProperty("wind");

            var temp = main.GetProperty("temp").GetDouble();
            var feelsLike = main.GetProperty("feels_like").GetDouble();
            var humidity = main.GetProperty("humidity").GetInt32();
            var description = weather.GetProperty("description").GetString();
            var windSpeed = wind.GetProperty("speed").GetDouble();
            var cityName = response.GetProperty("name").GetString();
            var country = response.GetProperty("sys").GetProperty("country").GetString();

            return $"Current weather in {cityName}, {country}: " +
                   $"{temp:F1}°C (feels like {feelsLike:F1}°C), {description}. " +
                   $"Humidity: {humidity}%, Wind speed: {windSpeed} m/s.";
        }
        catch (Exception ex)
        {
            return $"Error fetching weather data: {ex.Message}";
        }
    }

    private async Task<(double Lat, double Lon)> GetCoordinates(HttpClient client, string location)
    {
        var response = await client.GetFromJsonAsync<JsonElement>($"http://api.openweathermap.org/geo/1.0/direct?q={location}&limit=1&appid={OpenWeatherApiKey}");
        var firstResult = response.EnumerateArray().FirstOrDefault();
        if (firstResult.ValueKind == JsonValueKind.Undefined)
            throw new Exception($"Location not found: {location}");

        return (firstResult.GetProperty("lat").GetDouble(), firstResult.GetProperty("lon").GetDouble());
    }
}