# Journal App

A personal journal web app. Registered users write entries (title, content, optional mood), see them newest first, edit and delete them. Nobody else can see or change a user's entries.

- **Backend:** .NET 10 ASP.NET Core Web API, Clean Architecture, TDD, EF Core + SQL Server, JWT
- **Frontend:** Angular 21 (standalone components, signals, zoneless), also organized in clean-architecture layers
- **One process:** the Angular build is written to the API's `wwwroot`, so `dotnet run` serves both the API and the UI

## Quick start

Prerequisites: .NET 10 SDK, Node.js 22.12+ (or 24+) with npm, and either Docker **or** SQL Server LocalDB (Windows).

```bash
# 1. Database (Docker). Skip this if you use LocalDB, see below.
cp .env.example .env
docker compose up -d

# 2. Build the UI into src/JournalApp.Api/wwwroot
cd src/JournalApp.Web
npm install
npm run build
cd ../..

# 3. Run API + UI
dotnet run --project src/JournalApp.Api
```

Open **http://localhost:5192**. In Development the app applies migrations and loads demo data automatically, so you don't need to run `dotnet ef`.

| User | Email | Password | Entries |
|---|---|---|---|
| demo | `demo@journal.com` | `Demo123!` | 5, with different moods and dates |
| alex | `alex@journal.com` | `Alex123!` | 2 (shows that users can't see each other's entries) |

Swagger UI: http://localhost:5192/swagger. Log in with `POST /api/auth/login`, click **Authorize**, and paste the token.

### Using LocalDB instead of Docker (Windows)

Change `ConnectionStrings:DefaultConnection` in `src/JournalApp.Api/appsettings.Development.json` to:

```
Server=(localdb)\\MSSQLLocalDB;Database=JournalApp;Trusted_Connection=True;TrustServerCertificate=True
```

Or keep the file unchanged and override the setting with an environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=JournalApp;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run --project src/JournalApp.Api
```

### Frontend development with hot reload

```bash
dotnet run --project src/JournalApp.Api   # API on http://localhost:5192
cd src/JournalApp.Web && npm start        # UI on http://localhost:4200, proxies /api to the API
```

The Angular app always calls the relative `/api`, so it works the same behind the dev proxy and when the API serves it. No CORS configuration is needed.

## Tests

```bash
dotnet test                              # all backend suites
cd src/JournalApp.Web && npm test        # Angular unit tests (Vitest, the CLI default)
```

| Suite | What it covers |
|---|---|
| `Domain.Tests` | Entity invariants: title/content limits, trimming, mood validity, email normalization, username length |
| `Application.Tests` | Auth and entry services with mocked repositories, validators, the ownership rule (another user's entry → not found) |
| `Infrastructure.Tests` | Repositories against real SQL Server: unique email/username indexes, cascade delete, mood stored as text, seeder, password hasher, JWT claims |
| `Api.Tests` | `WebApplicationFactory` integration tests: status codes, 401 without a token, 404 on another user's entries, register → login → CRUD flow, unknown `/api/*` routes return 404 (not the SPA) |

`Infrastructure.Tests` and `Api.Tests` use **Testcontainers**, so **Docker must be running**. Each suite starts its own ephemeral SQL Server. To run them without Docker, point them at an existing server; each test class creates and drops its own database:

```powershell
$env:JOURNALAPP_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True'
dotnet test
```

EF Core InMemory is not used on purpose, because it doesn't enforce unique indexes or constraints.

## Architecture

```
Api ──► Application ──► Domain
 │            ▲
 └──► Infrastructure
```

| Project | Responsibility |
|---|---|
| `JournalApp.Domain` | `User`, `JournalEntry`, `Mood`, domain exceptions. Private setters and factory/update methods that enforce invariants. No dependencies. |
| `JournalApp.Application` | Use cases (`AuthService`, `JournalEntryService`), DTOs, FluentValidation validators, manual mapping, the interfaces Infrastructure implements. |
| `JournalApp.Infrastructure` | `AppDbContext` + configurations + migrations, repositories, `UnitOfWork` (turns unique-index violations into 409), `PasswordHasher<User>` adapter, `JwtTokenService`, `DbSeeder`. |
| `JournalApp.Api` | Composition root: thin controllers, exception → `ProblemDetails` middleware, JWT bearer auth, Swagger, static files + SPA fallback. |
| `JournalApp.Web` | Angular app, see below. |

Main decisions:

- **Ownership:** a request for someone else's entry returns **404**, never 403, so the API doesn't reveal that the entry exists. The check lives in `JournalEntryService`.
- **Server-side dates:** `CreatedAt`/`UpdatedAt` come from an injected `TimeProvider` and are stored as UTC `datetime2`. They are read back as `DateTimeKind.Utc`, so the JSON always carries `Z`.
- **Emails** are stored trimmed and lowercased, which makes the unique index case-insensitive regardless of collation.
- **`Content`** is `nvarchar(max)`, because SQL Server's `nvarchar(n)` tops out at 4000 characters. The 5000-character limit is enforced by the domain and the validators.
- **Error responses** are `ProblemDetails`. Validation errors come as `errors` keyed by camelCase field name, which the UI shows next to each field.

### Frontend layers (`src/JournalApp.Web/src/app`)

```
core/      AuthStore (signals, localStorage, auto-logout on expiry), guards, auth + error interceptors, API base URL
domain/    Models (JournalEntry, Mood, User, AppError) and abstract repositories (DI tokens)
data/      DTOs, mappers (ISO strings → Date, HTTP errors → AppError), HTTP repositories
features/  auth (login, register) and journal (list, detail, form, JournalStore, presentational components)
shared/    Spinner, alert, confirm dialog (native <dialog>), validators, form error helpers, excerpt pipe
```

Components never use `HttpClient` directly. `app.config.ts` binds each domain repository to its HTTP implementation. Presentational components use `input()`/`output()` and `OnPush`. A 401 on an authenticated request logs the user out and redirects to `/login`.

## Configuration and secrets

| Setting | Where |
|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` (points to the Docker container) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes` | `appsettings.json` |
| `Jwt:Key` | Demo key in `appsettings.Development.json` only. The app refuses to start if the key is missing or shorter than 32 characters. |
| `SA_PASSWORD` (Docker) | `.env` (gitignored), created from `.env.example`. Must match the connection string. |

The Development values are demo credentials. **Outside Development, provide `Jwt:Key` and the connection string through user secrets or environment variables** (for example `Jwt__Key`, `ConnectionStrings__DefaultConnection`). Never commit them.

## Useful commands

```bash
docker compose up -d                        # SQL Server on localhost:1433
dotnet ef migrations add <Name> -p src/JournalApp.Infrastructure -s src/JournalApp.Api -o Persistence/Migrations
cd src/JournalApp.Web && npm run build      # writes to ../JournalApp.Api/wwwroot
```
