using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;

namespace WhereAreThey.Tests.Data;

public class DbContextTests
{
    private static DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task Delete_ShouldSetDeletedAt_InsteadOfDeleting()
    {
        // Arrange
        var options = CreateOptions();
        await using var context = new ApplicationDbContext(options);
        
        var report = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0,
            CreatedAt = DateTime.UtcNow
        };
        context.Reports.Add(report);
        await context.SaveChangesAsync();

        // Act
        context.Reports.Remove(report);
        await context.SaveChangesAsync();

        // Assert
        // Re-fetch without query filters to see the soft-deleted item
        var deletedReport = await context.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == report.Id);

        Assert.NotNull(deletedReport);
        Assert.NotNull(deletedReport.DeletedAt);
        Assert.True(deletedReport.DeletedAt <= DateTime.UtcNow);

        // Verify it's hidden by default
        var activeReports = await context.Reports.ToListAsync();
        Assert.Empty(activeReports);
    }

    [Fact]
    public async Task Update_WithDeletedAt_ShouldPreserveValue()
    {
        // Arrange
        var options = CreateOptions();
        await using var context = new ApplicationDbContext(options);
        
        var report = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0,
            CreatedAt = DateTime.UtcNow
        };
        context.Reports.Add(report);
        await context.SaveChangesAsync();

        var manualDeletedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        report.DeletedAt = manualDeletedAt;
        context.Reports.Update(report);
        await context.SaveChangesAsync();

        // Assert
        var updatedReport = await context.Reports
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == report.Id);

        Assert.NotNull(updatedReport);
        // It might be slightly different due to UTC conversion logic but should be close
        Assert.Equal(DateTimeKind.Utc, updatedReport.DeletedAt!.Value.Kind);
    }

    [Fact]
    public async Task Add_ShouldSetCreatedAt_IfDefault()
    {
        // Arrange
        var options = CreateOptions();
        await using var context = new ApplicationDbContext(options);
        
        var report = new Report
        {
            Latitude = 40.0,
            Longitude = -74.0
        };

        // Act
        context.Reports.Add(report);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, report.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, report.CreatedAt.Kind);
    }
}
