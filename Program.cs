using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Globalization;
using System.Threading.RateLimiting;
using FluentValidation;
using Radzen;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Services;
using WhereAreThey.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = false;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Add services to the container.
var razorComponentsBuilder = builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

if (!builder.Environment.IsProduction())
{
    razorComponentsBuilder.AddCircuitOptions(options => { options.DetailedErrors = true; });
}

// Add Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllers(); // Needed for culture switching via cookie

// Add Data Protection for encrypted emails at rest - persist keys to DB for Railway
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// Add Radzen services
builder.Services.AddRadzenComponents();

// Add Response Compression for performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "image/svg+xml" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// Add HttpClient for proxy and other services
builder.Services.AddHttpClient();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add Email services
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddHttpClient<MicrosoftGraphEmailService>();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client => {
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddTransient<SmtpEmailService>();
builder.Services.AddScoped<IEmailService>(sp => 
    new FallbackEmailService(new IEmailService[] 
    {
        sp.GetRequiredService<MicrosoftGraphEmailService>(),
        sp.GetRequiredService<SmtpEmailService>()
    }, sp.GetRequiredService<ILogger<FallbackEmailService>>()));

// Add DbContextFactory with PostgreSQL
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string? connectionString = null;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Railway/Heroku often provide DATABASE_URL as a postgres:// URI
    if (databaseUrl.StartsWith("postgres://") || databaseUrl.StartsWith("postgresql://"))
    {
        try
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing DATABASE_URL URI: {ex}");
        }
    }
    else
    {
        // If it's not a URI, assume it's a direct connection string
        connectionString = databaseUrl;
    }
}

// If DATABASE_URL was not present or failed to parse, fallback to appsettings
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString ?? "Host=localhost;Database=wherearethey;Username=postgres;Password=postgres");
    options.EnableDetailedErrors();
});

// Add application services
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IReportProcessingService, ReportProcessingService>();
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAppThemeService, AppThemeService>();
builder.Services.AddScoped<UserTimeZoneService>();
builder.Services.AddHostedService<DatabaseCleanupService>();
builder.Services.AddHttpContextAccessor();

// Add Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("MapProxyPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Configure for high concurrency
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNumber))
    {
        serverOptions.ListenAnyIP(portNumber);
    }
    
    serverOptions.Limits.MaxConcurrentConnections = 10000;
    serverOptions.Limits.MaxConcurrentUpgradedConnections = 10000;
});

var app = builder.Build();

// Apply any pending migrations
using (var scope = app.Services.CreateScope())
{
    try
    {
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Failed to apply database migrations on startup.");
        throw;
    }
}

// Configure the HTTP request pipeline.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
};
forwardedOptions.KnownProxies.Clear();
forwardedOptions.KnownNetworks.Clear();
app.UseForwardedHeaders(forwardedOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseResponseCompression();

// Apex to WWW redirection - must be before HttpsRedirection for efficiency
app.Use(async (ctx, next) =>
{
    if (string.Equals(ctx.Request.Host.Host, "aretheyhere.com", StringComparison.OrdinalIgnoreCase))
    {
        var newUrl = "https://www.aretheyhere.com" + ctx.Request.Path + ctx.Request.QueryString;
        ctx.Response.Redirect(newUrl, permanent: true);
        return;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseAntiforgery();

// Configure Localization Middleware
var supportedCultures = new[] { "en", "es", "fr", "de", "it", "pt", "ru", "zh", "ja" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/map/proxy", async (string? reportId, ILocationService locationService, ISettingsService settingsService, IConfiguration configuration, IHttpClientFactory httpClientFactory) => 
{
    if (string.IsNullOrEmpty(reportId) || !Guid.TryParse(reportId, out var rGuid)) return Results.BadRequest();

    var report = await locationService.GetReportByExternalIdAsync(rGuid);
    if (report == null) return Results.NotFound();

    var httpClient = httpClientFactory.CreateClient();
    var settings = await settingsService.GetSettingsAsync();
    if (string.IsNullOrEmpty(settings.MapboxToken)) return Results.NotFound();

    var latStr = report.Latitude.ToString(CultureInfo.InvariantCulture);
    var lngStr = report.Longitude.ToString(CultureInfo.InvariantCulture);
    var mapboxUrl = $"https://api.mapbox.com/styles/v1/mapbox/streets-v11/static/pin-s-l+f44336({lngStr},{latStr})/{lngStr},{latStr},14,0/450x300?access_token={settings.MapboxToken}";

    var request = new HttpRequestMessage(HttpMethod.Get, mapboxUrl);
    
    // Use the BaseUrl as Referer as requested
    var referer = configuration["BaseUrl"] ?? "https://www.aretheyhere.com";
    request.Headers.Referrer = new Uri(referer);

    var response = await httpClient.SendAsync(request);
    if (!response.IsSuccessStatusCode) return Results.StatusCode((int)response.StatusCode);

    var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/png";
    var stream = await response.Content.ReadAsStreamAsync();
    return Results.Stream(stream, contentType);
}).RequireRateLimiting("MapProxyPolicy");

// App Association Files for Universal Links / App Links
app.MapGet("/.well-known/apple-app-site-association", (IWebHostEnvironment env) => 
    Results.File(Path.Combine(env.WebRootPath, ".well-known", "apple-app-site-association"), "application/json"));

app.MapGet("/.well-known/assetlinks.json", (IWebHostEnvironment env) => 
    Results.File(Path.Combine(env.WebRootPath, ".well-known", "assetlinks.json"), "application/json"));

app.MapGet("/.well-known/web-app-origin-association", (IWebHostEnvironment env) => 
    Results.File(Path.Combine(env.WebRootPath, ".well-known", "web-app-origin-association"), "application/json"));

app.Run();

public partial class Program { }
