using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Radzen;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Data Protection for encrypted emails at rest - persist keys to DB for Railway
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>();

// Add Radzen services
builder.Services.AddRadzenComponents();

// Add Email services
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

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
            Console.WriteLine($"Error parsing DATABASE_URL URI: {ex.Message}");
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
    options.UseNpgsql(connectionString ?? "Host=localhost;Database=wherearethey;Username=postgres;Password=postgres"));

// Add application services
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<DonationService>();
builder.Services.AddScoped<AppThemeService>();

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
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
