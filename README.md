# GymTracker API

GymTracker is an ASP.NET Core Web API for organizing fitness data. It uses Entity Framework Core with PostgreSQL and includes migrations, development seed data, Swagger/OpenAPI documentation, and synchronization with the WGER exercise API.

The current public repository contains the backend API. The previous frontend path was an unusable Git reference without `.gitmodules`, so this project is not presented as a full-stack application until the frontend source is restored correctly.

## Verified functionality

- Exercise browsing, searching, filtering, favorites, and WGER synchronization
- Reusable workout plans with ordered exercises
- Completed workout sessions and exercise details
- Body-weight history
- Dashboard totals and recent activity
- User-scoped plans, sessions, favorites, and weight records
- EF Core migrations and PostgreSQL persistence

## Technology

- C# and .NET 10
- ASP.NET Core Web API
- Entity Framework Core and Npgsql
- PostgreSQL
- Swagger/OpenAPI
- WGER API integration

## Project structure

```text
backend/GymTracker.Api/
├── Controllers/
├── Data/
├── Dtos/
├── Entities/
├── Migrations/
├── Services/
└── Program.cs
```

## Configuration

Copy `backend/GymTracker.Api/appsettings.Example.json` to `appsettings.Local.json`, or use environment variables:

```text
ConnectionStrings__DefaultConnection
Wger__ApiToken
```

Do not commit local configuration. The database credentials and WGER token previously committed to this repository must be rotated. Removing them from the current branch does not remove them from Git history.

## Run locally

Requirements: .NET 10 SDK and PostgreSQL.

```bash
dotnet restore GymTracker.sln
dotnet ef database update --project backend/GymTracker.Api
dotnet run --project backend/GymTracker.Api
```

Swagger is available at the URL printed by ASP.NET Core when the API runs in development.

## Profile scoping and security limitation

Requests are scoped by the `X-Profile-Key` header. Missing headers use a local default profile. This is a convenience mechanism, not authentication or authorization; clients that know another profile key can select it. The API should not be exposed as a multi-user service without a verified authentication and authorization layer.

## Current limitations

- Frontend source is not included in a usable form.
- The repository has no automated test project.
- Several zero-byte service/interface placeholders remain and should only be removed after a successful .NET build confirms they are unused.
