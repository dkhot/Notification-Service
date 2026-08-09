# Notification Service — DDD / Clean Architecture Refactor

## Context

The solution already has the project skeleton the spec calls for (`Notification.Api`, `Notification.Application`, `Notification.Domain`, `Notification.Infrastructure`, `Notification.Worker`, `Callback.Dispatcher`, `SharedKernel`), but the layering has collapsed:

- **`SharedKernel` and `Notification.Domain` both define the same enums and the same `INotificationRepository`.** The Domain copy of `Enums.cs` and the whole `Dtos/` folder are excluded from compilation via `<Compile Remove>` in `Notification.Domain.csproj` — dead, confusing code left in the tree.
- **`Notification.Domain/INotificationRepository.cs`** declares methods taking domain entities (`Notification`, `Callback`), but **`Notification.Infrastructure/Persistence/NotificationRepository.cs`** (which claims to implement it) actually takes `SharedKernel.Dtos.NotificationDto` / `CallbackDto`. The signatures don't match the interface at all — **the Infrastructure project does not currently compile.**
- **Business/use-case logic lives in Infrastructure**, not Application: `NotificationRequestHandler` (validation + orchestration) sits in `Notification.Infrastructure/Handlers`, and it constructs a `Notification.Domain.Dtos.NotificationDto` (a type excluded from compilation) to pass to the repository — another compile break.
- **Ports and adapters are mixed in one project**: `INotificationProvider` / `INotificationProviderResolver` (abstractions) and `FileEmailProvider` / `NotificationProviderResolver` (implementations) all live together in `Notification.Infrastructure`, so nothing outside Infrastructure can depend on the abstraction without depending on the implementation too.
- **Hosts contain business rules**: `NotificationProcessingWorker` and `CallbackDispatcherWorker` (in the `Notification.Worker` / `Callback.Dispatcher` host projects) directly manipulate repositories and domain state, run the retry/backoff formula inline, and `CallbackDispatcherWorker` builds the outbound HTTP webhook call itself via `IHttpClientFactory`. There is no reusable, host-independent "process a notification" / "dispatch a callback" use case.
- **Anemic entities**: `Notification` and `Callback` are plain property bags; every caller sets `.Status` directly, so status-transition rules (e.g. the exponential-backoff/max-retry policy) are duplicated wherever they're needed instead of being owned by the entity.

Goal: restructure so each project has one clear responsibility and the dependency rule (`Api`/`Worker`/`Callback.Dispatcher` → `Infrastructure` → `Application` → `Domain` → `SharedKernel`) actually holds, matching what the user described: API, processor (Worker) and Callback Dispatcher share one Domain + one persistence store; `Infrastructure` owns the repositories/persistence/adapters; `SharedKernel` holds only the cross-process contracts (enums + queue message DTOs).

## Target architecture

```
SharedKernel            -- zero project references
  Enums.cs                 NotificationChannel, NotificationStatus, CallbackStatus (single source of truth)
  Messages/                NotificationRequestMessage, NotificationCompletedMessage (wire contracts for the queue)

Notification.Domain      -- references: SharedKernel
  Entities/                 Notification, Callback, SourceConfiguration — rich entities with behavior
  Repositories/             INotificationRepository, ICallbackRepository, ISourceConfigurationRepository, IUnitOfWork (ports)
  Exceptions/               SourceNotRegisteredException, DuplicateNotificationException

Notification.Application -- references: Notification.Domain, SharedKernel
  Abstractions/             IMessageQueue, INotificationProvider, INotificationProviderResolver, IWebhookSender (ports Infrastructure implements)
  Notifications/            CreateNotificationCommand, ICreateNotificationHandler/Handler, IProcessNotificationHandler/Handler, Validators/
  Callbacks/                IHandleNotificationCompletedHandler/Handler, IDispatchPendingCallbackHandler/Handler
  DependencyInjection/      AddNotificationApplication(IServiceCollection)

Notification.Infrastructure -- references: Notification.Domain, Notification.Application, SharedKernel
  Persistence/               NotificationDbContext, NotificationRepository, CallbackRepository, SourceConfigurationRepository, UnitOfWork, SqlMessageQueue, MessageQueueItem
  Providers/                 FileEmailProvider, FileSmsProvider, NotificationProviderResolver
  Webhooks/                  HttpWebhookSender
  HealthChecks/               NotificationDbContextHealthCheck
  Extensions/                 AddNotificationInfrastructure(IServiceCollection, connectionString), HostExtensions (EnsureNotificationDatabase)

Notification.Api          -- references: Notification.Application, Notification.Infrastructure
  Endpoints/                 NotificationEndpoints.MapNotificationEndpoints (POST /notifications) — Program.cs only builds host + wires DI + maps endpoints + translates exceptions to HTTP

Notification.Worker       -- references: Notification.Application, Notification.Infrastructure, SharedKernel
  NotificationProcessingWorker : BackgroundService — dequeue loop only, delegates each message to IProcessNotificationHandler

Callback.Dispatcher       -- references: Notification.Application, Notification.Infrastructure, SharedKernel
  CallbackDispatcherWorker : BackgroundService — dequeue loop + poll loop only, delegates to IHandleNotificationCompletedHandler / IDispatchPendingCallbackHandler

Notification.Tests        -- references: everything
  Unit tests for Application handlers using fake in-memory repository/queue/provider ports (no EF Core needed anymore)
  Unit tests for Domain entity behavior (Callback retry/backoff policy)
```

