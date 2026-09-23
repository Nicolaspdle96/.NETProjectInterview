# Journal App — Project Guide for Claude Code

This file describes the project, its architecture, and the working rules. Read it in full before generating or modifying code, and follow the conventions defined here.

## 1. Context

Technical exercise: a personal journal web app with a **.NET (C#)** backend following **Clean Architecture** and **TDD**, and an **Angular** frontend, also organized with clean architecture. The Angular build is published inside the API project's `wwwroot`, so **a single .NET process serves both the API and the UI**.

### User story

> As a registered user, I want to write entries in my journal (title, content, and mood), view them sorted by date, edit them, and delete them, so I can keep a personal record of my days. Only I can see and modify my own entries.

### Acceptance criteria

- A visitor can register with a username, email, and password.
- A registered user can log in and receive a JWT token.
- An authenticated user can create, list, view, edit, and delete **their own** entries.
- A user cannot view, edit, or delete another user's entries.
- The list is displayed sorted by creation date, descending.
- The application starts up with preloaded demo data and credentials.

## 2. Tech stack

**Backend**
- .NET 10 (LTS) — ASP.NET Core Web API with controllers
- Entity Framework Core + SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
- JWT Bearer authentication
- FluentValidation for validation in the Application layer
- Tests: xUnit, Moq, FluentAssertions, `Microsoft.AspNetCore.Mvc.Testing` for API tests; Infrastructure and integration tests run against a real SQL Server (LocalDB)

**Frontend**
- Angular (latest stable version), standalone components, signals
- Angular Router with lazy loading per feature
- `HttpClient` with functional interceptors
- Reactive Forms
- Styling: SCSS, responsive design (mobile first)
- Tests: the Angular CLI's default runner

## 3. Solution structure

```
JournalApp/
├── CLAUDE.md
├── README.md
├── JournalApp.sln
├── src/
│   ├── JournalApp.Domain/            # Entities, enums, domain exceptions. No dependencies.
│   ├── JournalApp.Application/       # Use cases, DTOs, interfaces, validators. Depends only on Domain.
│   ├── JournalApp.Infrastructure/    # EF Core, repositories, JWT, hashing, seed. Implements Application interfaces.
│   ├── JournalApp.Api/               # Controllers, middleware, DI, Program.cs. Serves wwwroot.
│   │   └── wwwroot/                  # OUTPUT of the Angular build (do not edit by hand, gitignored)
│   └── JournalApp.Web/               # Angular source code
└── tests/
    ├── JournalApp.Domain.Tests/
    ├── JournalApp.Application.Tests/
    ├── JournalApp.Infrastructure.Tests/
    └── JournalApp.Api.Tests/
```

### Dependency rule (mandatory)

```
Api ──► Application ──► Domain
 │            ▲
 └──► Infrastructure
```

- `Domain` does not reference any other project or infrastructure package.
- `Application` references only `Domain`. It defines interfaces (`IJournalEntryRepository`, `IUserRepository`, `IPasswordHasher`, `ITokenService`, `ICurrentUserService`, `IUnitOfWork`) but **does not** implement them.
- `Infrastructure` implements those interfaces. It's the only project that knows about EF Core.
- `Api` is the composition root: it registers dependencies and exposes endpoints. Controllers contain no business logic; they only delegate to Application services.

## 4. Backend

### 4.1 Domain

```csharp
public class JournalEntry
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public Mood? Mood { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Factory method Create(...) and Update(...) method that enforce invariants
}

public enum Mood { Great, Good, Neutral, Bad, Awful }

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

- Private setters; state changes go through domain methods.
- Private parameterless constructor for EF Core.
- Domain exceptions: `DomainValidationException`, `NotFoundException`, `ConflictException`, `UnauthorizedException`.

### 4.2 Business rules

**JournalEntry**
- `Title` required, max 100 characters, stored with `Trim()`.
- `Content` required, max 5000 characters.
- `Mood` optional; if provided, must be a valid enum value.
- `CreatedAt` and `UpdatedAt` are assigned by the server in UTC; never accepted from the client.
- Only the owner (`UserId == currentUserId`) can read, edit, or delete an entry. If the requester isn't the owner, respond with **404** (don't reveal the resource's existence).

**User**
- `Username` required, 3–30 characters, unique.
- `Email` required, valid format, unique (case-insensitive comparison).
- `Password` minimum 8 characters, with at least one uppercase letter, one lowercase letter, and one digit.
- The password is never stored or returned in plain text.

### 4.3 Application

Organized by feature:

```
Application/
├── Common/
│   ├── Interfaces/        # IUserRepository, IJournalEntryRepository, IPasswordHasher, ITokenService, ICurrentUserService, IUnitOfWork
│   └── Exceptions/
├── Auth/
│   ├── Dtos/              # RegisterRequest, LoginRequest, AuthResponse, UserDto
│   ├── Validators/
│   └── AuthService.cs     # IAuthService
└── Entries/
    ├── Dtos/              # CreateEntryRequest, UpdateEntryRequest, EntryDto
    ├── Validators/
    └── JournalEntryService.cs  # IJournalEntryService
```

- Services receive and return DTOs, never entities.
- Entity ↔ DTO mapping is manual (extension methods), no AutoMapper.
- All methods are `async` and accept a `CancellationToken`.

### 4.4 Infrastructure

- `AppDbContext` with configurations in `IEntityTypeConfiguration<T>` classes, registered with `UseSqlServer(...)` using the `ConnectionStrings:DefaultConnection` connection string.
- Explicit column types: `nvarchar(100)` for `Title`, `nvarchar(max)` or `nvarchar(5000)` for `Content`, `datetime2` for dates, `Mood` stored as `nvarchar(20)` with `HasConversion<string>()` so it's readable in the database.
- Enable `EnableRetryOnFailure()` in the SQL Server options.
- Unique indexes on `Users.Email` and `Users.Username`; index on `JournalEntries.UserId`.
- One-to-many relationship User → JournalEntries with cascade delete.
- Password hashing with ASP.NET Core Identity's `PasswordHasher<User>` (only the hasher, not full Identity).
- `JwtTokenService` generates tokens with `sub` (user id), `email`, and `unique_name` claims.
- `DbSeeder` runs on startup in Development: applies migrations and creates demo data if the database is empty.

### 4.5 API

**Auth (`AuthController`)**

| Verb | Route | Auth | Responses |
|---|---|---|---|
| POST | `/api/auth/register` | Anonymous | 201 `UserDto`, 400 validation, 409 email/username already in use |
| POST | `/api/auth/login` | Anonymous | 200 `AuthResponse` (token + user), 401 invalid credentials |
| GET | `/api/auth/me` | JWT | 200 `UserDto`, 401 |
| GET | `/api/health` | Anonymous | 200 — example public endpoint |

**Entries (`EntriesController`)** — all require `[Authorize]`

| Verb | Route | Responses |
|---|---|---|
| GET | `/api/entries` | 200 list of `EntryDto` for the current user (desc. by date) |
| GET | `/api/entries/{id:guid}` | 200 `EntryDto`, 404 |
| POST | `/api/entries` | 201 `EntryDto` + `Location` header, 400 |
| PUT | `/api/entries/{id:guid}` | 200 `EntryDto`, 400, 404 |
| DELETE | `/api/entries/{id:guid}` | 204, 404 |

- Errors returned as `ProblemDetails` via a global middleware that maps exceptions → status codes.
- Swagger/OpenAPI enabled in Development with Bearer token support.
- JWT configuration in `appsettings.json` (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiresMinutes`). A demo key can be used in Development; document that in production it belongs in user secrets or environment variables.

