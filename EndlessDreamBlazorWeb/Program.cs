using EndlessDreamBlazorWeb.Data;
using EndlessDreamBlazorWeb.Services;
using Microsoft.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register centralized configuration
builder.Services.AddSingleton<AppConfiguration>();

// Add caching
builder.Services.AddMemoryCache();

// Register HttpClient factory with default configuration
builder.Services.AddHttpClient<CoinGeckoPriceService>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.coingecko.com/");
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
    });

builder.Services.AddHttpClient<LlmService>();
builder.Services.AddHttpClient<TerminalService>();
builder.Services.AddHttpClient<ImageGenerator>();

// Register tools with HttpClientFactory
builder.Services.AddHttpClient<WeatherTool>();
builder.Services.AddHttpClient<BitcoinPriceTool>();
builder.Services.AddHttpClient<WebScrapingTool>();

// Register conversation and generation services
builder.Services.AddScoped<OpenAIGPTConversationService>();

// Register legacy services
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<ToolManager>();

// Add logging
builder.Services.AddLogging();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //  _ = app.UseHsts();
}

WebHost.CreateDefaultBuilder(args).UseSetting(WebHostDefaults.DetailedErrorsKey, "true");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();