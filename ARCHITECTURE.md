# AreTheyHere - AI-Friendly Architecture & Context

This document provides a high-level overview of the application architecture, data flow, and key patterns to help AI agents understand and contribute to the project effectively.

## 🚀 Core Philosophy
- **Radical Privacy**: We do NOT collect user data. No names, no IPs (except for admin login attempts), no tracking.
- **Mobile-First**: The UI is designed for emergency situations—one-tap actions and high visibility.
- **Anonymous Reporting**: Users report incidents without logging in. We use an anonymous GUID (`UserIdentifier`) stored in local storage to enforce anti-spam limits.
- **Reliability**: Multi-provider fallback systems (Email) and high-concurrency configurations.

## 🏗️ Technical Stack
- **Framework**: .NET 10.0 (Blazor Server)
- **UI**: Radzen Blazor Components (Material Theme)
- **Database**: PostgreSQL with Entity Framework Core 10.0
- **Patterns**: Generic Base Service, Event Aggregator, Visibility Policies
- **Payments**: Square (via Square .NET SDK and Web Payments SDK)
- **Maps**: Leaflet (Frontend) & Mapbox (Geocoding / Static Maps)
- **Localization**: .NET `IStringLocalizer` with `.resx` files.

## 🏗️ Architectural Patterns

### 1. Generic Base Service (DRY & SOLID)
Most entities (`LocationReport`, `Alert`, `Donation`, `Feedback`) implement `IAuditable`, which includes `CreatedAt` and `DeletedAt`. Business services inherit from `BaseService<T>`, centralizing:
- **Soft-Delete**: Automatic handling of `DeletedAt` and notifications.
- **Admin Filtering**: `GetAllAsync(bool isAdmin)` handles `IgnoreQueryFilters()` automatically.

### 2. Event Aggregation Pattern
`IEventService` provides a generic `OnEntityChanged` event. This allows UI components (like Admin tabs or the Overview dashboard) to listen to a single event stream instead of multiple specialized events.

### 3. Visibility Policies
The `VisibilityPolicy` helper centralizes the logic for "ShouldShow" (e.g., `isAdmin || DeletedAt == null`), ensuring consistency between the Map, Lists, and API responses.

## 🧩 Key Components & Services

### Services
- **ReportService (Singleton)**: Manages the lifecycle of incident reports (Add, Get, Delete). Provides real-time notifications for UI updates via `IEventService`.
- **LocationService (Singleton)**: Handles specialized spatial and geographic logic, such as radius searches and local time formatting based on coordinates.
- **ReportProcessingService (Scoped)**: Handles background processing for new reports, including alert matching, geocoding, and email notifications.
- **AlertService (Scoped)**: Manages distance-based user alerts. Handles email encryption at rest using the Data Protection API.
- **SettingsService (Singleton)**: Manages global system settings (limits, tokens, toggle features) with a 1-minute memory cache.
- **Validators (Singleton)**: FluentValidation classes (`LocationReportValidator`, `AlertValidator`, `FeedbackValidator`) encapsulate complex validation logic for reports, alerts, and feedback.
- **AdminService (Scoped)**: Manages admin authentication and brute-force protection (IP tracking and lockout).
- **DonationService (Scoped)**: Manages Square payment processing and recording.
- **MapService (Scoped)**: Encapsulates Leaflet.js interop for heatmap and marker management.
- **ClientStorageService (Scoped)**: Manages browser local storage and client-side state (UserIdentifier).
- **GeocodingService (Scoped)**: Interface with Mapbox for forward (address to coords) and reverse (coords to address) geocoding.
- **EmailService (Scoped)**: A fallback chain: `MicrosoftGraphEmailService` -> `SmtpEmailService`.

### Data Models
- **LocationReport**: An incident report. Contains coordinates, message, and `ExternalId` (GUID) for public URLs.
- **Alert**: A user's subscription to a zone. Contains `EncryptedEmail`, `EmailHash` (for verification), coordinates, and `RadiusKm`.
- **Donation**: A record of a successful (or failed) donation.
- **SystemSettings**: Global configuration stored in DB.
- **AdminLoginAttempt**: Records IPs for failed/successful admin logins.

