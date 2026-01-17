# AreTheyHere - Development Guide

This document provides detailed instructions for setting up your development environment and contributing to the project.

## 🛠️ Local Setup

### 1. Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)
- An IDE (JetBrains Rider, VS Code, or Visual Studio 2022+)

### 2. Database Setup
The easiest way to run the database is via Docker Compose:
```bash
docker-compose up -d db
```
This starts a PostgreSQL instance on port 5432.

### 3. Configuration & Secrets
The app uses `appsettings.json` and .NET User Secrets.

#### Required Secrets:
- `Square:AccessToken`: Get from [Square Developer Dashboard](https://developer.squareup.com/)
- `Email:GraphClientSecret`: Get from Azure Portal (for Microsoft Graph email)
- `Mapbox:Token`: Get from [Mapbox](https://www.mapbox.com/) (needed for geocoding and static maps)

#### Set secrets locally:
```bash
dotnet user-secrets set "Square:AccessToken" "YOUR_TOKEN"
dotnet user-secrets set "Email:GraphClientSecret" "YOUR_SECRET"
dotnet user-secrets set "Mapbox:Token" "YOUR_TOKEN"
```

### 4. Running the App
```bash
dotnet run --project WhereAreThey.csproj
```
The app will be available at `https://localhost:7114` (or similar, check console output).

## 🧪 Testing

### Backend & Component Tests
We use xUnit for backend testing and [bUnit](https://bunit.dev/) for testing Blazor components.
Component tests are located in `WhereAreThey.Tests/ComponentTests`.

Run all .NET tests with:
```bash
dotnet test
```

### JavaScript Tests
We use [Vitest](https://vitest.dev/) for JavaScript testing.
Tests are located in the `tests/` directory.

#### Prerequisites
- Node.js (v18+) installed

#### Running JS Tests
```bash
npm install
npm test
```

### Mocking External Services
Backend tests use `Moq` to mock `IEmailService`, `IGeocodingService`, etc. to avoid hitting real APIs during test execution.
 bUnit tests mock JS interop and standard services.

## 🏗️ Project Structure

- **Components/Pages**: Blazor pages (Home is the main map).
- **Services**: Business logic and database interactions.
- **Models**: EF Core entities and ViewModels.
- **wwwroot/js**: Leaflet map implementation and JS helpers.

## 🔄 Database Migrations

When you change a model:
1. Add a migration: `dotnet ef migrations add YourMigrationName`
2. Update database: `dotnet ef database update`

## 🌍 Localization

We support multiple languages using `.resx` files in the `Resources` folder.
To add a new language:
1. Copy `App.resx` to `App.XX.resx` (where XX is the language code).
2. Translate the values.
3. Add the language code to `supportedCultures` in `Program.cs`.

## 🚢 Deployment

The project is configured for easy deployment to **Railway** or any platform supporting Docker.
Ensure `DATABASE_URL` and `BaseUrl` are set in your production environment.

### CI/CD in Docker
The `Dockerfile` is configured to run both .NET and JavaScript tests during the build stage. This ensures that any failing tests will prevent a broken build from being deployed to Railway or any other Docker-based environment.
