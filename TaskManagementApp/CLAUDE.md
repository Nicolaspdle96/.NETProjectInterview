# CLAUDE.md

This file guides Claude Code when working in this repository. Read it fully before making changes.

## Project Overview

**TaskManager API** is a RESTful API for managing tasks. Authenticated users can create, read, update, and delete their own tasks. Each task belongs to exactly one user.

### Core Requirements
- CRUD operations on tasks
- Each task has: `title`, `description`, `status`, `due_date`
- Tasks are owned by a user; users can only access their own tasks
- User registration and login with JWT authentication
- Clean Architecture
- SQLite for persistence

## Tech Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 (LTS), C# latest |
| Web | ASP.NET Core Web API (Controllers) |
| ORM | Entity Framework Core + `Microsoft.EntityFrameworkCore.Sqlite` |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| Password hashing | `BCrypt.Net-Next` |
| Validation | `FluentValidation` |
| API docs | `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` (UI at `/scalar`) |
| Testing | xUnit, **Shouldly** (chosen over FluentAssertions: v8+ requires a commercial license), `Microsoft.AspNetCore.Mvc.Testing` |

Do **not** add MediatR, AutoMapper, or other heavy libraries unless explicitly asked. Use plain application services and manual mapping.

Package notes:
- `Microsoft.IdentityModel.JsonWebTokens` (token creation in Infrastructure) is pinned to the same version that `Microsoft.AspNetCore.Authentication.JwtBearer` brings in. Mixed `Microsoft.IdentityModel.*` versions fail at runtime; keep them aligned when upgrading.
- Infrastructure has a `FrameworkReference` to `Microsoft.AspNetCore.App` (options binding/validation) instead of extra `Microsoft.Extensions.*` packages.
- `FluentValidation.DependencyInjectionExtensions` registers all validators with `AddValidatorsFromAssembly`.

## Solution Structure (Clean Architecture)

```
TaskManager.sln
src/
  TaskManager.Domain/          # Entities, enums, domain rules. No dependencies.
  TaskManager.Application/     # Use cases, DTOs, interfaces, validators. Depends on Domain.
  TaskManager.Infrastructure/  # EF Core, SQLite, repositories, JWT, hashing. Depends on Application.
  TaskManager.Api/             # Controllers, middleware, DI composition, Program.cs. Depends on Application + Infrastructure.
tests/
  TaskManager.Domain.Tests/
  TaskManager.Application.Tests/
  TaskManager.Api.IntegrationTests/
```

### Dependency Rules (strict)
- **Domain** references nothing.
- **Application** references only Domain. It defines interfaces (`ITaskRepository`, `IUserRepository`, `IUnitOfWork`, `IPasswordHasher`, `IJwtTokenGenerator`, `ICurrentUserService`, `IDateTimeProvider`).
- **Infrastructure** implements Application interfaces. EF Core lives **only** here.
- **Api** is the composition root. Controllers call Application services only; never `DbContext` directly.
- Never leak EF Core types, `DbContext`, or entities out of the API. Controllers return DTOs.

Each layer exposes a `DependencyInjection.cs` with an extension method (`AddApplication()`, `AddInfrastructure(IConfiguration)`) called from `Program.cs`.

## Domain Model

