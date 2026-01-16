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
- **Testing**: xUnit with 70+ comprehensive tests

For detailed instructions on local setup, secrets management, and testing, see [DEVELOPMENT.md](DEVELOPMENT.md).

## 🚀 Quick Start
1. `docker-compose up -d`
2. `dotnet run`

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

The project includes 70+ unit and integration tests covering all critical paths. See [DEVELOPMENT.md](DEVELOPMENT.md) for details.

## 📄 License

This project is open source and available for emergency response and humanitarian purposes.

---
*Built with care for life-critical situations. People's safety depends on reliability.*

## 🆘 Emergency Use

This platform is designed for life-critical situations. If you're in immediate danger, always call emergency services first (911, 112, etc.) before using this application.

## 👥 Contributing

Contributions are welcome! Please see [DEVELOPMENT.md](DEVELOPMENT.md) for contribution guidelines.

---

**Remember**: People's lives may depend on this system. Treat it with the seriousness it deserves.
