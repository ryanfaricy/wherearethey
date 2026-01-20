using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services;
using WhereAreThey.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using FluentValidation;
using Hangfire;

namespace WhereAreThey.Tests.Services;

public class PersistenceTests
{
    private static async Task<(ApplicationDbContext, IDbContextFactory<ApplicationDbContext>)> CreateContextAndFactoryAsync()
    {
        await Task.CompletedTask;
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        return (context, factoryMock.Object);
    }

    [Fact]
    public async Task UpdateAlertAsync_ShouldPersistCreatedAtAndDeletedAt()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        var protectorMock = new Mock<IDataProtectionProvider>();
        protectorMock.Setup(p => p.CreateProtector(It.IsAny<string>())).Returns(new Mock<IDataProtector>().Object);
        
        var validatorMock = new Mock<IValidator<Alert>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Alert>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var service = new AlertService(
            factory,
            protectorMock.Object,
            new Mock<IEmailService>().Object,
            new Mock<IBackgroundJobClient>().Object,
            new Mock<IEventService>().Object,
            new Mock<IBaseUrlProvider>().Object,
            new Mock<IEmailTemplateService>().Object,
            new Mock<ILogger<AlertService>>().Object,
            validatorMock.Object
        );

        var originalCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var alert = new Alert
        {
            Latitude = 1,
            Longitude = 1,
            RadiusKm = 1,
            CreatedAt = originalCreatedAt,
            UserIdentifier = "test",
        };
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        var newCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDeletedAt = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        
        var alertToUpdate = await context.Alerts.IgnoreQueryFilters().AsNoTracking().FirstAsync(a => a.Id == alert.Id);
        alertToUpdate.CreatedAt = newCreatedAt;
        alertToUpdate.DeletedAt = newDeletedAt;

        await service.UpdateAlertAsync(alertToUpdate);

        // Assert
        var updatedAlert = await context.Alerts.IgnoreQueryFilters().AsNoTracking().FirstAsync(a => a.Id == alert.Id);
        Assert.Equal(newCreatedAt, updatedAlert.CreatedAt);
        Assert.Equal(newDeletedAt, updatedAlert.DeletedAt);
    }

    [Fact]
    public async Task UpdateReportAsync_ShouldPersistCreatedAtAndDeletedAt()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        
        var validatorMock = new Mock<IValidator<Report>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Report>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var service = new ReportService(
            factory,
            new Mock<IBackgroundJobClient>().Object,
            new Mock<ISettingsService>().Object,
            new Mock<IEventService>().Object,
            new Mock<IBaseUrlProvider>().Object,
            validatorMock.Object,
            new Mock<ILogger<ReportService>>().Object
        );

        var originalCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var report = new Report
        {
            Latitude = 1,
            Longitude = 1,
            CreatedAt = originalCreatedAt,
        };
        context.Reports.Add(report);
        await context.SaveChangesAsync();

        // Act
        var newCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDeletedAt = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        
        var reportToUpdate = await context.Reports.IgnoreQueryFilters().AsNoTracking().FirstAsync(r => r.Id == report.Id);
        reportToUpdate.CreatedAt = newCreatedAt;
        reportToUpdate.DeletedAt = newDeletedAt;

        await service.UpdateReportAsync(reportToUpdate);

        // Assert
        var updatedReport = await context.Reports.IgnoreQueryFilters().AsNoTracking().FirstAsync(r => r.Id == report.Id);
        Assert.Equal(newCreatedAt, updatedReport.CreatedAt);
        Assert.Equal(newDeletedAt, updatedReport.DeletedAt);
    }

    [Fact]
    public async Task UpdateFeedbackAsync_ShouldPersistCreatedAtAndDeletedAt()
    {
        // Arrange
        var (context, factory) = await CreateContextAndFactoryAsync();
        
        var validatorMock = new Mock<IValidator<Feedback>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        var service = new FeedbackService(
            factory,
            new Mock<IEventService>().Object,
            validatorMock.Object
        );

        var originalCreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var feedback = new Feedback
        {
            Type = "Bug",
            Message = "Test",
            CreatedAt = originalCreatedAt,
        };
        context.Feedbacks.Add(feedback);
        await context.SaveChangesAsync();

        // Act
        var newCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDeletedAt = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        
        var feedbackToUpdate = await context.Feedbacks.IgnoreQueryFilters().AsNoTracking().FirstAsync(f => f.Id == feedback.Id);
        feedbackToUpdate.CreatedAt = newCreatedAt;
        feedbackToUpdate.DeletedAt = newDeletedAt;

        await service.UpdateFeedbackAsync(feedbackToUpdate);

        // Assert
        var updatedFeedback = await context.Feedbacks.IgnoreQueryFilters().AsNoTracking().FirstAsync(f => f.Id == feedback.Id);
        Assert.Equal(newCreatedAt, updatedFeedback.CreatedAt);
        Assert.Equal(newDeletedAt, updatedFeedback.DeletedAt);
    }
}
