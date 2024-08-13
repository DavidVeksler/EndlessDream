using System.Net.Http;
using HtmlAgilityPack;

public class WebScrapingTool : ITool
{
    public async Task<string> ExecuteAsync(string[] parameters)
    {
        if (parameters.Length == 0 || string.IsNullOrWhiteSpace(parameters[0]))
            return "Error: URL parameter is missing";

        var url = parameters[0];
        using var client = new HttpClient();

        try
        {
            var html = await client.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();
            var metaDescription = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", "");
            var bodyText = doc.DocumentNode.SelectSingleNode("//body")?.InnerText.Trim();

            if (bodyText?.Length > 500)
                bodyText = bodyText[..500] + "...";

            return $"Title: {title}\nDescription: {metaDescription}\nBody preview: {bodyText}";
        }
        catch (Exception ex)
        {
            return $"Error scraping web page: {ex.Message}";
        }
    }
}