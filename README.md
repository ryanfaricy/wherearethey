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
- **Containerization**: Docker & Docker Compose
- **Payment Processing**: Stripe.net 47.4.0
- **Testing**: xUnit with EF Core InMemory provider

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
- .NET 10.0 SDK or later (for local development)
- Docker and Docker Compose (recommended for deployment)
- JetBrains Rider (or Visual Studio/VS Code)

### Fast Deployment with Docker (Recommended)

1. **Clone the repository**
   ```bash
   git clone https://github.com/ryanfaricy/wherearethey.git
   cd wherearethey
   ```

2. **Configure Environment**
   
   Edit `docker-compose.yml` or set environment variables for Stripe and Email.

3. **Run with Docker Compose**
   ```bash
   docker-compose up -d
   ```
   The application will be available at `http://localhost:8080`.

### Local Development & Secrets

To debug locally without committing secrets to the repository:

1. **Initialize User Secrets**:
   ```bash
   dotnet user-secrets set "Stripe:SecretKey" "your_test_key"
   dotnet user-secrets set "Stripe:PublishableKey" "your_test_key"
   dotnet user-secrets set "Email:SmtpPass" "your_smtp_password"
   dotnet user-secrets set "Email:SmtpUser" "your_smtp_user"
   ```

2. **Start the Database**:
   The application requires PostgreSQL. Even if you are running the app in your IDE (Rider/VS), the database must be running. You don't need to install PostgreSQL manually if you have Docker:
   ```bash
   docker-compose up -d db
   ```
   This starts only the database container and exposes it on `localhost:5432`.

3. **Database Connection**:
   By default, the app expects PostgreSQL at `localhost`. You can verify or change this via user-secrets:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=wherearethey;Username=postgres;Password=postgres"
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

### ☁️ Cloud Deployment (Railway)

This application is optimized for [Railway](https://railway.app/).

1. **Connect your GitHub Repo**: Railway will automatically detect the `Dockerfile`.
2. **Add a PostgreSQL Database**: Click "New" -> "Database" -> "Add PostgreSQL".
3. **Environment Variables**:
   The app will automatically detect Railway's `DATABASE_URL`. You only need to add your secrets:
   - `Stripe__SecretKey`
   - `Stripe__PublishableKey`
   - `Email__SmtpUser`
   - `Email__SmtpPass`
   
   *Note: Use double underscores (`__`) for nested .NET configuration sections.*

### 🐳 Running with Docker Locally

To start the entire stack (App + DB) locally:
```bash
docker-compose up -d
```
You can provide secrets via a `.env` file in the root directory:
```env
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
SMTP_USER=user@domain.com
SMTP_PASS=password
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
- **Production Ready**: PostgreSQL for high-concurrency and reliability
- **Server-Side Rendering**: Blazor Server for efficient updates
- **Indexed Database Queries**: Optimized for fast lookups
- **Geographic Distance Calculations**: Haversine formula for accurate radius searches

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

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=wherearethey.db"
  },
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_..."
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

## 📞 Support

For issues or questions, please open a GitHub issue or contact the maintainer.

---

**Remember**: People's lives may depend on this system. Treat it with the seriousness it deserves.
