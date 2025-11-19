using HtmlAgilityPack;

/// <summary>
/// Tool for scraping web page content and extracting metadata.
/// Retrieves page title, description, and body preview from any URL.
/// </summary>
public class WebScrapingTool : ITool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebScrapingTool> _logger;
    private const int BodyPreviewLength = 500;

    public WebScrapingTool(HttpClient httpClient, ILogger<WebScrapingTool> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes web scraping for a given URL.
    /// </summary>
    /// <param name="parameters">Parameters array where first element is the URL to scrape</param>
    public async Task<string> ExecuteAsync(string[] parameters)
    {
        if (parameters.Length == 0 || string.IsNullOrWhiteSpace(parameters[0]))
        {
            _logger.LogWarning("WebScrapingTool called without URL parameter");
            return "Error: URL parameter is missing";
        }

        var url = parameters[0];
        if (!IsValidUrl(url))
        {
            _logger.LogWarning("Invalid URL provided: {Url}", url);
            return $"Error: Invalid URL format: {url}";
        }

        try
        {
            _logger.LogDebug("Scraping web page: {Url}", url);
            var html = await _httpClient.GetStringAsync(url);
            return ExtractPageContent(html);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error scraping web page: {Url}", url);
            return $"Error fetching web page: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping web page: {Url}", url);
            return $"Error scraping web page: {ex.Message}";
        }
    }

    /// <summary>
    /// Extracts title, description, and body preview from HTML.
    /// </summary>
    private static string ExtractPageContent(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = ExtractNodeText(doc, "//title");
        var metaDescription = ExtractMetaDescription(doc);
        var bodyText = ExtractNodeText(doc, "//body");

        if (bodyText?.Length > BodyPreviewLength)
        {
            bodyText = bodyText[..BodyPreviewLength] + "...";
        }

        return $"Title: {title ?? "N/A"}\n" +
               $"Description: {metaDescription ?? "N/A"}\n" +
               $"Body preview: {bodyText ?? "N/A"}";
    }

    /// <summary>
    /// Extracts text content from an HTML node.
    /// </summary>
    private static string? ExtractNodeText(HtmlDocument doc, string xPath)
    {
        var node = doc.DocumentNode.SelectSingleNode(xPath);
        return node?.InnerText?.Trim();
    }

    /// <summary>
    /// Extracts meta description from HTML document.
    /// </summary>
    private static string? ExtractMetaDescription(HtmlDocument doc)
    {
        var metaNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");
        return metaNode?.GetAttributeValue("content", null);
    }

    /// <summary>
    /// Validates that a string is a properly formed URL.
    /// </summary>
    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
