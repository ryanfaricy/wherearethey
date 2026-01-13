# AreTheyHere - Anonymous Emergency Incident Tracking

A production-ready, mobile-first Blazor Server application for anonymous location reporting and emergency alerts. Built with .NET 10, Radzen Blazor Components, and Entity Framework Core.

## 🚨 Core Features

- **Anonymous Reporting**: One-tap reporting of incidents without user accounts.
- **Live Heat Map**: Visual distribution of recent reports with color-coded urgency.
- **Smart Alerts**: Subscription-based email alerts for specific geographic zones (encrypted at rest).
- **Security-First**: GUID-based External IDs for all public links to prevent enumeration.
- **Anti-Spam**: Intelligent cooldowns, distance verification, and admin brute-force protection.
- **PWA Ready**: "Add to Home Screen" support for a native-like experience.
- **Geocoding**: Automatic translation of coordinates to approximate addresses.
- **Admin Control Panel**: Comprehensive management of reports, alerts, and system settings.
- **Donation Integration**: Support the service through secure Square payments.

## 🏗️ Technology Stack
- **Framework**: .NET 10.0 (Blazor Server)
- **UI Components**: Radzen Blazor Components (Material Design)
- **Database**: PostgreSQL with EF Core 9.0
- **Payments**: Square .NET SDK
- **Maps**: Leaflet & Mapbox API
- **Concurrency**: `IDbContextFactory` for high-traffic stability (10,000+ connections)
- **Testing**: xUnit with 60+ comprehensive tests

## 🚀 Getting Started

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL (or use the provided Docker Compose)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/ryanfaricy/wherearethey.git
   cd wherearethey
   ```

2. **Run with Docker (Recommended)**
   ```bash
   docker-compose up -d
   ```
   The app will be available at `http://localhost:8080`.

3. **Configure Secrets**
   Set the following environment variables or use .NET User Secrets:
   - `Square:ApplicationId`, `Square:AccessToken`, `Square:LocationId`, `Square:Environment`
   - `Email:GraphClientId`, `Email:GraphTenantId`, `Email:GraphClientSecret`
   - `AdminPassword`: The password for the Admin Control Panel.
   - `BaseUrl`: The public URL of your deployment.

## 🔒 Security & Privacy

- **No PII**: We do not collect names or IP addresses for regular users.
- **Encryption**: Alert emails are encrypted at rest using the AES-256 Data Protection API.
- **Anonymous Identifiers**: We use client-side generated GUIDs for rate limiting without tracking users.
- **Brute-Force Protection**: Admin login attempts are tracked by IP and locked out after repeated failures.

## ⚡ Performance

- **Optimized Queries**: All read-only operations use `AsNoTracking()`.
- **Bounding Box Filters**: Geographic queries use database-level bounding box filters before memory-intensive distance calculations.
- **Response Compression**: Gzip/Brotli compression enabled for faster load times.
- **Background Processing**: Alert notifications are processed in background tasks to keep the UI responsive.

## 🧪 Testing

The project includes 60+ unit and integration tests covering all critical paths.
```bash
cd WhereAreThey.Tests
dotnet test
```

## 📄 License

This project is open source and available for emergency response and humanitarian purposes.

---
*Built with care for life-critical situations. People's safety depends on reliability.*

## 🛠️ Development

### Database Migrations

The application uses EF Core Migrations for schema management. Migrations are automatically applied on startup.

**To add a new migration after model changes:**
```bash
dotnet ef migrations add <MigrationName>
```

**To apply migrations manually:**
```bash
dotnet ef database update
```

**Cloud Deployment:**
For high-concurrency cloud deployments, it is recommended to switch the database provider from SQLite to a managed service like Azure SQL or PostgreSQL. This ensures data persistence and better performance under heavy write loads.

### Adding New Features

1. Add models in `Models/`
2. Update `ApplicationDbContext`
3. Create service in `Services/`
4. Add page in `Components/Pages/`
5. Write tests in `WhereAreThey.Tests/`

## 📊 Database Schema

### LocationReports
- `Id` (Primary Key)
- `Latitude`, `Longitude` (Required)
- `Timestamp` (Indexed)
- `Message` (Optional)
- `ReporterIdentifier` (Anonymous session ID)
- `IsEmergency` (Boolean)

### Alerts
- `Id` (Primary Key)
- `Latitude`, `Longitude` (Required)
- `RadiusKm` (Double)
- `Message` (Optional)
- `CreatedAt`, `ExpiresAt`
- `IsActive` (Indexed)

### Donations
- `Id` (Primary Key)
- `Amount` (Decimal, precision 18,2)
- `Currency` (Default: USD)
- `DonorEmail`, `DonorName` (Optional)
- `CreatedAt` (Indexed)
- `StripePaymentIntentId`
- `Status` (pending/completed/failed)

## 🎨 UI/UX Design

- **Mobile-First**: Responsive design with collapsible sidebar
- **Radzen Material Theme**: Modern, clean interface
- **Accessibility**: ARIA labels and keyboard navigation
- **Icon System**: Material Design icons via Radzen
- **Color-Coded Status**: Visual indicators for emergency reports

## 🔧 Configuration

### Secrets Management
Sensitive data like Stripe and SMTP keys should be stored in .NET User Secrets during development:
```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
```

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=wherearethey;Username=postgres;Password=postgres"
  }
}
```

### Kestrel Server Configuration
Configured for high concurrency in `Program.cs`:
- MaxConcurrentConnections: 10,000
- MaxConcurrentUpgradedConnections: 10,000

## 📄 License

This project is open source and available for emergency response and humanitarian purposes.

## 🆘 Emergency Use

This platform is designed for life-critical situations. If you're in immediate danger, always call emergency services first (911, 112, etc.) before using this application.

## 👥 Contributing

Contributions are welcome! Please ensure:
1. All tests pass
2. New features include tests
3. Code follows existing patterns
4. Security vulnerabilities are reported privately

## 🛠️ Troubleshooting

### PostgreSQL Migration Issues
If you encounter an error like `relation "Alerts" already exists` during startup, it means your database schema is out of sync with the migration history. 

For development environments, you can reset the database using:
```bash
dotnet ef database drop -f
dotnet ef database update
```

### Email Delivery Issues on Railway
If you experience issues with email delivery:
1.  **Multi-Provider Fallback**: The application now uses a fallback chain: Microsoft Graph -> SMTP. If one fails, it automatically tries the next.
2.  **Verify Secrets**: Ensure `Email:GraphClientId`, `Email:GraphTenantId`, `Email:GraphClientSecret`, and `Email:GraphSenderUserId` are correctly set in Railway environment variables.
3.  **Logs**: Check the application logs for "Attempting to send email via..." or any "Failed to send email via..." error messages. The logs will show which provider succeeded or why they failed.
4.  **SMTP Last Resort**: If Microsoft Graph fails, the app will attempt SMTP via the configured `SmtpServer`.

## 📞 Support

For issues or questions, please open a GitHub issue or contact the maintainer.

---

**Remember**: People's lives may depend on this system. Treat it with the seriousness it deserves.
