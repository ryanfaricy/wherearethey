# WhereAreThey - Emergency Location Tracking System

A critical, mobile-first Blazor Server application for anonymous location reporting and emergency alerts. Built with .NET 10, Radzen Blazor Components, and Entity Framework Core.

## 🚨 Mission-Critical Features

- **Anonymous Location Reporting**: Report locations without revealing personal information
- **Real-Time Heat Maps**: Visualize reported locations to identify areas of concern
- **Distance-Based Alerts**: Create alerts for specific geographic areas
- **Donation Integration**: Support the service through Stripe payments
- **Mobile-First Design**: Optimized for emergency situations on any device
- **High Concurrency**: Configured to handle thousands of simultaneous connections
- **Test-Driven Development**: Comprehensive test coverage with xUnit

## 🏗️ Architecture

### Technology Stack
- **Framework**: .NET 10.0 (Blazor Server with Interactive rendering)
- **UI Components**: Radzen Blazor Components 5.7.6 (Material Design)
- **Database**: SQLite with Entity Framework Core 9.0
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
└── AlertServiceTests.cs
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

3. **Configure Stripe (Optional)**
   
   Edit `appsettings.json` to add your Stripe keys:
   ```json
   "Stripe": {
     "PublishableKey": "pk_test_YOUR_KEY",
     "SecretKey": "sk_test_YOUR_KEY"
   }
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

### 1. Location Reporting
- Anonymous submission of GPS coordinates
- Optional emergency flag for critical situations
- Automatic timestamp recording
- Uses browser geolocation API

### 2. Heat Map Visualization
- View all reports from the last 24 hours (configurable)
- Filter by emergency status
- See geographic distribution of reports
- Export data for analysis

### 3. Distance-Based Alerts
- Create alerts for specific locations
- Set radius (in kilometers) for monitoring
- Optional expiration dates
- Manage active alerts

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
- **Lightweight Database**: SQLite for minimal overhead
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
- ✅ Location report creation
- ✅ Time-range filtering
- ✅ Radius-based queries
- ✅ Alert creation and management
- ✅ Alert expiration handling

## 🛠️ Development

### Database Migrations

The application uses automatic database creation. To manually manage migrations:

```bash
dotnet ef migrations add InitialCreate
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
