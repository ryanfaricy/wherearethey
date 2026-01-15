using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using WhereAreThey.Components;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Validators;

public class LocationReportValidator : AbstractValidator<LocationReport>
{
    public LocationReportValidator(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISettingsService settingsService,
        IStringLocalizer<App> l)
    {
        RuleFor(x => x.ReporterIdentifier)
            .NotEmpty().WithMessage(l["Identifier_Error"])
            .MinimumLength(8).WithMessage(l["Passphrase_Length_Error"]);

        RuleFor(x => x.Message)
            .Must(m => string.IsNullOrEmpty(m) || (!m.Contains("http://") && !m.Contains("https://") && !m.Contains("www.")))
            .WithMessage(l["Links_Error"]);

        RuleFor(x => x)
            .CustomAsync(async (report, context, cancellation) =>
            {
                var settings = await settingsService.GetSettingsAsync();

                // Cooldown check
                await using var dbContext = await contextFactory.CreateDbContextAsync(cancellation);
                var cutoff = DateTime.UtcNow.AddMinutes(-settings.ReportCooldownMinutes);
                
                var hasRecent = await dbContext.LocationReports
                    .AnyAsync(r => r.ReporterIdentifier == report.ReporterIdentifier && r.Timestamp >= cutoff, cancellation);

                if (hasRecent)
                {
                    context.AddFailure(nameof(report.ReporterIdentifier), string.Format(l["Cooldown_Error"], settings.ReportCooldownMinutes));
                }

                // Distance check
                if (report.ReporterLatitude.HasValue && report.ReporterLongitude.HasValue)
                {
                    var distance = GeoUtils.CalculateDistance(report.Latitude, report.Longitude,
                        report.ReporterLatitude.Value, report.ReporterLongitude.Value);

                    var maxDistanceKm = (double)settings.MaxReportDistanceMiles * 1.60934;
                    if (distance > maxDistanceKm)
                    {
                        context.AddFailure(nameof(report.Latitude), string.Format(l["Distance_Error"], settings.MaxReportDistanceMiles));
                    }
                }
                else
                {
                    context.AddFailure(nameof(report.ReporterLatitude), l["Location_Verify_Error"]);
                }
            });
    }
}
