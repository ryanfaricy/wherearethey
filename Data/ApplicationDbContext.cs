using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Models;

namespace WhereAreThey.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<LocationReport> LocationReports { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<Donation> Donations { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<SystemSettings> Settings { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocationReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.UserIdentifier);
            entity.HasIndex(e => e.EmailHash);
            entity.HasIndex(e => e.IsVerified);
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
        });

        modelBuilder.Entity<Donation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmailHash).IsUnique();
            entity.HasIndex(e => e.Token).IsUnique();
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserIdentifier);
        });

        modelBuilder.Entity<SystemSettings>().HasData(new SystemSettings
        {
            Id = 1,
            ReportExpiryHours = 6,
            ReportCooldownMinutes = 5,
            MaxReportDistanceMiles = 5.0m,
            MapboxToken = null,
            DonationsEnabled = true
        });
    }
}
