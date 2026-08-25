---
name: ddd-check
description: Audit or refactor the Notification Service .NET solution against its DDD/Clean Architecture layering (SharedKernel -> Domain -> Application -> Infrastructure -> hosts). Use when the user asks to check/verify/audit architecture boundaries in this repo, when planning further refactoring toward this structure, or when new code needs to be placed in the right project. Source of truth: docs/Specification.docx and docs/ImplementationPlan.docx, and the 2026-08 refactor that took the solution from a non-compiling, layer-collapsed state to this structure.

---

# ddd-check — Notification Service architecture guardrail

This skill encodes the target architecture defined by `docs/Specification.docx` /
`docs/ImplementationPlan.docx` and realized by the DDD/Clean Architecture refactor of this solution.
Use it both to **check** whether current or proposed code respects the boundaries, and to **guide**
further refactoring/feature work so the layering never collapses again.

Full rationale and rules also live in the project's `CLAUDE.md` (always-loaded) — this skill is the
active/on-demand counterpart: run it when you want an explicit audit pass or when starting non-trivial
structural work.

## Origin (why these boundaries exist)

The spec describes one bounded context — notification delivery — split across four cooperating
processes that share one domain and one persistence store:

- **Notification.Api** — intake: validates a request, persists it, publishes it to a queue.
- **Notification.Worker** ("processor") — dequeues, resolves a provider (Email/SMS), sends, updates
  status, publishes a `NotificationCompleted` event.
- **Callback.Dispatcher** — consumes `NotificationCompleted`, persists a callback record, retries with
  backoff, invokes the consumer's webhook.
- **SharedKernel** — enums + queue message contracts shared by all of the above.

The implementation plan named the projects (`Notification.Api`, `.Application`, `.Domain`,
`.Infrastructure`, `.Worker`, `Callback.Dispatcher`, `SharedKernel`) but the repo's first pass collapsed
the boundaries: duplicate enums/DTOs in both `SharedKernel` and `Notification.Domain`, a repository
interface in Domain whose implementation in Infrastructure used entirely different (DTO) parameter
types, and business/orchestration logic sitting in Infrastructure and in the host `Program.cs`/worker
files instead of Application. **The project did not compile.** The refactor re-established the
boundaries below; this skill exists to keep them from drifting back.

## Target architecture

```
Notification.Api / Notification.Worker / Callback.Dispatcher   (hosts — composition roots)
        -> Notification.Infrastructure
                -> Notification.Application
                        -> Notification.Domain
                                -> SharedKernel
```

A project may only reference projects to its right/below. Check `*.csproj` `<ProjectReference>` entries
against this graph — any reference pointing the other way (e.g. Domain -> Application, or Application ->
Infrastructure) is a violation.

| Layer | Owns | Must never contain |
|---|---|---|
| **SharedKernel** | Enums (`NotificationChannel`, `NotificationStatus`, `CallbackStatus`); queue wire messages (`Messages/*`) | Interfaces/ports, DTOs that duplicate Domain entities, any logic |
| **Notification.Domain** (`Entities/`, `Repositories/`, `Exceptions/`) | Rich entities with private setters + `Create(...)` factory + behavior methods that own state transitions (e.g. `Callback.ScheduleRetry` owns the retry/backoff policy); repository port interfaces (`INotificationRepository`, `ICallbackRepository`, `ISourceConfigurationRepository`, `IUnitOfWork`); typed domain exceptions | EF Core or any package reference beyond SharedKernel; public setters mutated from outside the entity |
| **Notification.Application** (`Abstractions/`, `Notifications/`, `Callbacks/`, `.../Validators/`, `DependencyInjection/`) | One handler per use case (`ICreateNotificationHandler`, `IProcessNotificationHandler`, `IHandleNotificationCompletedHandler`, `IDispatchPendingCallbackHandler`); ports Infrastructure must implement (`IMessageQueue`, `INotificationProvider`, `INotificationProviderResolver`, `IWebhookSender`); input validators; the `AddNotificationApplication` DI extension | EF Core, `HttpClient`/`IHttpClientFactory`, any concrete infrastructure/SDK type |
| **Notification.Infrastructure** (`Persistence/`, `Providers/`, `Webhooks/`, `HealthChecks/`, `Extensions/`) | Implementations of every Domain repository port and every Application abstraction port; the `AddNotificationInfrastructure` DI extension | Business rules, use-case orchestration, retry/backoff policy |
| **Hosts** (`Notification.Api`, `Notification.Worker`, `Callback.Dispatcher`) | `Program.cs` composition root (`AddNotificationApplication()` + `AddNotificationInfrastructure()`); thin I/O loop or minimal-API endpoint that calls **one** Application handler | Direct repository/`DbContext`/domain-entity access; retry/backoff or status-transition logic inline |

