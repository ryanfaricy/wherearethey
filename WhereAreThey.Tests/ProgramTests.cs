using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WhereAreThey.Services;
using Xunit;

namespace WhereAreThey.Tests;

public class ProgramTests
{
    [Fact]
    public void ServiceProvider_ShouldBuildWithoutCircularDependency()
    {
        // This test simulates the service registration part of Program.cs
        // to ensure that IEmailService (FallbackEmailService) can be resolved
        var builder = WebApplication.CreateBuilder();

        // Register same services as in Program.cs
        builder.Services.AddLogging();
        builder.Services.Configure<EmailOptions>(options => { });
        
        builder.Services.AddHttpClient<MicrosoftGraphEmailService>();
        builder.Services.AddHttpClient<BrevoHttpEmailService>();
        builder.Services.AddHttpClient<MailjetHttpEmailService>();
        builder.Services.AddHttpClient<SendGridHttpEmailService>();
        builder.Services.AddTransient<SmtpEmailService>();
        builder.Services.AddScoped<IEmailService>(sp => 
            new FallbackEmailService(new IEmailService[] 
            {
                sp.GetRequiredService<MicrosoftGraphEmailService>(),
                sp.GetRequiredService<BrevoHttpEmailService>(),
                sp.GetRequiredService<MailjetHttpEmailService>(),
                sp.GetRequiredService<SendGridHttpEmailService>(),
                sp.GetRequiredService<SmtpEmailService>()
            }, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FallbackEmailService>>()));

        // Act
        var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        // Assert
        Assert.IsType<FallbackEmailService>(emailService);
    }
}
