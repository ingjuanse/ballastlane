# FlowTrack Architecture

FlowTrack is a .NET 8 Clean Architecture solution. The solution deliberately does not use Entity Framework, Dapper, or MediatR.

## Projects and references

| Project | Responsibility | References |
| --- | --- | --- |
| `FlowTrack.Domain` | Enterprise business rules, entities, value objects, and domain contracts | None |
| `FlowTrack.Application` | Use cases, application services, DTOs, and ports/interfaces for external concerns | `FlowTrack.Domain` |
| `FlowTrack.Infrastructure` | Implementations for persistence, messaging, files, and other external systems | `FlowTrack.Application`, `FlowTrack.Domain` |
| `FlowTrack.Api` | HTTP endpoints, composition root, serialization, and API hosting concerns | `FlowTrack.Application`, `FlowTrack.Infrastructure` |
| `FlowTrack.UnitTests` | Fast isolated tests for domain and application behavior | `FlowTrack.Domain`, `FlowTrack.Application` |
| `FlowTrack.IntegrationTests` | Tests that exercise the hosted API and infrastructure adapters together | `FlowTrack.Api`, `FlowTrack.Infrastructure` |

## Folder structure

```text
FlowTrack/
├── FlowTrack.Api/
│   ├── Controllers/
│   └── Program.cs
├── FlowTrack.Application/
│   ├── Abstractions/
│   ├── WorkItems/
│   └── DependencyInjection.cs
├── FlowTrack.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   └── Validation/
├── FlowTrack.Infrastructure/
│   ├── Persistence/
│   ├── MongoDb/
│   └── DependencyInjection.cs
├── FlowTrack.UnitTests/
├── FlowTrack.IntegrationTests/
└── ARCHITECTURE.md
```

As the system grows, keep features grouped within each layer. For example, a `Projects` use case can have its application service and request/response models under `FlowTrack.Application/Projects`, while its domain model remains under `FlowTrack.Domain` and its external adapter under `FlowTrack.Infrastructure`.

## Separation of concerns

- **Domain** contains business meaning and invariants. It does not know about ASP.NET Core, databases, logging providers, or infrastructure implementations. `User` owns identity/profile invariants, while `TaskItem` owns title, description, ownership, due-date, and status-transition rules.
- **Application** coordinates use cases. It depends on abstractions such as `IWorkItemRepository`, not on concrete storage technologies.
- **Infrastructure** implements application abstractions. The sample uses an in-memory repository to keep the initial solution free from persistence frameworks.
- **API** translates HTTP requests and responses and composes the application and infrastructure services in `Program.cs`.
- **Unit tests** validate domain and application behavior without starting a server or requiring external resources.
- **Integration tests** are reserved for boundary tests involving the hosted API and real infrastructure adapters.

## Dependency flow

The intended direction is:

```text
FlowTrack.Api
	-> FlowTrack.Application
	-> FlowTrack.Infrastructure
		 -> FlowTrack.Application
		 -> FlowTrack.Domain
FlowTrack.Application
	-> FlowTrack.Domain
```

The Domain project is the center of the architecture and has no project dependencies. Infrastructure dependencies point inward through application-defined interfaces. The API is the composition root where concrete implementations are registered with dependency injection.

## Task domain rules

- Users require a username of 3-100 characters, a valid email address, and a nonempty password hash. Password hashing is performed outside the domain.
- Tasks require a title of 1-200 characters and a nonempty `UserId`. Descriptions are optional and limited to 2,000 characters.
- New tasks start in `Todo`. Valid forward transitions are `Todo` to `InProgress`, `Completed`, or `Cancelled`, and `InProgress` to `Completed` or `Cancelled`.
- Completed and cancelled tasks are terminal and cannot be reopened or edited.
- Due dates may be omitted, but supplied dates cannot be in the past.

## MongoDB integration

MongoDB is isolated in `FlowTrack.Infrastructure/MongoDb`:

- `MongoDbSettings` binds the `MongoDb` configuration section and validates the connection string, database name, and collection names.
- `MongoDbContext` exposes typed `IMongoCollection<User> Users` and `IMongoCollection<TaskItem> Tasks` collections.
- `AddInfrastructure` registers the settings, a singleton `IMongoClient`, and a singleton `MongoDbContext`.
- The API configures the integration through `builder.Services.AddInfrastructure(builder.Configuration)`.

Default collection names are `users` and `tasks`. The connection string and database name should be overridden with environment-specific configuration or user secrets outside local development.
