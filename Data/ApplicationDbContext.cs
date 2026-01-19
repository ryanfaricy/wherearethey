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
    public DbSet<AdminLoginAttempt> AdminLoginAttempts { get; set; }
    public DbSet<AdminPasskey> AdminPasskeys { get; set; }
    public DbSet<SystemSettings> Settings { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            // Ensure Kind is UTC for PostgreSQL
            if (entry.Entity.CreatedAt.Kind != DateTimeKind.Utc)
            {
                entry.Entity.CreatedAt = DateTime.SpecifyKind(entry.Entity.CreatedAt, DateTimeKind.Utc);
            }

            if (entry.Entity.DeletedAt.HasValue && entry.Entity.DeletedAt.Value.Kind != DateTimeKind.Utc)
            {
                entry.Entity.DeletedAt = DateTime.SpecifyKind(entry.Entity.DeletedAt.Value, DateTimeKind.Utc);
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocationReport>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Alert>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Donation>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Feedback>().HasQueryFilter(e => e.DeletedAt == null);

        modelBuilder.Entity<LocationReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.DeletedAt);
            entity.HasIndex(e => new { e.CreatedAt, e.DeletedAt, e.Latitude, e.Longitude });
            entity.Property(e => e.Latitude).IsRequired();
            entity.Property(e => e.Longitude).IsRequired();
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExternalId).HasDefaultValueSql("gen_random_uuid()");
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => new { e.DeletedAt, e.IsVerified, e.Latitude, e.Longitude });
            entity.HasIndex(e => e.DeletedAt);
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
            entity.HasIndex(e => e.DeletedAt);
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
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.DeletedAt);
            entity.HasIndex(e => e.UserIdentifier);
        });

        modelBuilder.Entity<AdminLoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IpAddress);
        });

        modelBuilder.Entity<AdminPasskey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CredentialId).IsUnique();
        });

        modelBuilder.Entity<SystemSettings>().HasData(new SystemSettings
        {
            Id = 1,
            ReportExpiryHours = 6,
            ReportCooldownMinutes = 5,
            AlertLimitCount = 3,
            MaxReportDistanceMiles = 5.0m,
            MapboxToken = null,
            DonationsEnabled = true,
            DataRetentionDays = 30,
        });
    }
}
