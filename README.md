# PeerReview Clean Architecture (.NET 8)

**Layers**
- `PeerReview.Domain` — Entities, enums, base abstractions (no dependencies)
- `PeerReview.Application` — DTOs, Validators, Interfaces (`IJwtTokenService`, `IFileStorage`)
- `PeerReview.Infrastructure` — EF Core (`AppDbContext`), JWT implementation, File storage, Seeding
- `PeerReview.Api` — Web API (controllers, DI wiring, ProblemDetails, Swagger)

**Features**
- JWT Auth (Register prevents Admin role)
- FluentValidation + ProblemDetails
- Users/Roles/Questions/Answers/Assignments/Lookups endpoints
- File uploads for Answers (Local storage under `wwwroot/uploads`)
- Dashboard metrics with role flags (fine-grained visibility)
- Soft delete with global query filters
- Ready for EF **Migrations** (use `dotnet ef`)

## Quick Start
1. Update `src/PeerReview.Api/appsettings.json` (ConnectionStrings, Jwt Key).
2. From `src/PeerReview.Api`:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Swagger: `http://localhost:5215/swagger`

## Migrations (recommended)
From `src/PeerReview.Api` or create a classlib for migrations targeting Infrastructure:
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Initial --project ../PeerReview.Infrastructure --startup-project .
dotnet ef database update --project ../PeerReview.Infrastructure --startup-project .
```