## Anti-patterns to grep for during a check

Run these against `src/**/*.cs` (excluding `obj/`, `bin/`) — any hit is a finding to investigate:

- **Duplicate contracts**: a second definition of `NotificationChannel`/`NotificationStatus`/
  `CallbackStatus`, or a `Dtos/` folder anywhere outside `Notification.Application`'s message-mapping
  code. (`grep -rn "enum NotificationStatus" src` should return exactly one hit.)
- **EF Core outside Infrastructure**: `using Microsoft.EntityFrameworkCore` in `Notification.Domain` or
  `Notification.Application`.
- **HTTP/queue SDK outside Infrastructure**: `IHttpClientFactory`, `HttpClient`, or a message-queue SDK
  type referenced from `Notification.Application` or a host project directly (should only ever appear
  behind the `IWebhookSender`/`IMessageQueue` ports, implemented in Infrastructure).
- **Anemic entity mutation**: `notification.Status =` or `callback.Status =` anywhere outside
  `Notification.Domain/Entities/*.cs` — status must change only via a behavior method.
- **Business logic in a host**: a `BackgroundService` or minimal-API lambda that calls a repository or
  `IUnitOfWork` directly instead of a single Application handler call.
- **Namespace/type collision**: an unqualified reference to `Notification` (the entity) inside a file
  under any `Notification.*` namespace without an alias — fails to compile with `CS0118 'Notification'
  is a namespace but is used like a type`. Always import via
  `using NotificationEntity = Notification.Domain.Entities.Notification;`.
- **DB-coupled unit tests**: an Application handler test referencing
  `Microsoft.EntityFrameworkCore.InMemory` instead of an in-memory fake of the Domain/Application ports
  (see `Notification.Tests/Notifications/CreateNotificationHandlerTests.cs` for the pattern).

## Workflow

**For an audit/check pass** (no code changes intended): read `CLAUDE.md`, walk the anti-pattern list
above with Grep, check each `.csproj`'s `<ProjectReference>`s against the dependency graph, then run the
verification commands below. Report findings by layer and rule violated; don't fix unless asked.

**For a refactor or new feature that touches multiple layers**: use `EnterPlanMode`. Skip exploration
only if the scope is already fully understood (file paths and layer known); otherwise explore first.
Write the plan (via the plan-mode plan file, not a file in this repo) with a **Context** section (why),
a **Target architecture** section (reuse the table above, adjusted for the change), and a
**Verification** section. Call `ExitPlanMode` for approval. Implement inside-out —
SharedKernel → Domain → Application → Infrastructure → hosts → Tests — so each layer only ever compiles
against already-finished layers below it. Use `TodoWrite` to track the per-layer steps.

## Verification (always run after structural changes)

```bash
dotnet build src/NotificationService.slnx
dotnet test src/Notification.Tests/Notification.Tests.csproj
```

Both must report 0 errors. This repo has a documented history of dead, `<Compile Remove>`'d code masking
real compile breaks — don't trust that code "looks right," build and test it.