Ports live as close to their consumer as DDD dictates: repository interfaces (operate on Domain aggregates) live in **Domain**; infrastructure-facing ports used only by use cases (queue, provider, webhook sender) live in **Application**. Infrastructure implements both. Hosts depend on Application (to run use cases) and Infrastructure (composition root only — DI registration), never construct domain logic themselves.

## Changes by project

**SharedKernel**: delete `Dtos/` (CallbackDto, NotificationDto — duplicates of Domain entities, no longer needed since Application maps Domain entities ↔ messages directly) and `INotificationRepository.cs` (wrong layer). Keep `Enums.cs` and `Messages/*`. `IMessageQueue.cs` moves to `Notification.Application/Abstractions/IMessageQueue.cs` (it's a port, not a shared contract).

**Notification.Domain**: delete the dead `Enums/Enums.cs` and `Dtos/` folder and their csproj `<Compile Remove>` exclusions (no longer needed once the files are gone). Delete the old flat `INotificationRepository.cs`, replaced by `Repositories/INotificationRepository.cs` + `Repositories/ICallbackRepository.cs` + `Repositories/ISourceConfigurationRepository.cs` + `Repositories/IUnitOfWork.cs`. Rewrite `Entities/Notification.cs` and `Entities/Callback.cs` as rich entities: private setters, a static `Create(...)` factory, and behavior methods (`MarkProcessing`, `MarkSent`, `MarkFailed`, `MarkCallbackPending`, `MarkCallbackCompleted`, `MarkCallbackFailed` on `Notification`; `MarkAttempt`, `MarkCompleted`, `ScheduleRetry(reason)` — carrying the existing 5-attempt / `2^n * 5s` backoff formula from `CallbackDispatcherWorker` — on `Callback`). Add `Exceptions/SourceNotRegisteredException.cs` and `Exceptions/DuplicateNotificationException.cs`.

**Notification.Application**: new project content.
- `Abstractions/`: `IMessageQueue.cs` (moved), `INotificationProvider.cs` + `INotificationProviderResolver.cs` (moved from Infrastructure), new `IWebhookSender.cs` (`Task<WebhookSendResult> SendAsync(string url, object payload, CancellationToken)`).
- `Notifications/CreateNotificationCommand.cs` (renamed from `Models/CreateNotificationRequest.cs`), `Notifications/ICreateNotificationHandler.cs` + `CreateNotificationHandler.cs` (moved/fixed from `Notification.Infrastructure/Handlers/NotificationRequestHandler.cs` — now builds a `Notification` via `Notification.Create(...)`, throws the new Domain exceptions instead of `InvalidOperationException`, uses `INotificationRepository` + `IUnitOfWork` + `IMessageQueue` directly).
- `Notifications/Validators/CreateNotificationCommandValidator.cs` (the existing null/empty guard clauses extracted out of the handler body).
- `Notifications/IProcessNotificationHandler.cs` + `ProcessNotificationHandler.cs`: the full body of `NotificationProcessingWorker.ProcessRequestAsync` moves here (fetch notification, `MarkProcessing`, save, resolve provider, `SendAsync`, `MarkSent`/`MarkFailed`, check source config for callback eligibility, `MarkCallbackPending`, save, build `NotificationCompletedMessage`, enqueue).
- `Callbacks/IHandleNotificationCompletedHandler.cs` + `Callbacks/IDispatchPendingCallbackHandler.cs` + handlers: bodies of `CallbackDispatcherWorker.HandleCompletionAsync` and `ProcessPendingCallbacksAsync`/`ScheduleRetryAsync` move here, using `Callback.Create(...)`, `callback.MarkCompleted()`/`ScheduleRetry(...)`, and the new `IWebhookSender` port instead of an inline `IHttpClientFactory` call.
- `DependencyInjection/ServiceCollectionExtensions.cs`: `AddNotificationApplication(this IServiceCollection)` registering all four handlers (scoped). Needs `Microsoft.Extensions.DependencyInjection.Abstractions` package reference.

**Notification.Infrastructure**:
- `Persistence/NotificationRepository.cs`: rewritten to implement the real `Notification.Domain.Repositories.INotificationRepository` directly against `NotificationEntity`/`Callback` — no more DTO mapping.
- New `Persistence/CallbackRepository.cs`, `Persistence/SourceConfigurationRepository.cs`, `Persistence/UnitOfWork.cs` (wraps `NotificationDbContext.SaveChangesAsync`).
- `Extensions/SqlMessageQueue.cs`: same logic, implements `Notification.Application.Abstractions.IMessageQueue`.
- `Providers/FileEmailProvider.cs`, `FileSmsProvider.cs`, `NotificationProviderResolver.cs`: same logic, implement the `Notification.Application.Abstractions` interfaces; delete `Providers/NotificationProviders.cs` (old `INotificationProvider` definition, superseded).
- New `Webhooks/HttpWebhookSender.cs` implementing `IWebhookSender` via `IHttpClientFactory` (extracted from `CallbackDispatcherWorker`).
- `Extensions/ServiceCollectionExtensions.cs`: `AddNotificationInfrastructure` registers the new repository/UnitOfWork/queue/provider/webhook-sender types and keeps the health check registration.
- `Extensions/HostExtensions.cs`, `Persistence/NotificationDbContext.cs`, `NotificationDbContextExtensions.cs`, `HealthChecks/NotificationDbContextHealthCheck.cs` (moved from `Extensions/`): unchanged behavior, updated `using`s.

**Notification.Api**: `Endpoints/NotificationEndpoints.cs` with a `MapNotificationEndpoints` extension containing today's `POST /notifications` lambda, now depending on `ICreateNotificationHandler` and `CreateNotificationCommand`, catching `SourceNotRegisteredException`/`DuplicateNotificationException` (→ 400 / 409) plus the existing generic fallbacks. `Program.cs` shrinks to host build + `AddNotificationApplication()` + `AddNotificationInfrastructure()` + `MapNotificationEndpoints()`.

**Notification.Worker**: `NotificationProcessingWorker` keeps the polling `while` loop and `IMessageQueue.DequeueAsync<NotificationRequestMessage>`/`MarkProcessedAsync` calls, but the body of `ProcessRequestAsync` becomes a single call to `IProcessNotificationHandler.HandleAsync(message.Payload, ct)`. `Program.cs` adds `AddNotificationApplication()` alongside the existing `AddNotificationInfrastructure()` and drops the direct `Notification.Domain` project reference (no longer needed by the host).

**Callback.Dispatcher**: `CallbackDispatcherWorker` keeps its loop/poll structure but delegates to `IHandleNotificationCompletedHandler.HandleAsync(...)` and `IDispatchPendingCallbackHandler.DispatchNextAsync(ct)`. `Program.cs` adds `AddNotificationApplication()`, drops the direct `Notification.Domain` reference.

**Notification.Tests**: replace `NotificationRequestHandlerTests.cs` with `Notification.Application.Tests/Notifications/CreateNotificationHandlerTests.cs` using fake in-memory `INotificationRepository`/`ISourceConfigurationRepository`/`IUnitOfWork`/`IMessageQueue` (no EF Core `InMemory` provider needed anymore — this is the concrete payoff of moving the handler into Application). Add `Domain/CallbackTests.cs` covering `ScheduleRetry` backoff/exhaustion. Delete the placeholder `UnitTest1.cs`.

**csproj reference changes**: `Notification.Domain.csproj` — remove the `<Compile Remove>` hacks and the empty `Enums\` folder include. `Notification.Application.csproj` — add ref to `Notification.Domain` (already has it) + `Microsoft.Extensions.DependencyInjection.Abstractions` package. `Notification.Worker.csproj` / `Callback.Dispatcher.csproj` — drop the direct `Notification.Domain` project reference (keep `Notification.Application`, `Notification.Infrastructure`, `SharedKernel`). `Notification.Api.csproj` — drop the direct `SharedKernel` reference (transitively available via Application). Others unchanged.

## Verification

- `dotnet build src/NotificationService.slnx` — must succeed across all 8 projects (today `Notification.Infrastructure` and its dependents do not compile at all, so this is the primary correctness gate).
- `dotnet test src/Notification.Tests/Notification.Tests.csproj` — new Application-handler tests (create/duplicate/source-not-registered) and Domain `Callback` retry tests pass without a database.
- Manual smoke check (optional, needs SQL Server via `docker-compose up -d sqlserver`): run `Notification.Api`, `POST /notifications`, confirm 202 response, then run `Notification.Worker` and `Callback.Dispatcher` and confirm a file appears under `storage/email` or `storage/sms` and the notification's status progresses in the DB.
