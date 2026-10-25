# MediSlot

Hospital appointment booking REST API built with .NET 10, EF Core and SQL Server.

## Features
- JWT authentication with refresh token rotation and reuse detection
- Roles: Admin, Doctor, Patient (Admin is seeded, patients self-register, Admin creates doctors)
- Doctor schedules and automatic slot generation
- Appointment booking with optimistic concurrency (RowVersion) to prevent double booking
- Cancellation rule (configurable cutoff) and Complete / NoShow flow
- Pagination, filtering and sorting
- SQL Server stored procedure and indexes for a doctor-wise report
- Hangfire background jobs: nightly slot generation, appointment reminders
- Serilog logging, health checks, FluentValidation, global exception handling

## Architecture
Clean Architecture: `MediSlot.API`, `MediSlot.Application`, `MediSlot.Domain`, `MediSlot.Infrastructure`

## Run locally
Prerequisites: .NET 10 SDK, SQL Server

1. Clone the repo
2. Check the connection string in `src/MediSlot.API/appsettings.json`
3. Set secrets (nothing sensitive is stored in the repo):

```
cd MediSlot
dotnet user-secrets init --project src/MediSlot.API
dotnet user-secrets set "Jwt:Key" "<random string, at least 32 characters>" --project src/MediSlot.API
dotnet user-secrets set "Admin:Name" "System Admin" --project src/MediSlot.API
dotnet user-secrets set "Admin:Email" "admin@example.com" --project src/MediSlot.API
dotnet user-secrets set "Admin:Password" "<your admin password>" --project src/MediSlot.API
```

4. Create the database:

```
dotnet ef database update --project src/MediSlot.Infrastructure --startup-project src/MediSlot.API
```

5. Run:

```
dotnet run --project src/MediSlot.API
```

- Swagger: https://localhost:7021/swagger
- Hangfire dashboard: https://localhost:7021/hangfire
- Health: https://localhost:7021/health/ready

## Future improvements
Unit tests, CI pipeline, Docker support, real email notifications.    D:\MediSlot\MediSlot.slnx