### User
| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Email` | `string` | Required, unique, max 256, stored lowercased |
| `PasswordHash` | `string` | Required, never exposed |
| `CreatedAt` | `DateTime` (UTC) | |
| `Tasks` | `ICollection<TaskItem>` | Navigation |

### TaskItem
Name the entity `TaskItem` to avoid clashing with `System.Threading.Tasks.Task`.

| Property | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `Title` | `string` | Required, max 200 |
| `Description` | `string?` | Optional, max 2000 |
| `Status` | `TaskStatus` enum | Default `Todo` |
| `DueDate` | `DateTime?` (UTC) | Optional |
| `UserId` | `Guid` | FK → User, cascade delete, indexed |
| `CreatedAt` | `DateTime` (UTC) | Set on create |
| `UpdatedAt` | `DateTime?` (UTC) | Set on update |

### TaskStatus enum
`Todo`, `InProgress`, `Done`. Persist as string in the database (`HasConversion<string>()`) and serialize as string in JSON.

Entities should protect their invariants: use private setters, a constructor/factory method, and methods like `Update(...)` and `ChangeStatus(...)` rather than public setters everywhere.

## API Contract

JSON uses **snake_case** (`JsonNamingPolicy.SnakeCaseLower`) so fields match the spec (`due_date`, `created_at`). Enums serialize as strings via `JsonStringEnumConverter`.

### Auth (anonymous)
| Method | Route | Body | Success |
|---|---|---|---|
| POST | `/api/auth/register` | `{ email, password }` | `201` with `{ id, email }` |
| POST | `/api/auth/login` | `{ email, password }` | `200` with `{ access_token, token_type: "Bearer", expires_at }` |

### Auth (requires JWT)
| Method | Route | Success |
|---|---|---|
| GET | `/api/auth/me` | `200` with current user `{ id, email, created_at }` |

### Tasks (all require JWT, scoped to current user)
| Method | Route | Body | Success |
|---|---|---|---|
| GET | `/api/tasks` | — | `200` paged list |
| GET | `/api/tasks/{id}` | — | `200` task |
| POST | `/api/tasks` | `{ title, description?, status?, due_date? }` | `201` + `Location` header |
| PUT | `/api/tasks/{id}` | `{ title, description?, status, due_date? }` | `200` updated task |
| DELETE | `/api/tasks/{id}` | — | `204` |

`GET /api/tasks` query params: `status`, `due_before`, `due_after`, `page` (default 1), `page_size` (default 20, max 100), `sort` (`due_date`, `created_at`; prefix `-` for descending).

Query semantics (implemented and tested; keep them stable):
- `due_after` is inclusive, `due_before` is exclusive (`due_after <= due_date < due_before`). Tasks without a due date are excluded when either is given.
- Default sort is `-created_at`. When sorting by `due_date`, tasks without one come **last in both directions**. Ties are broken by `Id` so pages are stable.
- Invalid `sort`, unknown `status`, `due_after >= due_before`, or out-of-range paging return `400` (not clamped).
- `status` is optional on POST (defaults to `Todo`) but required on PUT; the DTO property is nullable only so a missing value is reported instead of defaulting.
- Client dates are normalized to UTC in Application (`DateTimeExtensions.AsUtc`): offsets are converted, zone-less values are taken as UTC. "Not in the past" compares UTC calendar days.
- Query parameters are documented with `[Description]`, not XML `<param>` comments (those are matched by C# name and lost on renamed snake_case keys).

Paged response shape:
```json
{ "items": [], "page": 1, "page_size": 20, "total_count": 0 }
```

Task response shape:
```json
{
  "id": "guid",
  "title": "string",
  "description": "string | null",
  "status": "Todo | InProgress | Done",
  "due_date": "2026-10-01T00:00:00Z",
  "created_at": "...",
  "updated_at": "..."
}
```

### Ownership Rule (security-critical)
- `UserId` always comes from the JWT (`sub` claim) via `ICurrentUserService`, **never** from the request body or route.
- Every task query filters by the current user's id.
- Accessing another user's task returns **404**, not 403, to avoid leaking existence.

## Validation Rules
- `title`: required, not whitespace, 1–200 chars.
- `description`: max 2000 chars.
- `status`: must be a valid enum value.
- `due_date`: optional; on create, must not be in the past (allow same day).
- `email`: required, valid format, max 256.
- `password`: min 8 chars, at least one letter and one digit.

Validation lives in Application (FluentValidation). Validation failures return `400` as `ValidationProblemDetails`.

## Error Handling
- Use RFC 7807 `ProblemDetails` for all errors (`builder.Services.AddProblemDetails()` plus a global exception handler implementing `IExceptionHandler`).
- Application services return a `Result`/`Result<T>` type for expected failures (not found, conflict, validation) instead of throwing. Exceptions are for unexpected failures only.
- Status mapping: validation → 400, bad credentials → 401, not found → 404, duplicate email → 409, unhandled → 500 (no stack traces outside Development).
- Login failures return a generic message; do not reveal whether the email exists.
- `Result` → HTTP mapping lives in one place: `ApiControllerBase.Problem(Error)`. Validation keys are converted to snake_case there.
- `UseStatusCodePages()` gives bodyless responses (JWT challenge 401, unknown route 404) a ProblemDetails body.
- A duplicate email that slips past the existence check (concurrent registration) is caught by the unique index; `AppDbContext` translates it to `UniqueConstraintException`, which `AuthService` maps to 409.
- MVC and the HTTP (ProblemDetails/OpenAPI) JSON options are configured by the same `ConfigureJson` in `Program.cs`; change both together. Only JSON media types are advertised (`StringOutputFormatter` and `text/json` removed; `application/*+json` must stay for `application/problem+json`).

## Authentication (JWT)
- Symmetric key HMAC-SHA256.
- Config section `Jwt`: `Issuer`, `Audience`, `Key`, `ExpiryMinutes` (default 60).
- Claims: `sub` (user id), `email`, `jti`.
- Validate issuer, audience, lifetime, and signing key. `ClockSkew` = 1 minute.
- **Never commit the signing key.** In development use `dotnet user-secrets`; elsewhere use environment variables (`Jwt__Key`). Key must be at least 32 bytes; fail fast at startup if missing or too short.
- Refresh tokens are out of scope for now.

## Persistence
- `AppDbContext` in Infrastructure with `IEntityTypeConfiguration<T>` classes per entity (no data annotations on domain entities).
- Connection string in `appsettings.json`: `"DefaultConnection": "Data Source=taskmanager.db"`.
- Add `*.db`, `*.db-shm`, `*.db-wal` to `.gitignore`.
- Use migrations (never `EnsureCreated` outside tests). Apply migrations automatically on startup **only** in Development.
- Unique index on `User.Email`; index on `TaskItem.UserId` and `(UserId, Status)`.
- SQLite does not support `DateTimeOffset` ordering well: store `DateTime` in UTC and add a value converter that sets `DateTimeKind.Utc` on read.
- Use `AsNoTracking()` for read queries.

## Commands

```bash
# Build & run
dotnet build
dotnet run --project src/TaskManager.Api

# Tests
dotnet test

# EF Core migrations (install once: dotnet tool install --global dotnet-ef)
dotnet ef migrations add <Name> --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api
dotnet ef database update --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api

# Secrets (development) — UserSecretsId is already in TaskManager.Api.csproj
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)" --project src/TaskManager.Api
# Windows PowerShell 5.1 has no static RandomNumberGenerator.GetBytes; use:
#   $b = New-Object byte[] 64; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)

# Formatting (must pass; files use CRLF per .editorconfig)
dotnet format --verify-no-changes
dotnet format
```

Migrations go in `Persistence/Migrations` (pass `--output-dir Persistence/Migrations` to `migrations add`). That folder is marked `generated_code` in `.editorconfig`, so style rules don't apply to it.

## Coding Conventions
- Nullable reference types enabled; treat warnings as errors in all projects (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`), configured in a root `Directory.Build.props`.
- File-scoped namespaces, `sealed` classes by default, `record` types for DTOs and requests.
- Async all the way; every async method accepts and forwards a `CancellationToken`.
- Use `IDateTimeProvider` instead of `DateTime.UtcNow` directly (testability).
- Controllers are thin: map request → call service → map `Result` to HTTP response.
- Feature folders in Application: `Tasks/`, `Auth/` each holding DTOs, validators, service interface, and implementation.
- No commented-out code, no `TODO` left without a matching note in this file.
- Naming: private instance fields `_camelCase`; constants and `static readonly` fields `PascalCase` (enforced by `.editorconfig`).
- `TaskStatus` clashes with `System.Threading.Tasks.TaskStatus` from implicit usings. Every project that uses it has `global using TaskStatus = TaskManager.Domain.Tasks.TaskStatus;` in `GlobalUsings.cs`; add it to any new project that needs the enum.
- Domain methods take `DateTime utcNow` from the caller (Application passes `IDateTimeProvider.UtcNow`) and reject non-UTC dates.

## Testing Strategy
- **Domain tests**: entity invariants and state changes.
- **Application tests**: services with fake/in-memory repositories; validators.
- **Integration tests**: `WebApplicationFactory<Program>` with in-memory SQLite. Cover the full auth flow, every task endpoint, validation errors, and **cross-user isolation** (user B cannot read, update, or delete user A's task → 404).
- Add `public partial class Program;` at the end of `Program.cs` so tests can reference it.
- All tests must pass before a task is considered complete.

How the test setup actually works:
- `TaskManagerApiFactory` points `ConnectionStrings:DefaultConnection` at a uniquely named shared-cache in-memory database (`DataSource=file:<guid>?mode=memory&cache=shared`) and keeps one connection open for its lifetime. This is equivalent to `:memory:` + shared connection without replacing EF service registrations. It runs in `Development`, so the real migrations are applied at startup. It sets a test `Jwt:Key` via `UseSetting`, which takes precedence over user-secrets.
- There is no Infrastructure test project: repository tests (`Persistence/`, fresh in-memory SQLite per test with real migrations), JWT generator, hasher and token-validation tests live in `TaskManager.Api.IntegrationTests`. Infrastructure and Application expose internals to their test projects via `InternalsVisibleTo`.
- `Model_HasNoChangesMissingFromMigrations` fails if the model changes without a new migration.
- Application tests use hand-written fakes in `Fakes/Fakes.cs` (no mocking library). The fake task repository only records `TaskListCriteria`; filtering/sorting is verified against SQLite.
- Use unique emails per test (`ApiClientExtensions.UniqueEmail()`); the factory's database is shared across a test class.

## Implementation Plan

Work through these milestones in order. Finish each (building and tests green) before starting the next, and commit at the end of each.

1. **Scaffold**: solution, four `src` projects, three test projects, project references following the dependency rules, `Directory.Build.props`, `.gitignore`, `.editorconfig`.
2. **Domain**: `User`, `TaskItem`, `TaskStatus`, plus domain unit tests.
3. **Infrastructure – persistence**: `AppDbContext`, entity configurations, repositories, `IUnitOfWork`, initial migration.
4. **Auth**: password hasher, JWT generator, `AuthService` (register/login), `AuthController`, JWT middleware configuration, `ICurrentUserService`.
5. **Tasks CRUD**: `TaskService`, validators, `TasksController`, ownership enforcement.
6. **Cross-cutting**: ProblemDetails, global exception handler, snake_case JSON, OpenAPI + Scalar with JWT bearer security scheme.
7. **Querying**: filtering, sorting, pagination on `GET /api/tasks`.
8. **Tests & polish**: integration tests, README with setup steps and example requests (`.http` file in the Api project).

Status: all eight milestones are complete. Deviations from the plan above: snake_case JSON was configured in milestone 4 (the auth contract depends on it), and pagination (`page`, `page_size`, paged envelope) shipped in milestone 5 so the list contract never changed; milestone 7 added filtering and sorting.

## Definition of Done
- `dotnet build` produces zero warnings.
- `dotnet test` passes.
- Endpoints work end to end via the `.http` file or Scalar UI.
- No secrets in source control.
- This file is updated if architecture, commands, or conventions change.

## Do Not
- Put business logic in controllers or EF configuration.
- Reference Infrastructure from Application or Domain.
- Accept `UserId` from clients for task operations.
- Return entities or password hashes from any endpoint.
- Log passwords, tokens, or full request bodies of auth endpoints.
- Add new NuGet packages without stating why in the commit message.
