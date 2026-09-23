# TaskManager API

A RESTful API for managing personal tasks. Users register, log in with a JWT, and create, read, update and delete **their own** tasks. A user can never see or change another user's tasks.

Built with .NET 10, ASP.NET Core controllers, EF Core on SQLite, and Clean Architecture.

## Quick start

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# 1. Set the JWT signing key (at least 32 bytes). It is never committed.
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)" --project src/TaskManager.Api
```

On Windows PowerShell (no `openssl`):

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:Key" ([Convert]::ToBase64String($bytes)) --project src/TaskManager.Api
```

```bash
# 2. Run. In Development the SQLite database (taskmanager.db) is created and migrated automatically.
dotnet run --project src/TaskManager.Api
```

Then open:

| URL | What |
|---|---|
| http://localhost:5185/scalar | Interactive API reference (Scalar). Log in, then paste `access_token` into the Bearer auth field. |
| http://localhost:5185/openapi/v1.json | OpenAPI 3.1 document |

[`src/TaskManager.Api/TaskManager.Api.http`](src/TaskManager.Api/TaskManager.Api.http) has ready-made requests for VS Code (REST Client) or Visual Studio.

The app refuses to start if `Jwt:Key` is missing or shorter than 32 bytes.

## Configuration

| Setting | Default | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `Data Source=taskmanager.db` | SQLite file, relative to the working directory |
| `Jwt:Issuer` | `TaskManager.Api` | |
| `Jwt:Audience` | `TaskManager.Client` | |
| `Jwt:ExpiryMinutes` | `60` | |
| `Jwt:Key` | *(none)* | Required. `dotnet user-secrets` in development, environment variable elsewhere |

Every setting can be overridden with an environment variable, using `__` as the separator (e.g. `Jwt__Key`, `ConnectionStrings__DefaultConnection`).

Outside Development, migrations are **not** applied on startup and the API reference is not served. Apply migrations as part of deployment:

```bash
dotnet ef database update --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api
```

## API

All JSON uses **snake_case** property names, and enums are strings. Every error is an [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) `application/problem+json` response.

### Auth

| Method | Route | Auth | Success | Errors |
|---|---|---|---|---|
| POST | `/api/auth/register` | — | `201` `{ id, email }` | `400` validation, `409` email already registered |
| POST | `/api/auth/login` | — | `200` `{ access_token, token_type: "Bearer", expires_at }` | `400`, `401` invalid credentials |
| GET | `/api/auth/me` | JWT | `200` `{ id, email, created_at }` | `401` |

### Tasks (all require `Authorization: Bearer <token>`)

| Method | Route | Body | Success | Errors |
|---|---|---|---|---|
| GET | `/api/tasks` | — | `200` paged list | `400` invalid query |
| GET | `/api/tasks/{id}` | — | `200` task | `404` |
| POST | `/api/tasks` | `{ title, description?, status?, due_date? }` | `201` task + `Location` | `400` |
| PUT | `/api/tasks/{id}` | `{ title, description?, status, due_date? }` | `200` task | `400`, `404` |
| DELETE | `/api/tasks/{id}` | — | `204` | `404` |

Task:

```json
{
  "id": "0199...",
  "title": "Write quarterly report",
  "description": "Include Q3 numbers",
  "status": "Todo",
  "due_date": "2026-10-01T00:00:00Z",
  "created_at": "2026-09-23T12:00:00Z",
  "updated_at": null
}
```

`GET /api/tasks` query parameters:

| Parameter | Description |
|---|---|
| `status` | `Todo`, `InProgress` or `Done` (case-insensitive) |
| `due_after` | Tasks due at or after this instant (inclusive) |
| `due_before` | Tasks due before this instant (exclusive) |
| `sort` | `created_at`, `-created_at` (default), `due_date`, `-due_date`. A `-` prefix means descending. |
| `page` | 1-based, default `1` |
| `page_size` | 1–100, default `20` |

Paged response: `{ "items": [...], "page": 1, "page_size": 20, "total_count": 42 }`.

