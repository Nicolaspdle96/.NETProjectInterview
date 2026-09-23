# Journal App

A personal journal web app. Registered users write entries (title, content, optional mood), see them newest first, edit and delete them. Nobody else can see or change a user's entries.

- **Backend:** .NET 10 ASP.NET Core Web API, Clean Architecture, TDD, EF Core + SQL Server, JWT
- **Frontend:** Angular 21 (standalone components, signals, zoneless), also organized in clean-architecture layers
- **One process:** the Angular build is written to the API's `wwwroot`, so `dotnet run` serves both the API and the UI

## Quick start

Prerequisites: Windows with SQL Server LocalDB (installed with Visual Studio, or on its own via the SQL Server Express installer), the .NET 10 SDK, and Node.js 22.12+ (or 24+) with npm.

```bash
# 1. Build the UI into src/JournalApp.Api/wwwroot
cd src/JournalApp.Web
npm install
npm run build
cd ../..

# 2. Run API + UI
dotnet run --project src/JournalApp.Api
```

Open **http://localhost:5192**. The app connects to LocalDB (`(localdb)\MSSQLLocalDB`), which starts automatically on first connection. In Development it creates the `JournalApp` database, applies migrations and loads demo data, so you don't need to run `dotnet ef`.

| User | Email | Password | Entries |
|---|---|---|---|
| demo | `demo@journal.com` | `Demo123!` | 5, with different moods and dates |
| alex | `alex@journal.com` | `Alex123!` | 2 (shows that users can't see each other's entries) |

Swagger UI: http://localhost:5192/swagger. Log in with `POST /api/auth/login`, click **Authorize**, and paste the token.

### Using a different SQL Server instance

The connection string is `ConnectionStrings:DefaultConnection` in `src/JournalApp.Api/appsettings.Development.json`. To point at another instance without editing the file, set an environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection = 'Server=.\SQLEXPRESS;Database=JournalApp;Trusted_Connection=True;TrustServerCertificate=True'
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
| `Application.Tests` | Auth and entry services with mocked repositories, validators, the ownership rule (another user's entry → not found), caching decorators |
| `Infrastructure.Tests` | Repositories against real SQL Server: unique email/username indexes, cascade delete, mood stored as text, seeder, password hasher, JWT claims, in-memory cache |
| `Api.Tests` | `WebApplicationFactory` integration tests: status codes, 401 without a token, 404 on another user's entries, register → login → CRUD flow, no stale reads after writes, unknown `/api/*` routes return 404 (not the SPA) |

`Infrastructure.Tests` and `Api.Tests` run against real SQL Server on **LocalDB** by default. Each test class (and the API test host) creates its own throwaway database with a random name, migrates it, and drops it afterwards, so the tests never touch the `JournalApp` development database. To run them on another instance, set `JOURNALAPP_TEST_SQLSERVER` to a server connection string without a database name:

```powershell
$env:JOURNALAPP_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True'
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
| `JournalApp.Application` | Use cases (`AuthService`, `JournalEntryService`) and their caching decorators, DTOs, FluentValidation validators, manual mapping, the interfaces Infrastructure implements. |
| `JournalApp.Infrastructure` | `AppDbContext` + configurations + migrations, repositories, `UnitOfWork` (turns unique-index violations into 409), `PasswordHasher<User>` adapter, `JwtTokenService`, `MemoryCacheService`, `DbSeeder`. |
| `JournalApp.Api` | Composition root: thin controllers, exception → `ProblemDetails` middleware, JWT bearer auth, Swagger, static files + SPA fallback. |
| `JournalApp.Web` | Angular app, see below. |

Main decisions:

- **Ownership:** a request for someone else's entry returns **404**, never 403, so the API doesn't reveal that the entry exists. The check lives in `JournalEntryService`.
- **Server-side dates:** `CreatedAt`/`UpdatedAt` come from an injected `TimeProvider` and are stored as UTC `datetime2`. They are read back as `DateTimeKind.Utc`, so the JSON always carries `Z`.
- **Emails** are stored trimmed and lowercased, which makes the unique index case-insensitive regardless of collation.
- **`Content`** is `nvarchar(max)`, because SQL Server's `nvarchar(n)` tops out at 4000 characters. The 5000-character limit is enforced by the domain and the validators.
- **Error responses** are `ProblemDetails`. Validation errors come as `errors` keyed by camelCase field name, which the UI shows next to each field.
- **Cache layer:** reads are cached so repeated requests don't hit the database (see below).

### Cache layer

```
Controller ──► CachedJournalEntryService ──(miss)──► JournalEntryService ──► Repository ──► SQL Server
                       │
                       └──(hit)──► ICacheService (MemoryCacheService)
```

Controllers talk to caching decorators (`CachedJournalEntryService`, `CachedAuthService`) that wrap the real Application services:

| Request | Cached? | Key |
|---|---|---|
| `GET /api/entries` | Yes | per user |
| `GET /api/entries/{id}` | Yes | per user and entry |
| `GET /api/auth/me` | Yes | per user |
| `POST`/`PUT`/`DELETE` on `/api/entries` | No; invalidates that user's cached entries | — |
| `POST /api/auth/register`, `POST /api/auth/login` | No | — |

- **Safe with the ownership rule:** every key includes the user id, and the inner service still does the ownership check on a miss, so one user can never be served another user's cached entry. Errors such as 404 are never cached.
- **No stale reads:** each user's entry keys include a version token. Any create, update or delete by that user drops the token, so all their cached lists and entries are bypassed at once. The API tests check that reads right after each kind of write return fresh data.
- **Only DTOs are cached**, never EF entities, so change tracking isn't affected.
- **Bounded:** entries expire after `Cache:ExpirationSeconds` (default 300) and the cache holds at most `Cache:SizeLimit` items (default 10000). Hits and misses are logged at `Debug` level (category `JournalApp.Infrastructure.Caching`).
- **Single process:** the cache is in memory, which fits this single-process deployment. Running several instances would need a distributed `ICacheService` implementation (for example Redis); the decorators wouldn't change.

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
| `ConnectionStrings:DefaultConnection` | `appsettings.Development.json` (LocalDB with Windows authentication, so there is no password to store) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes` | `appsettings.json` |
| `Jwt:Key` | Demo key in `appsettings.Development.json` only. The app refuses to start if the key is missing or shorter than 32 characters. |
| `Cache:ExpirationSeconds`, `Cache:SizeLimit` | `appsettings.json` (defaults 300 and 10000; both must be greater than zero) |

The Development JWT key is a demo value. **Outside Development, provide `Jwt:Key` and the connection string through user secrets or environment variables** (for example `Jwt__Key`, `ConnectionStrings__DefaultConnection`). Never commit them.

## Useful commands

```bash
sqllocaldb info MSSQLLocalDB                # check the LocalDB instance
dotnet ef migrations add <Name> -p src/JournalApp.Infrastructure -s src/JournalApp.Api -o Persistence/Migrations
cd src/JournalApp.Web && npm run build      # writes to ../JournalApp.Api/wwwroot
```