### 4.6 SQL Server database

Development uses **SQL Server LocalDB** (Windows; ships with Visual Studio or the SQL Server Express installer). Docker is not used.

`Server=(localdb)\\MSSQLLocalDB;Database=JournalApp;Trusted_Connection=True;TrustServerCertificate=True`

- This is the connection string in `appsettings.Development.json`. It uses Windows authentication, so there is no database password to manage. The README explains how to override it with `ConnectionStrings__DefaultConnection` to use another instance.
- Migrations are applied automatically on startup in Development (`Database.MigrateAsync()`), so the reviewer doesn't need to run `dotnet ef`.
- Infrastructure and API tests run against LocalDB: each test class (and the API test host) creates a throwaway database with a unique name, migrates it and drops it afterwards. The `JOURNALAPP_TEST_SQLSERVER` environment variable can point the tests at another server. Do not use EF Core's InMemory provider, since it doesn't enforce real unique indexes or constraints.

### 4.7 Serving Angular from wwwroot

In `Program.cs`, after mapping the controllers:

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html"); // enables Angular routing (deep links)
```

- The fallback must not catch `/api/*` routes: a non-existent API route returns 404, not `index.html`.
- `wwwroot/` is added to `.gitignore` except for a `.gitkeep`.

## 5. Frontend (Angular)

### 5.1 Structure

```
JournalApp.Web/src/app/
├── core/                      # Cross-cutting singletons
│   ├── auth/                  # AuthStore (signals), authGuard, guestGuard
│   ├── http/                  # authInterceptor (adds Bearer), errorInterceptor (401 → logout)
│   └── config/                # API base URL
├── domain/                    # Models and contracts, no Angular HTTP dependencies
│   ├── models/                # JournalEntry, Mood, User
│   └── repositories/          # abstract class JournalEntryRepository, AuthRepository
├── data/                      # HTTP implementations of the domain repositories
│   ├── dtos/
│   ├── mappers/
│   └── repositories/          # JournalEntryHttpRepository, AuthHttpRepository
├── features/
│   ├── auth/
│   │   ├── pages/             # login-page, register-page
│   │   └── auth.routes.ts
│   └── journal/
│       ├── pages/             # entry-list-page, entry-detail-page, entry-form-page
│       ├── components/        # entry-card, mood-picker, empty-state (presentational)
│       ├── state/             # JournalStore (signals)
│       └── journal.routes.ts
├── shared/                    # Reusable UI: buttons, spinner, confirmation dialog, pipes
├── app.routes.ts
└── app.config.ts              # provideRouter, provideHttpClient(withInterceptors(...)), repository → implementation bindings
```

### 5.2 Frontend architecture rules

- Components **never** use `HttpClient` directly. They consume stores or domain repositories.
- Domain repositories are `abstract class`es used as DI tokens; `app.config.ts` binds them to their HTTP implementation (`{ provide: JournalEntryRepository, useClass: JournalEntryHttpRepository }`).
- API DTOs are mapped to domain models in `data/mappers` (e.g., date strings → `Date`).
- Page components (smart) manage state; components in `components/` are presentational, with `input()` / `output()` and `ChangeDetectionStrategy.OnPush`.
- State with signals (`signal`, `computed`); no BehaviorSubject unless RxJS interop is needed.
- The JWT token is stored in `localStorage` and restored on app startup; expiration or a 401 → logout and redirect to `/login`.
- The whole app must run **with no warnings or errors in the browser console**.

### 5.3 Screens

- `/login` and `/register` (unauthenticated users only).
- `/entries` — card-based list with mood, title, excerpt, and date; empty state if there are no entries.
- `/entries/new` and `/entries/:id/edit` — reactive form with validations matching the backend and per-field error messages.
- `/entries/:id` — detail view with edit and delete actions (with confirmation).
- Loading and error states visible for every operation.
- Responsive layout: single column on mobile, grid on desktop.

### 5.4 Building into wwwroot

In `angular.json`, the build's `outputPath` points to the API's `wwwroot`, with no `browser` subfolder:

```json
"outputPath": {
  "base": "../JournalApp.Api/wwwroot",
  "browser": ""
}
```

- For development with hot reload, use `ng serve` with a `proxy.conf.json` that forwards `/api` to the API (`https://localhost:<port>`), so no CORS configuration is needed.
- The API's base URL in Angular is relative (`/api`), so it works the same in development (proxy) and when served from wwwroot.

## 6. Demo data (seed)

| Field | Value |
|---|---|
| Email | `demo@journal.com` |
| Username | `demo` |
| Password | `Demo123!` |

The demo user has 5 sample entries with different moods and dates. A second user (`alex@journal.com` / `Alex123!`) with 2 entries demonstrates that a user cannot see another user's entries.

## 7. Workflow (TDD)

When implementing any backend feature:

1. Write the failing test first (red) and show it.
2. Implement the minimal code to make it pass (green).
3. Refactor while keeping the tests green.
4. Run `dotnet test` before considering the task done.

Expected coverage:
- **Domain.Tests**: entity invariants (valid and invalid creation and updates).
- **Application.Tests**: services with mocked repositories; validators; entry ownership rule.
- **Infrastructure.Tests**: repositories against a real SQL Server on LocalDB (including verifying the unique indexes on email and username); hasher and token generation.
- **Api.Tests**: integration tests with `WebApplicationFactory` — status codes, 401 without a token, 404 when accessing another user's entries, full register → login → CRUD flow.

Test naming: `Method_Scenario_ExpectedResult` (e.g. `CreateAsync_EmptyTitle_ThrowsValidationException`). Arrange / Act / Assert structure.

### Suggested implementation order

1. Create the solution, projects, and references following the dependency rule.
2. Domain + Domain.Tests.
3. Application (Auth and Entries) + Application.Tests.
4. Infrastructure (DbContext, migrations, repositories, JWT, seed) + tests.
5. Api (controllers, error middleware, auth, Swagger, wwwroot) + integration tests.
6. Angular: core → domain → data → auth feature → journal feature.
7. Configure the build into wwwroot and verify that `dotnet run` serves the full UI.
8. Final README for the reviewer.

## 8. Commands

```bash
# Database (LocalDB starts automatically on first connection)
sqllocaldb info MSSQLLocalDB  # check the LocalDB instance

# Backend
dotnet restore
dotnet build
dotnet test
dotnet ef migrations add <Name> -p src/JournalApp.Infrastructure -s src/JournalApp.Api
dotnet run --project src/JournalApp.Api

# Frontend
cd src/JournalApp.Web
npm install
npm start          # ng serve with proxy to the API
npm run build      # generates the build in ../JournalApp.Api/wwwroot
npm test
```

## 9. Code conventions

- C#: nullable reference types enabled, file-scoped namespaces, `async` with the `Async` suffix, constructor injection (primary constructors allowed).
- Warnings treated as errors in the `src/` projects.
- No business logic in controllers or in Angular components.
- No `DateTime.Now`: use an injected `TimeProvider` so dates can be tested.
- TypeScript in `strict` mode; no `any`.
- Code, test, and commit names in English.
- Small, descriptive commits per TDD step (e.g. `test: add validation tests for journal entry title`, `feat: implement journal entry creation`).

## 10. What NOT to do

- Don't add NuGet or npm packages outside the defined stack without checking first.
- Don't use full ASP.NET Core Identity or Entity Framework in Domain or Application.
- Don't skip tests to "move faster."
- Don't edit files inside `wwwroot/` by hand.
- Don't return domain entities from controllers.
- Don't hardcode secrets outside `appsettings.Development.json`.
