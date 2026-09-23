# Journal App — Project Presentation

## 1. The user story

> As a registered user, I want to write entries in my journal (title, content, and mood), view them sorted by date, edit them, and delete them, so I can keep a personal record of my days. Only I can see and modify my own entries.

This story drives every decision in the project. It gives us:

- **Two entities**: `User` and `JournalEntry` (one user, many entries).
- **Full CRUD** on journal entries.
- **Authentication and authorization**: users register and log in, and every entry operation is scoped to its owner.
- **A clear business rule** to test: a user can never see or modify someone else's entries.

### Technical goals

- **Backend**: .NET Web API following Clean Architecture, built with TDD.
- **Frontend**: Angular app, also organized with a clean architecture.
- **Hosting**: the Angular build is served from the API's `wwwroot`, so a single `dotnet run` serves both the API and the UI.
- **Database**: SQL Server with EF Core.

---

## 2. Development process overview

I built this project using **Claude Code** as my GenAI coding tool, following a spec-driven workflow: first define the project clearly in a spec file, then let the AI generate code against that spec, and finally review, test, and correct the output in small iterations.

| Step | What I did | Output |
|---|---|---|
| 1 | Wrote a first spec from the user story and a high-level tech stack | Initial `CLAUDE.md` |
| 2 | Refined the spec until it matched the actual requirements | Final `CLAUDE.md` |
| 3 | Generated the project from the spec | Backend, frontend, tests |
| 4 | Reviewed the result, found gaps, and created follow-up tasks | Tasks 4.1 – 4.3 |
| 5 | Verified the API and UI end to end and reviewed test coverage | Working app + coverage report |

---

## 3. Step 1 — Creating the spec from the user story

Instead of jumping straight into code, I started by writing a **`CLAUDE.md`** file. Claude Code reads this file automatically at the start of every session, so it acts as persistent context: the AI always knows the architecture, the rules, and the conventions without me repeating them in every prompt.

The first version was intentionally high level:

- The user story and acceptance criteria.
- The tech stack: .NET, Clean Architecture, TDD, Angular, served through `wwwroot`.
- The general project structure.

**Why start with a spec?** A vague prompt like "build me a journal app" produces generic code that makes its own architectural decisions. A spec turns the AI into an implementer of *my* design rather than the designer.

---

## 4. Step 2 — Refining the spec

I reviewed the first draft and iterated on it until it described exactly what I needed. The main refinements were:

- **Clean Architecture made explicit**: four projects (`Domain`, `Application`, `Infrastructure`, `Api`) with a mandatory dependency rule. `Domain` depends on nothing; `Application` depends only on `Domain`; `Infrastructure` implements `Application`'s interfaces.
- **Business rules written down**: field lengths, required fields, password policy, unique email/username, and the ownership rule.
- **Security decision**: when a user requests another user's entry, the API returns **404, not 403**, so it doesn't reveal that the resource exists.
- **Database changed from SQLite to SQL Server**, with explicit column types, unique indexes, and automatic migrations in Development.
- **Testing strategy**: I explicitly forbade EF Core's InMemory provider, because it doesn't enforce unique indexes or real constraints, so a test could pass while the real database would fail.
- **Frontend clean architecture**: `core`, `domain`, `data`, `features`, and `shared` layers. Components never touch `HttpClient` directly; domain repositories are abstract classes bound to HTTP implementations through DI.
- **Seeded demo data**: two users, so the ownership rule can be demonstrated live.
- **Guardrails for the AI**: a "What NOT to do" section (no extra packages without asking, no business logic in controllers, no skipping tests, no hardcoded secrets).

**Takeaway:** most of the value of the AI came from this step. The more precise the spec, the less correcting I had to do later.

---

## 5. Step 3 — Generating the project

With the spec ready, I asked Claude Code to build the project following the implementation order defined in the spec, one layer at a time, instead of generating everything in a single prompt:

1. Solution, projects, and references (respecting the dependency rule).
2. Domain + Domain tests.
3. Application (Auth and Entries) + Application tests.
4. Infrastructure (DbContext, migrations, repositories, JWT, seed) + tests.
5. API (controllers, error middleware, auth, Swagger, wwwroot) + integration tests.
6. Angular: core → domain → data → auth feature → journal feature.
7. Angular build into `wwwroot`.

Example prompt:

```
Read CLAUDE.md and implement step 2 of the implementation order (Domain + Domain.Tests).
Follow TDD: write the failing tests first and show them, then implement the minimal code
to make them pass, then refactor. Run dotnet test at the end and show the result.
Don't move on to step 3.
```

