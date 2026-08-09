# Notification Service — Architecture Rules

This solution follows DDD / Clean Architecture. The dependency direction is one-way:

```
Notification.Api / Notification.Worker / Callback.Dispatcher   (hosts — composition roots)
        -> Notification.Infrastructure
                -> Notification.Application
                        -> Notification.Domain
                                -> SharedKernel
```

A project may only reference projects to its right/below in that chain. Never add a reference that
points the other way (e.g. Domain must never reference Application or Infrastructure).

## Where things belong

- **SharedKernel** — only cross-process contracts: enums (`NotificationChannel`, `NotificationStatus`,
  `CallbackStatus`) and queue wire-format messages (`Messages/*`). No interfaces, no DTOs that duplicate
  Domain entities, no business logic.
- **Notification.Domain** — rich entities only (`Entities/`): private setters, a static `Create(...)`
  factory, and behavior methods that own state transitions (e.g. `Callback.ScheduleRetry(...)` owns the
  retry/backoff policy — don't reimplement it in a handler or worker). Repository ports live in
  `Repositories/` (`INotificationRepository`, `ICallbackRepository`, `ISourceConfigurationRepository`,
  `IUnitOfWork`) — interfaces only, no EF Core or any infrastructure dependency. Business-rule failures
  are typed exceptions in `Exceptions/` (e.g. `DuplicateNotificationException`), not generic
  `InvalidOperationException`.
- **Notification.Application** — one use case per handler under `Notifications/` and `Callbacks/`
  (`ICreateNotificationHandler`, `IProcessNotificationHandler`, `IHandleNotificationCompletedHandler`,
  `IDispatchPendingCallbackHandler`). Ports that Infrastructure must implement live in `Abstractions/`
  (`IMessageQueue`, `INotificationProvider`, `INotificationProviderResolver`, `IWebhookSender`). Input
  validation lives in `Notifications/Validators/`. This layer has no EF Core, no HTTP client, no queue
  SDK — only Domain + SharedKernel + these ports.
- **Notification.Infrastructure** — implements every Domain repository port and every Application
  abstraction port (`Persistence/`, `Providers/`, `Webhooks/`, `HealthChecks/`). One DI extension,
  `AddNotificationInfrastructure`, registers all of it.
- **Hosts** (`Notification.Api`, `Notification.Worker`, `Callback.Dispatcher`) — composition roots only.
  `Program.cs` wires DI (`AddNotificationApplication()` + `AddNotificationInfrastructure(...)`) and starts
  the host. Background services / minimal API endpoints do I/O (dequeue, HTTP mapping) and delegate the
  actual work to one Application handler call — they must never touch a repository, `DbContext`, or
  domain entity directly, and must never contain retry/backoff or status-transition logic inline.

## Hard rules

1. **No duplicate DTOs.** Don't create a `Dtos/NotificationDto` that mirrors the `Notification` entity
   "for safety" — Application maps Domain entities directly to/from SharedKernel messages. This
   duplication is exactly what caused the layering to collapse before the 2026-08 refactor (dead,
   `<Compile Remove>`'d files that quietly went stale and broke the build).
2. **Ports live next to their consumer, not their implementer.** Repository interfaces belong in
   Domain (they operate on aggregates). Infra-facing ports used only by use cases (queue, provider,
   webhook sender) belong in Application. Infrastructure implements both — it never *defines* a port
   another layer depends on.
3. **Entities are rich, not anemic.** Don't add public setters and mutate `.Status` from outside the
   entity. Add a behavior method (`MarkSent`, `MarkFailed`, `ScheduleRetry`, ...) so the invariant lives
   in one place.
4. **Namespace/type name collision.** `Notification.Domain.Entities.Notification` collides with the
   `Notification.*` root namespace from inside any file under that root — referencing it unqualified
   inside a `namespace Notification.Domain.Repositories { ... }` block (etc.) fails to compile with
   `CS0118 'Notification' is a namespace but is used like a type`. Always import it via an alias:
   `using NotificationEntity = Notification.Domain.Entities.Notification;`. Same caution applies to any
   other type whose simple name matches a namespace segment.
5. **Application handlers must be unit-testable without a database.** Depend only on the Domain
   repository ports / Application abstractions (never `DbContext` or EF Core types) so tests can use
   simple in-memory fakes (see `Notification.Tests/Notifications/CreateNotificationHandlerTests.cs` for
   the pattern) instead of `Microsoft.EntityFrameworkCore.InMemory`.

## Verification loop after structural changes

```bash
dotnet build src/NotificationService.slnx
dotnet test src/Notification.Tests/Notification.Tests.csproj
```

Both must be clean (0 errors) before considering a change done — this project has a history of
`<Compile Remove>`'d dead code masking real compile breaks, so don't trust "it looks right," build it.
