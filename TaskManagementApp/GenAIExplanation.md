# GenAI Tools — Task Management API

**Tools:** Claude (spec) + Claude Code (implementation)
**Output code:** [`TaskManagementApp/`](./TaskManagementApp)

## 1. Approach

I used a spec-driven approach:

1. Asked Claude to write a `CLAUDE.md` spec for the project.
2. Reviewed and adjusted the spec.
3. Gave the spec to Claude Code to build the app.

The spec keeps design separate from implementation, and Claude Code reads it automatically, so later prompts can stay short.

## 2. Prompts

**Prompt 1 — Spec (Claude)**

```
I need a claude.md for claude code to start the development of a RESTful API to manage tasks.
The app should be in .NET

* We need Create, read, update, and delete tasks (CRUD)
* Each task has a title, description, status, and due_date
* Tasks are associated with a user (assume basic User model exists)
* Add Auth with JWT and user model

Use Clean Architecture as structure
for data storage use SQLite
```

**Prompt 2 — Implementation (Claude Code)**, with `CLAUDE.md` at the project root:

```
start the development of this app
```

## 3. Output

The generated code is in `TaskManagementApp/`.

## 4. Validation

I validated the architectural decisions in the spec **before** generating any code: layers, entities, endpoints, and JWT auth. I adjusted them to what I wanted. Reviewing a one-page spec is faster than reviewing a full codebase.

After generation, I built the solution, ran the tests, and tested the endpoints.

## 5. Corrections

No corrections were needed. Claude Code did make some decisions on its own that the spec didn't cover:

- [Decision 1] — Use Scalar instead Swagger

**Lesson:** anything the spec doesn't define, the AI decides for you. Decisions I agree with should be added to the spec as explicit rules.

## 6. Next steps

To keep working on this project, I would formalize the process with Claude Code:

- **Agents** for each SDLC phase: spec, architecture review, development (TDD), testing, code review, and documentation.
- **A skill** that runs the spec-driven steps: write the spec → human review → implement → test → update the spec.

This makes the process repeatable, while I stay in control at the key point: approving the spec.
