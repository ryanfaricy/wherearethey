# WhereAreThey - Emergency Location Tracking System

A critical, mobile-first Blazor Server application for anonymous location reporting and emergency alerts. Built with .NET 10, Radzen Blazor Components, and Entity Framework Core.

## 🚨 Mission-Critical Features

- **ARE THEY HERE?**: Instant heat map visualization of recent reports
- **THEY ARE HERE!**: One-tap anonymous location reporting
- **Distance-Based Alerts**: Sign up with encrypted email for location-specific notifications
- **Donation Integration**: Support the service through simplified Stripe payments
- **Mobile-First Design**: Radical simple UI optimized for phone use in emergencies
- **High Concurrency**: Configured to handle 10,000+ simultaneous connections
- **Data Privacy**: Emails are stored encrypted at rest using AES-256 (via Data Protection API)

## 🏗️ Architecture

### Technology Stack
- **Framework**: .NET 10.0 (Blazor Server with Interactive rendering)
- **UI Components**: Radzen Blazor Components 5.7.6 (Material Design)
- **Database**: PostgreSQL with Entity Framework Core 9.0
- **Concurrency**: IDbContextFactory for high-traffic stability
- **Payment Processing**: Stripe.net 47.4.0
- **Testing**: xUnit with EF Core InMemory provider
- **Deployment**: Docker-ready (Railway optimized)

### Project Structure
```
WhereAreThey/
├── Components/
│   ├── Layout/         # MainLayout, NavMenu (Radzen-based)
│   └── Pages/          # Home, Report, HeatMap, Alerts, Donate
├── Data/               # ApplicationDbContext
├── Models/             # LocationReport, Alert, Donation
├── Services/           # LocationService, AlertService, DonationService
└── wwwroot/            # Static assets and JavaScript

WhereAreThey.Tests/
├── LocationServiceTests.cs
├── AlertServiceTests.cs
├── GeoUtilsTests.cs
├── DonationServiceTests.cs
├── AppThemeServiceTests.cs
└── SmtpEmailServiceTests.cs
```

## 🚀 Getting Started

### Prerequisites
- .NET 10.0 SDK or later
- JetBrains Rider (or Visual Studio/VS Code)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/ryanfaricy/wherearethey.git
   cd wherearethey
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure Secrets (Optional)**
   
   It is recommended to use .NET User Secrets for sensitive information:
   ```bash
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY"
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"
   dotnet user-secrets set "Email:SmtpServer" "your-smtp-server"
   dotnet user-secrets set "Email:SmtpUser" "your-smtp-user"
   dotnet user-secrets set "Email:SmtpPass" "your-smtp-password"
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```
   
   The application will be available at `https://localhost:5001` or `http://localhost:5000`

5. **Run tests**
   ```bash
   cd ../WhereAreThey.Tests
   dotnet test
   ```

## 📱 Features

### 1. THEY ARE HERE! (Location Reporting)
- Radical simple submission of GPS coordinates
- Optional emergency flag for critical situations
- Automatic timestamp recording
- Uses browser geolocation API

### 2. ARE THEY HERE? (Heat Map Visualization)
- View all reports from the last 24 hours (configurable)
- Mobile-first list view of recent reports
- See geographic distribution of reports
- Color-coded urgency indicators

### 3. Distance-Based Alerts
- Create alerts with your email (encrypted at rest)
- Set radius (in kilometers) for monitoring
- Optional expiration dates
- Masked email display for privacy

### 4. Donation System
- Stripe integration for secure payments
- Preset donation amounts ($5, $10, $25, $50)
- Optional donor information
- Recent donations display

## 🔒 Security Features

- **Anonymous Reporting**: No authentication required for reporting
- **SQL Injection Prevention**: Parameterized queries via EF Core
- **XSS Protection**: Blazor's built-in sanitization
- **HTTPS Enforcement**: Configured for production environments
- **Dependency Vulnerability Management**: Updated to secure package versions

## ⚡ Performance & Scalability

- **High Concurrency Configuration**: Handles 10,000+ concurrent connections
- **PostgreSQL**: Production-grade database for robust data handling
- **DbContextFactory**: Efficiently manages database operations in Blazor Server
- **Server-Side Rendering**: Blazor Server for efficient updates
- **Indexed Database Queries**: Optimized for fast lookups
- **Geographic Distance Calculations**: Haversine formula for accurate radius searches

## 🐳 Docker & Cloud Deployment

The application is fully dockerized for both local development and cloud deployment (e.g., Railway).

### ⚡ Quick Start (Docker Compose)
The fastest way to get everything running locally with PostgreSQL:

1.  **Start the stack**:
    ```bash
    docker-compose up -d
    ```
    This starts the application at `http://localhost:8080` and a PostgreSQL database at `localhost:5432`.

2.  **Configure Secrets (Optional)**:
    Copy `.env.example` to `.env` and fill in your Stripe and SMTP keys. Docker Compose will automatically pick them up.
    ```bash
    cp .env.example .env
    ```

### 💻 Local Development (Hybrid)
If you want to debug in your IDE while using the Docker-managed database:

1.  **Start only the database**:
    ```bash
    docker-compose up -d db
    ```
2.  **Run the app normally**:
    ```bash
    dotnet run
    ```
    The app will connect to the PostgreSQL instance running in Docker because the port `5432` is exposed to your host.

### 🚆 Deployment to Railway
Railway will automatically detect the `Dockerfile` and deploy the application.

1.  **Add PostgreSQL**: Add the PostgreSQL plugin to your Railway project. Railway provides the `DATABASE_URL` automatically.
2.  **Configuration**:
    *   `PORT`: Automatically handled by Railway.
    *   **Secrets**: Add your `Stripe__SecretKey`, `Email__SmtpPass`, etc., as environment variables in the Railway dashboard.

## 🧪 Testing

The project includes comprehensive unit tests:

```bash
cd WhereAreThey.Tests
dotnet test --verbosity normal
```

### Test Coverage
- ✅ Location report creation and time-range filtering (8 tests)
- ✅ Geographic radius queries and accuracy (3 tests)
- ✅ Alert creation, management, and expiration (7 tests)
- ✅ Cross-user alert integration (User A reports, User B alerted)
- ✅ Donation recording and status updates (3 tests)
- ✅ Theme state and event management (3 tests)
- ✅ Email service fallback and error resilience (1 test)
- ✅ Radius limit enforcement (160.9km)
- ✅ Encrypted email at rest verification
- ✅ 100% Pass Rate (25 tests total)

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

## 📞 Support

For issues or questions, please open a GitHub issue or contact the maintainer.

---

**Remember**: People's lives may depend on this system. Treat it with the seriousness it deserves.
