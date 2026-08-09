# Notification Service - Architecture Rules

This solution follows DDD / Clean Architecture. The dependency direction is one-way:

```text
Notification.Api / Notification.Worker / Callback.Dispatcher
        -> Notification.Infrastructure
                -> Notification.Application
                        -> Notification.Domain
                                -> SharedKernel
```

A project may only reference projects to its right or below in that chain. Never add a reference that points the other way. For example, Domain must never reference Application or Infrastructure.

## Where Things Belong

- **SharedKernel** - Cross-process contracts only: enums (`NotificationChannel`, `NotificationStatus`, `CallbackStatus`) and queue wire-format messages (`Messages/*`). No interfaces, no duplicated Domain DTOs, no business logic.
- **Notification.Domain** - Rich entities under `Entities/`, repository ports under `Repositories/`, and typed business-rule exceptions under `Exceptions/`.
- **Notification.Application** - Use-case handlers, validators, and infrastructure-facing ports such as `IMessageQueue`, `INotificationProvider`, `INotificationProviderResolver`, and `IWebhookSender`.
- **Notification.Infrastructure** - EF Core persistence, repositories, SQL-backed queue, file-based notification providers, webhook sender, health checks, and dependency injection registration.
- **Hosts** - `Notification.Api`, `Notification.Worker`, and `Callback.Dispatcher` are composition roots. They wire dependencies, map inbound I/O, and delegate business work to Application handlers.

## Hard Rules

1. **No duplicate DTOs.** SharedKernel contains queue contracts; Domain contains entities. Do not add DTOs that mirror Domain entities just to pass data between layers.
2. **Ports live next to their consumer.** Repository interfaces belong in Domain. Use-case infrastructure ports belong in Application. Infrastructure implements both.
3. **Entities are rich, not anemic.** Do not add public setters and mutate state from outside the entity. Add behavior methods such as `MarkSent`, `MarkFailed`, or `ScheduleRetry`.
4. **Avoid namespace/type name collisions.** `Notification.Domain.Entities.Notification` collides with the `Notification.*` root namespace from inside files under that root. Use an alias such as `using NotificationEntity = Notification.Domain.Entities.Notification;`.
5. **Application handlers must be unit-testable without a database.** Depend on repository and adapter ports so tests can use simple in-memory fakes.

## Verification Loop

```bash
dotnet build src/NotificationService.slnx
dotnet test src/Notification.Tests/Notification.Tests.csproj
```

Both commands should pass before considering a structural change complete.
