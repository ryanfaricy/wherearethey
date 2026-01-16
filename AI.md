# AreTheyHere - AI Context & Development Guidelines

This document provides high-level context, design philosophy, and technical guidelines for AI agents (like GitHub Copilot, Cursor, or Junie) working on the AreTheyHere project.

## 🎯 Project Goals
- **Crisis Response**: Provide a reliable, anonymous way to report and track incidents during emergencies.
- **Privacy First**: Zero collection of PII. No user accounts.
- **Mobile Optimized**: Fast, responsive, and resilient to poor connectivity.

## 🧠 Architectural North Stars

### 1. Anonymous State Management
- We use a client-side generated GUID (`UserIdentifier`) stored in `localStorage`.
- This GUID is passed to services to enforce rate limits and associate alerts/reports with a "session" without knowing who the user is.
- **AI Guideline**: When adding features that require "user" context, always use `UserIdentifier` and never ask for or store names/emails (except for verified alerts, which are encrypted).

### 2. Service-Oriented Blazor
- Logic belongs in Services, not in `.razor` components.
- Services should use `IDbContextFactory<ApplicationDbContext>` to ensure thread safety in Blazor Server's multi-threaded environment.
- **AI Guideline**: If a component's `@code` block exceeds 200 lines, look for opportunities to extract logic into a Scoped or Singleton service.

### 3. Threading & UI Updates
- Since this is Blazor Server, event handlers in components triggered by services **MUST** use `InvokeAsync(StateHasChanged)` to ensure the update happens on the UI thread.
- Failing to do so will result in `System.InvalidOperationException: The current thread is not associated with the Dispatcher`.
- **AI Guideline**: Always wrap `StateHasChanged()` or any UI-interacting code in `InvokeAsync(...)` when inside a service event handler.

### 4. Real-time Updates via Event Bus
- `IEventService` is the central hub for all cross-component communication.
- When data changes in a service, it **must** notify the event bus.
- **AI Guideline**: Always trigger `Notify...` methods in `IEventService` after successful DB operations that affect the UI.

### 5. Database & Performance
- Use `AsNoTracking()` for all read-only queries.
- Prefer database-level filtering (e.g., bounding boxes for coordinates) over in-memory calculations.
- **AI Guideline**: Check `LocationService` or `AlertService` for examples of geographic filtering.

## 🛠️ Tech Stack Specifics
- **.NET 10**: Use latest C# features (primary constructors, etc.).
- **Radzen Blazor**: Use Radzen components for UI to maintain consistency.
- **Leaflet.js**: Map logic is primarily in `wwwroot/js/map.js`. JS Interop is used to bridge the gap.
- **Hangfire**: Used for background tasks like sending alert emails and geocoding.

## 🧪 Testing Expectations
- All business logic in `Services/` must have corresponding tests in `WhereAreThey.Tests/`.
- Use `Moq` for dependencies and `IDbContextFactory` mocks for database testing.

## 📂 Key File Map
- `Program.cs`: Dependency injection and middleware.
- `Components/Pages/Home.razor`: The main map interface (High complexity).
- `Services/ReportService.cs`: Core incident reporting logic.
- `Services/AlertService.cs`: Geofencing and email alert logic.
- `wwwroot/js/map.js`: Leaflet map initialization and markers.

## 🛑 Critical Constraints
- **Do NOT** add IP logging.
- **Do NOT** add user registration/login for public users.
- **Do NOT** store unencrypted emails in the database.
- **Do NOT** use `DbContext` directly in components (use `IDbContextFactory`).
