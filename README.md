# GymTracker

GymTracker is a full-stack fitness application for discovering exercises, creating reusable workout plans, logging completed sessions, and tracking body weight. The responsive browser interface is served by an ASP.NET Core API backed by PostgreSQL.

## Verified functionality

- Exercise browsing, searching, filtering, favorites, and WGER synchronization
- Reusable workout plans with ordered exercises
- Completed workout sessions and exercise details
- Body-weight history
- Dashboard totals and recent activity
- User-scoped plans, sessions, favorites, and weight records
- EF Core migrations and PostgreSQL persistence

## Technology

- Responsive single-page web interface (HTML, CSS, JavaScript)
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
├── wwwroot/
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

Open the URL printed by ASP.NET Core to use the application. Swagger is available at `/swagger` in development.

## Deploy on Railway

The included `Dockerfile` and `railway.json` package the frontend and API as one service.

1. Create a Railway project from this GitHub repository.
2. Add a PostgreSQL service to the same project.
3. Set the app service variable `DATABASE_URL` to `${{Postgres.DATABASE_URL}}` (adjust `Postgres` if the database service has a different name).
4. Generate a public domain for the app service. Railway uses `/health` to verify the deployment.

Database migrations and initial exercise import run automatically on first startup.

## Profile scoping and security limitation

Requests are scoped by the `X-Profile-Key` header. Missing headers use a local default profile. This is a convenience mechanism, not authentication or authorization; clients that know another profile key can select it. The API should not be exposed as a multi-user service without a verified authentication and authorization layer.

## Current limitations

- The repository has no automated test project.
- Several zero-byte service/interface placeholders remain and should only be removed after a successful .NET build confirms they are unused.
- Browser profiles are separated with a locally generated `X-Profile-Key`; this is a portfolio demo mechanism, not production authentication.