### Example session

```bash
BASE=http://localhost:5185

curl -s -X POST $BASE/api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Passw0rd!"}'

TOKEN=$(curl -s -X POST $BASE/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"Passw0rd!"}' | sed -E 's/.*"access_token":"([^"]+)".*/\1/')

curl -s -X POST $BASE/api/tasks -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"Write quarterly report","due_date":"2099-10-01T00:00:00Z"}'

curl -s "$BASE/api/tasks?status=Todo&sort=due_date" -H "Authorization: Bearer $TOKEN"
```

## Rules and behavior

**Validation** (returns `400` with per-field errors keyed in snake_case):

- `title`: required, not only whitespace, at most 200 characters (stored trimmed).
- `description`: at most 2000 characters. An empty or whitespace-only description is stored as `null`.
- `status`: `Todo`, `InProgress` or `Done`. Optional on create (defaults to `Todo`), required on update.
- `due_date`: optional. On create it must not be in the past; "today" (UTC calendar day) is allowed.
- `email`: required, valid format, at most 256 characters. Stored lowercased, so uniqueness is case-insensitive.
- `password`: at least 8 characters, with at least one letter and one digit.

**Ownership.** The user id always comes from the token's `sub` claim, never from the request. Every task query is filtered by it. Another user's task returns `404`, exactly like a task that does not exist, so ids cannot be probed.

**Login.** A wrong password and an unknown email return the same `401` message.

**Dates.** All dates are stored and returned in UTC. Input with an offset (`+02:00`) is converted to UTC; input without zone information (e.g. `2026-10-01`) is taken as UTC.

**Due-date filtering and sorting.** Tasks without a due date are excluded when `due_after` or `due_before` is used, and sort last in both directions when sorting by `due_date`. Results are ordered deterministically (ties broken by id), so pages never overlap.

**Errors.** Unexpected exceptions return a `500` problem response. Exception details are only included in Development.

## Tests

```bash
dotnet test
```

| Project | Covers |
|---|---|
| `TaskManager.Domain.Tests` | Entity invariants and state changes |
| `TaskManager.Application.Tests` | Services (with in-memory fakes) and validators |
| `TaskManager.Api.IntegrationTests` | The real API via `WebApplicationFactory` against in-memory SQLite: auth flow, token validation (expiry, clock skew, issuer, audience, key), every task endpoint, validation errors, **cross-user isolation**, error responses and the OpenAPI document. Also repository, JWT generator and password hasher tests against the real implementations. |

The integration tests apply the real EF Core migrations, and one test fails if the model has changes without a migration.

## Project structure

```
src/
  TaskManager.Domain/          Entities (User, TaskItem), TaskStatus, invariants. No dependencies.
  TaskManager.Application/     Use cases (AuthService, TaskService), DTOs, validators, Result type,
                               and the interfaces Infrastructure implements.
  TaskManager.Infrastructure/  EF Core + SQLite, repositories, migrations, BCrypt, JWT generation.
  TaskManager.Api/             Controllers, JWT bearer setup, error handling, OpenAPI, composition root.
tests/
  TaskManager.Domain.Tests/
  TaskManager.Application.Tests/
  TaskManager.Api.IntegrationTests/
```

Dependencies point inward: Api → Application → Domain, and Infrastructure → Application. Controllers only call Application services; EF Core types never leave Infrastructure.

Expected failures (validation, not found, conflict, bad credentials) travel as `Result` values and are mapped to HTTP status codes in one place, [`ApiControllerBase`](src/TaskManager.Api/Controllers/ApiControllerBase.cs). Exceptions are reserved for unexpected failures.

## Development commands

```bash
dotnet build                      # warnings are errors
dotnet test
dotnet format --verify-no-changes

# Migrations (install once: dotnet tool install --global dotnet-ef)
dotnet ef migrations add <Name> --project src/TaskManager.Infrastructure --startup-project src/TaskManager.Api --output-dir Persistence/Migrations
```