## 🔒 Security Patterns

### 1. External IDs vs. Primary Keys
We never expose integer Primary Keys in URLs. We use `ExternalId` (GUID) for `reportId` and `alertId` in query strings. This prevents ID enumeration and hides the exact number of records.

### 2. Email Encryption at Rest
User emails for alerts are encrypted using the `.NET Data Protection API` before being stored. Keys are persisted in the database. A SHA-256 `EmailHash` is used for verification lookups without decrypting the entire table.

### 3. Anti-Spam & Limits
- **Cooldowns**: Enforced for Reports, Alerts, and Feedback based on `UserIdentifier`.
- **Distance Check**: Reports must be within a configurable distance (default 5 miles) of the user's actual GPS location.
- **Admin Lockout**: 5 failed attempts from an IP results in a 15-minute lockout.

## ⚡ Performance Patterns

### 1. Database Efficiency
- **AsNoTracking()**: Used for all read-only queries in services to reduce memory overhead and CPU cycles.
- **Bounding Box Filters**: Used in `GetMatchingAlertsAsync` and `GetReportsInRadiusAsync` to filter records at the database level before performing expensive Haversine distance calculations in memory.
- **DbContextFactory**: Essential for Blazor Server to avoid thread-safety issues with DbContext.
- **Global Query Filters**: Soft-delete is enforced at the DB level using EF Core global query filters. Services can use `.IgnoreQueryFilters()` when admin access is required.

### 2. UI & Assets
- **Response Compression**: Enabled for HTTPS to speed up initial load.
- **High Concurrency**: Kestrel is configured for 10,000+ simultaneous connections.
- **Mobile Connectivity Optimization**: 
  - **Circuit Retention**: Server-side circuits are retained for 5 minutes during disconnects to support app switching on mobile.
  - **SignalR Resiliency**: Increased `ClientTimeoutInterval` (60s) and `HandshakeTimeout` (30s) to handle flaky mobile networks.
  - **Subtle Reconnection**: A 2-second delay and pill-shaped bottom notification replace the standard disruptive reconnection modal.

## 🧪 Testing Strategy
- **Unit Testing**: Business logic in services and validators is covered by xUnit tests.
- **Integration Testing**: Database interactions are tested using SQLite in-memory and EF Core In-Memory providers.
- **Mocking**: `Moq` is used to isolate services from external dependencies like `HttpClient`, `IEmailService`, and `IBackgroundJobClient`.
- **Validation Testing**: `FluentValidation` rules are extensively tested to ensure anti-spam and security constraints are honored.
- **Generic Component Testing**: `AdminTabBase` allows for testing shared UI logic (data loading, event handling) once for all entity types.

## 🤖 AI Development Guidelines
1. **Always Use `AsNoTracking()`** for read-only service methods.
2. **Follow Localization Patterns**: Add new strings to `App.resx` and `App.es.resx`. Use `L["Key"]` in components.
3. **Respect Anonymity**: Never add fields that collect PII (Personally Identifiable Information) unless it's for the Admin area.
4. **Use `IDbContextFactory`**: Always create a new context within service methods using `await using var context = await contextFactory.CreateDbContextAsync()`.
5. **Interface-Based DI**: Register all services via interfaces in `Program.cs`. Inject interfaces into constructors.
6. **Centralized Validation**: Use FluentValidation builders (`LocationReportValidator`, etc.) for any submission-related checks (spam, content, distance).
7. **Decouple Post-Processing**: Use `IReportProcessingService` for non-blocking tasks that occur after a report is saved.
8. **Inherit from `BaseService<T>`**: New business services for auditable entities should inherit from `BaseService<T>` to gain automatic soft-delete and admin filtering.
9. **Use `OnEntityChanged`**: Always use the generic `OnEntityChanged` event for UI updates related to auditable entities.
10. **Update Tests**: Any logic change in services MUST be accompanied by a test update in `WhereAreThey.Tests`.