**Why step by step?** Small increments let me review each layer before building on top of it. A mistake in the Domain layer caught early is cheap; the same mistake discovered after the API and frontend exist is expensive.

### How I validated each step

- Checked the **project references** to confirm the dependency rule wasn't broken (e.g., no EF Core reference in `Application`).
- Read the **tests before the implementation** to confirm they actually tested the business rules in the spec, not just trivial happy paths.
- Ran `dotnet test` and the app myself instead of trusting the AI's summary.
- Tested the API manually through **Swagger**: register, login, CRUD, and accessing another user's entry.

---

## 6. Step 4 — Reviewing gaps and creating tasks

Once the app was generated, I reviewed it as a user and as a code reviewer, and turned what was missing into specific tasks. When a task changed the design, I updated `CLAUDE.md` first so the spec stayed the single source of truth.

### Task 4.1 — UI improvements

Small refinements to make the frontend more polished and user-friendly:

- [e.g., Remove unnecessary information ]

I also verified there were **no warnings or errors in the browser console**, which is one of the evaluation criteria.

### Task 4.2 — Removing the Docker option for SQL Server

The original spec supported running SQL Server either through LocalDB or a Docker container. I decided Docker wasn't necessary for this project, so I removed it to reduce complexity:

- Removed `docker-compose.yml`, the `.env` files, and the Docker connection string.
- The default connection string now points to SQL Server / LocalDB.
- Updated `CLAUDE.md` and the README so the setup instructions stay consistent.

**Lesson:** simpler setup means fewer points of failure when a reviewer clones and runs the project.

### Task 4.3 — Cache layer between the controller and the database

I added a caching layer to avoid hitting the database on every read.

**Design decision:** the cache is implemented as a **decorator** over the repository, so neither the controllers nor the business logic know it exists. This keeps the Clean Architecture boundaries intact.

- Uses `IMemoryCache`, with cache keys **scoped per user** so one user's cached data can never be served to another.
- **Invalidation**: create, update, and delete operations evict the affected user's cached entries, so the UI never shows stale data.
- Registered through DI in `Infrastructure`, so swapping the cache implementation (e.g., to Redis) wouldn't require any changes in `Application` or `Api`.

**What I checked in the AI's output:** that cache keys included the user id, that every write operation invalidated the cache, and that the tests covered both a cache hit and invalidation after an update.

---

## 7. Step 5 — Final verification and test coverage

After the tasks, I verified the whole application again:

**API**
- Full flow in Swagger: register → login → create → list → update → delete.
- Authorization: requests without a token return 401; another user's entry returns 404.
- Validation errors return `ProblemDetails` with 400.

**UI**
- Logged in with both seeded users to confirm each only sees their own entries.
- Full CRUD from the UI, on desktop and mobile widths.
- Browser console free of warnings and errors.

**Test coverage**

| Project | Coverage |
|---|---|
| Domain | 91.6% |
| Application | 100% |
| Infrastructure | 100% |
| API | 78.9% |
| Total | 93.7% lines 82% branches 100% of methods |

---

## 8. GenAI: how I evaluated and corrected the AI's output

This project shows how I use GenAI as a productivity tool without giving up control over the design.

**How I used it**
- A detailed spec (`CLAUDE.md`) as persistent context instead of long, repeated prompts.
- Small, scoped prompts (one layer or one task at a time).
- Explicit instructions to follow TDD and show failing tests first.

**How I validated its suggestions**
- Reviewed every change before accepting it, starting with the tests.
- Ran the tests and the app myself.
- Checked architectural boundaries (project references, no logic in controllers).

**What I corrected or improved**
- [e.g., UI improvements.]
- [e.g., Remove unnecessary code.]
- [e.g., Add cache layer.]

**Edge cases, authentication, and validation**
- Ownership enforced in the Application layer, not only in the UI.
- Passwords hashed with `PasswordHasher<User>`, never stored or returned in plain text.
- Server-assigned timestamps (UTC), never accepted from the client.
- Frontend validations mirror the backend, but the backend remains the source of truth.

---

## 9. Final architecture

```
Angular (wwwroot)
      │  HTTP /api
      ▼
Api (controllers, middleware, JWT)
      │
      ▼
Application (services, DTOs, validation, business rules)
      │  interfaces
      ▼
Infrastructure (cache decorator → EF Core repositories → SQL Server)
      │
      ▼
Domain (entities, invariants)
```

## 10. Demo credentials

| Email | Password |
|---|---|
| `demo@journal.com` | `Demo123!` |
| `alex@journal.com` | `Alex123!` |
