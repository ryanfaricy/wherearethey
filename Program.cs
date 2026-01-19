using System.Globalization;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Fido2NetLib;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Radzen;
using Serilog;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Helpers;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

// Add services to the container.
var razorComponentsBuilder = builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        // Increase timeouts for SignalR to be more resilient on mobile networks
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    })
    .AddCircuitOptions(options =>
    {
        // Increase the allowed disconnect window for better mobile experience
        // This allows the server to keep the circuit alive longer while the app is in background
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
    });

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
        ["image/svg+xml"]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Add HttpClient for proxy and other services
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Add Email services
builder.Services.AddOptions<AppOptions>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SquareOptions>()
    .Bind(builder.Configuration.GetSection("Square"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection("Email"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<MicrosoftGraphEmailService>();
builder.Services.AddHttpClient<IGeocodingService, GeocodingService>(client => {
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddTransient<SmtpEmailService>();
builder.Services.AddScoped<IEmailService>(sp => 
    new FallbackEmailService([
        sp.GetRequiredService<MicrosoftGraphEmailService>(),
        sp.GetRequiredService<SmtpEmailService>(),
    ], sp.GetRequiredService<ILogger<FallbackEmailService>>()));

// Add DbContextFactory with PostgreSQL
var connectionString = ConfigurationHelper.GetConnectionString(builder.Configuration) 
                      ?? "Host=localhost;Database=wherearethey;Username=postgres;Password=postgres";

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableDetailedErrors();
});

// Add Hangfire
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(connectionString);
        });
});
builder.Services.AddHangfireServer();

// Add application services
builder.Services.AddValidatorsFromAssemblyContaining<WhereAreThey.Program>();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddScoped<IReportProcessingService, ReportProcessingService>();
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddSingleton<UserConnectionService>();
builder.Services.AddScoped<CircuitHandler, UserConnectionCircuitHandler>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IDonationService, DonationService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAppThemeService, AppThemeService>();
builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IClientStorageService, ClientStorageService>();
builder.Services.AddScoped<IMapStateService, MapStateService>();
builder.Services.AddScoped<IMapInteractionService, MapInteractionService>();
builder.Services.AddScoped<IClientLocationService, ClientLocationService>();
builder.Services.AddScoped<IHapticFeedbackService, HapticFeedbackService>();
builder.Services.AddScoped<IMapNavigationManager, MapNavigationManager>();
builder.Services.AddScoped<IAdminPasskeyService, AdminPasskeyService>();
builder.Services.AddScoped<IBaseUrlProvider, BaseUrlProvider>();
builder.Services.AddScoped<IFido2>(sp =>
{
    var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
    var uri = new Uri(appOptions.BaseUrl);
    return new Fido2(new Fido2Configuration
    {
        ServerDomain = builder.Configuration["Fido2:ServerDomain"] ?? uri.Host,
        ServerName = "WhereAreThey Admin",
        Origins = new HashSet<string> { appOptions.BaseUrl.TrimEnd('/') },
    });
});
builder.Services.AddScoped<UserTimeZoneService>();
builder.Services.AddHostedService<DatabaseCleanupService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

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
                QueueLimit = 0,
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
if (builder.Configuration["SkipMigrations"] != "true")
{
    using var scope = app.Services.CreateScope();
    try
    {
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WhereAreThey.Program>>();
        logger.LogCritical(ex, "Failed to apply database migrations on startup.");
        throw;
    }
}

// Configure the HTTP request pipeline.
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
};
forwardedOptions.KnownProxies.Clear();
forwardedOptions.KnownIPNetworks.Clear();
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
app.UseSerilogRequestLogging();
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

app.MapHealthChecks("/health");
app.MapControllers();

var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter(appOptions.AdminPassword)],
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api/map/proxy", async (string? reportId, IReportService reportService, ISettingsService settingsService, IOptions<AppOptions> applicationOptions, IHttpClientFactory httpClientFactory, IBaseUrlProvider baseUrlProvider) => 
{
    if (string.IsNullOrEmpty(reportId) || !Guid.TryParse(reportId, out var rGuid))
    {
        return Results.BadRequest();
    }

    var result = await reportService.GetReportByExternalIdAsync(rGuid);
    if (result.IsFailure)
    {
        return Results.NotFound();
    }

    var report = result.Value!;
    var httpClient = httpClientFactory.CreateClient();
    var settings = await settingsService.GetSettingsAsync();
    if (string.IsNullOrEmpty(settings.MapboxToken))
    {
        return Results.NotFound();
    }

    var latStr = report.Latitude.ToString(CultureInfo.InvariantCulture);
    var lngStr = report.Longitude.ToString(CultureInfo.InvariantCulture);
    var mapboxUrl = $"https://api.mapbox.com/styles/v1/mapbox/streets-v11/static/pin-s-l+f44336({lngStr},{latStr})/{lngStr},{latStr},14,0/450x300?access_token={settings.MapboxToken}";

    var request = new HttpRequestMessage(HttpMethod.Get, mapboxUrl);
    
    // Use the BaseUrl as Referer as requested
    var referer = baseUrlProvider.GetBaseUrl();
    request.Headers.Referrer = new Uri(referer);

    var response = await httpClient.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        return Results.StatusCode((int)response.StatusCode);
    }

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
}
catch (Exception ex) when (ex is not HostAbortedException and not OperationCanceledException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

namespace WhereAreThey
{
    public class Program;
}
