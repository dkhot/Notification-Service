# Notification Service Execution Plan

## Goal
Implement a proof-of-concept Enterprise Notification Service in .NET 10 that supports:
- HTTP API request intake for notification creation
- Asynchronous queue-based processing
- Provider dispatch for Email and SMS via file-based POC providers
- Delivery status tracking and completion event publishing
- Callback dispatch with retries and webhook invocation
- SQL Server persistence, Serilog logging, health checks, and Docker Compose support

## Scope
Based on the provided specification and implementation plan, this effort will build the end-to-end POC for:
- `POST /notifications` API
- Notification persistence and queue publish
- Notification worker that resolves providers, sends notifications, updates metadata, and publishes completion events
- Callback dispatcher that consumes completion events, persists callback attempts, retries failures, and invokes webhook callbacks
- Supporting infrastructure for configuration, logging, health checks, and local persistence

Out of scope:
- Template management
- Subscription management
- Scheduling
- Push notifications
- Provider failover
- Attachments
- Multi-tenancy
- UI

## Proposed Implementation

1. Create a .NET solution matching the suggested structure:
   - Notification.Api
   - Notification.Application
   - Notification.Domain
   - Notification.Infrastructure
   - Notification.Worker
   - Callback.Dispatcher
   - SharedKernel

2. Define domain models and storage schemas:
   - Notification entity with MessageId, SourceId, EventType, Channel, Recipient, Subject, Body, Payload, Status, CreatedAt, CompletedAt, Provider, FailureReason
   - Callback entity with NotificationId, SourceId, RetryCount, Status, LastAttempt, NextAttempt, FailureReason
   - Source configuration entity with SourceId, Application, WebhookUrl, Enabled
   - Use EF Core with SQL Server for persistence and migrations

3. Implement API:
   - `POST /notifications` endpoint
   - Request validation with MessageId uniqueness and SourceId validation against source configuration; callback URL is not supplied in the request
   - Persist notification metadata
   - Publish a `NotificationRequest` message to a queue via an `IMessageBus` abstraction
   - Return quick acknowledgement within target latency

4. Implement worker and provider infrastructure:
   - Queue consumer for notifications using an `IMessageBus` abstraction
   - Provider resolution based on channel
   - File-based Email and SMS providers for POC
   - Notification status lifecycle: Pending -> Queued -> Processing -> Sent/Failed -> CallbackPending -> CallbackCompleted/CallbackFailed
   - Notification status updates and completion event publishing via the message bus
   - Retry and idempotency support in the worker

5. Implement callback dispatcher:
   - Consume `NotificationCompleted` events
   - Lookup callback webhook from source configuration via SourceId
   - Persist callback attempt metadata
   - Invoke configured consumer webhook with retry/backoff
   - Update callback status and support dead-letter handling after repeated failures

6. Add supporting infrastructure:
   - Configuration via appsettings and environment variables
   - Serilog logging
   - Health checks for API, database, and queue connectivity
   - Docker Compose definition for SQL Server and the services
   - Basic xUnit tests for validation, worker flow, and callback retry logic
   - POC mode without authentication; design for production HMAC-signed webhook callbacks and internal auth later

## Ambiguities and Assumptions

- The spec does not describe how webhook callback URLs are registered or supplied. Assumption: callback URLs are derived from a master source configuration table keyed by `SourceId` (`SourceId`, `Application`, `WebhookUrl`, `Enabled`). This should be clarified before implementation.
- The spec does not define the exact queue topology. Assumption: use separate queues/topics for notification requests and completion events via an `IMessageBus` abstraction.
- The spec references Azure Service Bus but local development support is unclear. Implementation will use a queue abstraction with an Azure Service Bus implementation and preserve the option to substitute RabbitMQ or another broker later.
- The request payload shape is not fully specified beyond the listed fields. Assumption: `Payload` is opaque JSON metadata and `Recipient` is a single address/string.
- The implementation should validate MessageId uniqueness only, not implement additional deduplication schemes at this stage.
- The callback contract is not defined; assumption: the webhook receives the completion event payload in JSON.
- There is no existing code in the repository, so the implementation will start from a new solution scaffold.

## Risks

- Local development may be blocked by the lack of a Service Bus emulator or accessible Azure Service Bus instance.
- Callback delivery semantics and webhook authentication are underspecified and may require additional design.
- Idempotency and at-least-once delivery across queue and persistence boundaries are non-trivial to implement correctly.
- Without a consumer registration mechanism, callback processing cannot be fully validated.
- The service may need a real queue provider or fallback solution to satisfy the design if Azure Service Bus is not accessible.

## Next Step
Once this plan is approved, implement the service by creating the solution and the project structure, then iteratively develop the API, worker, and callback dispatcher.
