# Notification Service

[![CI](https://github.com/<your-github-username>/Notification-Service/actions/workflows/ci.yml/badge.svg)](https://github.com/<your-github-username>/Notification-Service/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

This repository contains a proof-of-concept Enterprise Notification Service built on .NET 10, following
DDD / Clean Architecture. See [`CLAUDE.md`](CLAUDE.md) for the full layering rules and the
`.claude/skills/ddd-check` skill for auditing/refactoring against them.

> Replace `<your-github-username>` in the badges above with your actual GitHub username/repo once pushed.

## Highlights

- **Refactored, not vibe-coded**: the solution started as a non-compiling, layer-collapsed prototype
  (duplicate enums/DTOs across projects, a repository interface whose implementation used mismatched
  types) and was rebuilt into an enforced DDD/Clean Architecture — see `CLAUDE.md` for the before/after.
- **Spec-driven**: scope and structure trace back to source `.docx` requirement/implementation documents,
  not ad hoc decisions — see [Spec-driven development](#spec-driven-development) below.
- **Rich domain model**: `Callback.ScheduleRetry(...)` owns the exponential-backoff/max-attempts policy;
  `Notification` exposes behavior methods (`MarkSent`, `MarkFailed`, ...) instead of public setters.
- **Application layer testable without a database**: use-case handlers depend only on repository/queue
  ports, so unit tests use plain in-memory fakes — no EF Core `InMemory` provider required.
- **Self-auditing**: the `.claude/skills/ddd-check` skill greps the codebase for the exact anti-patterns
  that caused the original collapse (anemic setters, EF Core leaking into Application, business logic in
  a host), so the architecture doesn't quietly drift back.

## Spec-driven development

This service is built spec-first: the requirements and project layout are not inferred from code, they
come from two source documents that remain the authority for scope and structure —

- [`docs/Specification.docx`](docs/Specification.docx) — problem statement, functional/non-functional
  requirements, high-level architecture, event contract, storage schema, sequence and failure-handling
  flows, and the open decisions/questions for the POC.
- [`docs/ImplementationPlan.docx`](docs/ImplementationPlan.docx) — the feature breakdown (solution
  structure, `POST /notifications`, the notification worker, providers, the callback dispatcher, testing)
  and the deliverables per feature, including the project/folder structure the Architecture section below
  implements.

Changing scope (new channels, new endpoints, retry/backoff policy, storage shape) starts by updating
these documents, not by adding ad hoc code — the DDD layering in `CLAUDE.md` and `ddd-check` then keeps
the implementation honest against them. When the two disagree with the running code, the docs win and the
code is the thing to fix.

## Architecture

Dependencies flow one way only — hosts depend on Infrastructure, Infrastructure implements the ports
Application defines, Application depends on Domain, Domain depends only on SharedKernel:

```mermaid
graph TD
    subgraph Hosts["Hosts (composition roots)"]
        API[Notification.Api]
        WORKER[Notification.Worker]
        DISPATCHER[Callback.Dispatcher]
    end

    INFRA[Notification.Infrastructure]
    APP[Notification.Application]
    DOMAIN[Notification.Domain]
    SK[SharedKernel]

    API --> INFRA
    WORKER --> INFRA
    DISPATCHER --> INFRA
    INFRA --> APP
    APP --> DOMAIN
    DOMAIN --> SK
```

End-to-end request flow:

```mermaid
sequenceDiagram
    participant Client
    participant API as Notification.Api
    participant Queue as SQL-backed queue
    participant Worker as Notification.Worker
    participant Provider as Email/SMS provider
    participant Dispatcher as Callback.Dispatcher
    participant Webhook as Consumer webhook

    Client->>API: POST /notifications
    API->>Queue: enqueue NotificationRequestMessage
    API-->>Client: 202 Accepted
    Worker->>Queue: dequeue
    Worker->>Provider: send
    Worker->>Queue: enqueue NotificationCompletedMessage
    Dispatcher->>Queue: dequeue completion event
    Dispatcher->>Dispatcher: persist Callback, schedule retry on failure
    Dispatcher->>Webhook: POST callback payload
```

## Projects

- `Notification.Api` - Minimal HTTP API host. `POST /notifications` maps straight to the
  `ICreateNotificationHandler` Application use case.
- `Notification.Worker` - Background host that dequeues notification requests and delegates each one to
  the `IProcessNotificationHandler` Application use case (resolves a provider, sends, updates status,
  publishes the completion event).
- `Callback.Dispatcher` - Background host that delegates to the `IHandleNotificationCompletedHandler` and
  `IDispatchPendingCallbackHandler` Application use cases (persist callback, retry with backoff, invoke
  the consumer's webhook).
- `Notification.Application` - Use-case handlers (`Notifications/`, `Callbacks/`), validators, and the
  ports Infrastructure implements (`Abstractions/`: `IMessageQueue`, `INotificationProvider(Resolver)`,
  `IWebhookSender`). No EF Core, no HTTP client, no queue SDK.
- `Notification.Domain` - Rich entities (`Notification`, `Callback`, `SourceConfiguration`) with factory
  methods and behavior (e.g. `Callback.ScheduleRetry` owns the retry/backoff policy), repository ports
  (`Repositories/`), and typed domain exceptions (`Exceptions/`).
- `Notification.Infrastructure` - Implements every Domain repository port and every Application
  abstraction port: EF Core persistence (`Persistence/`), file-based Email/SMS providers (`Providers/`),
  the HTTP webhook sender (`Webhooks/`), health checks, and the `AddNotificationInfrastructure` DI
  extension.
- `SharedKernel` - Cross-process contracts only: enums (`NotificationChannel`, `NotificationStatus`,
  `CallbackStatus`) and queue wire-format messages (`Messages/`).
- `Notification.Tests` - xUnit tests for Application handlers (using in-memory fakes of the Domain ports —
  no database needed) and Domain entity behavior (e.g. the `Callback` retry policy).

## Running locally

> The SQL Server password baked into `appsettings.json`/`docker-compose.yml` (`Your_password123`) is a
> local-dev-only placeholder — it's committed on purpose for a zero-friction clone-and-run experience, not
> something to reuse anywhere real.

1. Update the SQL Server connection string in `src/Notification.Api/appsettings.json`, `src/Notification.Worker/appsettings.json`, and `src/Callback.Dispatcher/appsettings.json` if needed.
2. Start SQL Server locally or use Docker Compose:

```bash
docker-compose up -d sqlserver
```

3. Run the API:

```bash
dotnet run --project src/Notification.Api/Notification.Api.csproj
```

4. Run the workers in separate terminals:

```bash
dotnet run --project src/Notification.Worker/Notification.Worker.csproj

dotnet run --project src/Callback.Dispatcher/Callback.Dispatcher.csproj
```

## Docker

To build and run the full stack with Docker Compose:

```bash
docker compose up --build
```

## Tests

Run unit tests with:

```bash
dotnet test src/Notification.Tests/Notification.Tests.csproj
```

CI runs the same two commands on every push/PR via `.github/workflows/ci.yml`.

## License

MIT — see [`LICENSE`](LICENSE).
