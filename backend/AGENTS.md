# AGENTS.md — ePrevzem Backend

Guidance for AI agents working in `backend/`. Read this before editing.

## Stack

ASP.NET Core 9 Web API, EF Core 9 + Npgsql (PostgreSQL), MediatR, FluentValidation, Serilog, JWT bearer auth. xUnit + FluentAssertions + Testcontainers.PostgreSql for tests. Target framework `net9.0` across all projects.

## Architecture — modular monolith, Clean Architecture

Four projects + tests under `backend/`. Strict one-way dependency flow:

```
Api ─▶ Application ─▶ Domain
 │           ▲
 └─▶ Infrastructure ─▶ Application, Domain
```

- **`ePrevzem.Domain`** — entities, value objects, aggregates (`AggregateRoot<TId>`), domain events (`IDomainEvent`). **Zero external dependencies.** No EF, no MediatR, no ASP.NET.
- **`ePrevzem.Application`** — use cases as MediatR `IRequest`/`IRequestHandler`, DTOs, FluentValidation validators, port interfaces in `Common/Abstractions/` (`IClock`, `ICurrentUser`, `ITenantContext`, `IEPrevzemDbContext`, future `ISiTrustClient`, `ILockerGateway`, `IAuditLog`, `INotificationSender`). No EF types in public APIs.
- **`ePrevzem.Infrastructure`** — EF Core `EPrevzemDbContext`, repositories, adapters for SI-TRUST and Direct4.me lockers, `SystemClock`. Composition via `AddInfrastructure(IConfiguration)`.
- **`ePrevzem.Api`** — controllers (thin: dispatch to MediatR), DI wiring, auth, CORS, Serilog, OpenAPI, `/health`. Configuration via `appsettings.json`.

## Feature modules

Organize by **bounded context**, not technical layer. Mirror folders in `Application/` and `Domain/`:

```
Organizations / Pickups / Lockers / Delegations / Identity / Audit / Notifications
```

Each module owns its aggregates, handlers, validators, and ports. **Cross-module communication = domain events**, never direct entity references.

## Non-negotiable rules

- **Dependency rule.** Never let `Domain` reference anything outside itself. Never let `Application` reference `Infrastructure` or ASP.NET types. Adding `EntityFrameworkCore` to `Application.csproj` is a smell — push the leak to Infrastructure behind a port.
- **Controllers are thin.** Parse → dispatch via `IMediator` → return `Results.Ok`/`Problem`. No business logic, no EF, no `if`-trees in controllers.
- **Multi-tenancy.** Every tenant-scoped entity carries `OrganizationId`. Apply EF Core global query filters driven by `ITenantContext`. Never filter by tenant manually in handlers — rely on the filter.
- **Pickup lifecycle.** Model state transitions on the `Pickup` aggregate with explicit guarded methods (`Assign`, `MarkReady`, `Complete`, `Expire`). Do **not** flip status from outside the aggregate.
- **Audit log is append-only.** Write from a MediatR pipeline behavior or domain-event handler — never from controllers. No updates, no deletes.
- **Delegations are first-class aggregates** with their own lifecycle (revocable, validity window, independently audited).
- **JWT.** API issues its own short-lived access tokens after SI-TRUST identification. Never relay SI-TRUST tokens to clients.
- **Secrets.** Empty in `appsettings.json`. Dev values in `appsettings.Development.json`. Real secrets via user-secrets / env vars — never commit.
- **Migrations.** EF Core migrations live in `Infrastructure/Persistence/Migrations/`. Add via `dotnet ef migrations add <Name> --project backend/ePrevzem.Infrastructure --startup-project backend/ePrevzem.Api`.

## Testing

Unit tests for `Domain` and `Application` (no DB). Integration tests in `ePrevzem.Tests` use Testcontainers Postgres + `WebApplicationFactory` — **never mock the DB**. One assembly, organize folders by feature.

## Commands

```
dotnet build ePrevzem.sln
dotnet run --project backend/ePrevzem.Api
dotnet test backend/ePrevzem.Tests
dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~Pickups"
```

Code and identifiers in English. Slovenian only in user-facing strings (error messages reaching clients).
