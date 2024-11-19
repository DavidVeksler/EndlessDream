using System.Net.Http.Json;
using System.Text.Json;

public class BitcoinPriceTool : ITool
{
    public async Task<string> ExecuteAsync(string[] parameters)
    {
        using var client = new HttpClient();
        var response = await client.GetFromJsonAsync<JsonElement>("https://api.coindesk.com/v1/bpi/currentprice.json");
        var price = response.GetProperty("bpi").GetProperty("USD").GetProperty("rate").GetString();
        return $"${price}";
    }
}