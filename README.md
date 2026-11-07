# FlowTrack

FlowTrack is a task-management application built with ASP.NET Core 8, MongoDB, and a standalone Angular application. It provides JWT-based authentication and an authenticated task workflow for creating, viewing, updating, and deleting tasks.

## Overview

FlowTrack is designed as a Clean Architecture solution with a clear separation between:

- Domain business rules
- Application use cases
- Infrastructure and persistence
- HTTP API concerns
- Angular presentation and client-side API integration

The application includes:

- User registration and login
- JWT access-token authentication
- Authenticated task management
- Pagination, filtering, and search support
- Swagger/OpenAPI documentation
- Global exception handling with ProblemDetails responses
- xUnit, FluentAssertions, and Moq tests
- Docker Compose orchestration for the API, web application, and MongoDB

## Architecture

```text
FlowTrack/
├── FlowTrack.Domain/
├── FlowTrack.Application/
├── FlowTrack.Infrastructure/
├── FlowTrack.Api/
├── FlowTrack.Web/
├── FlowTrack.UnitTests/
├── FlowTrack.IntegrationTests/
├── docker-compose.yml
└── FlowTrack.slnx
```

### Backend architecture

#### `FlowTrack.Domain`

Contains the core business model and rules. This project has no dependency on the API, database, or UI.

Responsibilities include:

- `User` and `TaskItem` entities
- `TaskStatus` values
- Domain validation
- Domain business-rule exceptions
- Task state transitions and terminal-state rules

#### `FlowTrack.Application`

Contains application workflows and abstractions. It depends on the Domain project but not on infrastructure details.

Responsibilities include:

- Register user and login use cases
- Create, query, update, and delete task use cases
- Repository interfaces
- Password-hashing and token-generation abstractions
- Validation and paged-result contracts

#### `FlowTrack.Infrastructure`

Provides implementations for external concerns.

Responsibilities include:

- MongoDB context and repositories
- BCrypt password hashing
- JWT token generation
- Infrastructure dependency injection

#### `FlowTrack.Api`

The ASP.NET Core composition root and HTTP boundary.

Responsibilities include:

- Controllers and API DTOs
- Authentication and authorization configuration
- JWT bearer validation
- Swagger/OpenAPI configuration
- Global exception middleware
- Dependency-injection composition

### Frontend architecture

#### `FlowTrack.Web/src/app/core`

Application-wide services and infrastructure:

- `AuthService`
- `TaskService`
- JWT interceptor
- Authentication guard
- Shared client-side models

#### `FlowTrack.Web/src/app/shared`

Reusable presentation components and UI utilities that are independent of a specific business feature.

#### `FlowTrack.Web/src/app/features`

Business-facing screens organized by feature:

- Authentication
- Dashboard
- Tasks

#### `FlowTrack.Web/src/app/layouts`

Application shells such as the authenticated sidebar layout and main content area.

## Technologies

### Backend

- .NET 8
- ASP.NET Core Web API
- C# nullable reference types
- MongoDB.Driver 3.x
- MongoDB 8
- BCrypt.Net-Next
- JWT Bearer authentication
- Swashbuckle.AspNetCore
- RFC 7807 ProblemDetails

### Frontend

- Angular 20-compatible standalone application
- TypeScript 5.8
- Bootstrap 5.3
- RxJS 7.8
- Angular Reactive Forms
- Angular functional guards and HTTP interceptors
- Nginx for the production container

### Testing

- xUnit
- FluentAssertions
- Moq
- Microsoft.NET.Test.Sdk
- Angular Karma/Jasmine tooling

## Local Setup

### Prerequisites

Install the following:

- .NET 8 SDK
- Node.js 22 or later
- npm
- MongoDB 8, or a reachable MongoDB instance
- Angular CLI, or use the local CLI installed by npm

### Clone and restore

```powershell
git clone <repository-url>
cd FlowTrack
```

Restore the .NET solution:

```powershell
dotnet restore .\FlowTrack.slnx
```

Install Angular dependencies:

```powershell
cd .\FlowTrack.Web
npm install
cd ..
```

### Configure the API

Keep secrets out of `appsettings*.json`. The API requires a JWT signing secret of at
least 32 UTF-8 bytes. For local development, store one in .NET user-secrets:

```powershell
dotnet user-secrets set "Jwt:Secret" "<a-private-random-secret-of-at-least-32-bytes>" --project .\FlowTrack.Api
```

MongoDB settings and JWT issuer/audience are configured in `FlowTrack.Api/appsettings.json`.
For non-local environments, inject `Jwt__Secret` from a secret manager or environment variable.

### Start MongoDB

Start MongoDB locally on its default port:

```powershell
mongod --dbpath <mongodb-data-directory>
```

Alternatively, use the Docker Compose setup described below.

### Start the API

```powershell
dotnet run --project .\FlowTrack.Api\FlowTrack.Api.csproj
```

The API uses the launch profile configured in the project. Swagger is enabled in the Development environment.

### Start the Angular application

In a second terminal:

```powershell
cd .\FlowTrack.Web
npm start
```

The Angular development server is normally available at:

```text
http://localhost:4200
```

The Angular application calls the API using `/api` routes. Configure a development proxy if the Angular development server and API run on different origins.

## Docker Setup

### Configure environment variables

Copy the example environment file:

```powershell
Copy-Item .env.example .env
```

Set a secure local JWT secret in `.env`:

```dotenv
JWT_SECRET=replace-this-with-a-secure-development-secret-at-least-32-bytes
```

For anything beyond local development, use a unique secret stored in a secure secret manager.

### Start the stack

```powershell
docker compose up --build
```

The Compose stack contains:

- `mongodb`: MongoDB database with a persistent named volume
- `api`: ASP.NET Core 8 API
- `web`: Angular production build served by Nginx

All services share the `flowtrack-network` bridge network. The API connects to MongoDB using the service hostname `mongodb`, and Nginx proxies browser `/api` requests to the `api` service.

### Stop the stack

```powershell
docker compose down
```

To remove the persisted MongoDB data as well:

```powershell
docker compose down -v
```

### Docker URLs

| Service | URL |
|---|---|
| Angular web application | http://localhost:4200 |
| ASP.NET Core API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| MongoDB | mongodb://localhost:27017 |

### Compose environment variables

| Variable | Default | Description |
|---|---:|---|
| `WEB_PORT` | `4200` | Host port for the Angular/Nginx container |
| `API_PORT` | `8080` | Host port for the API container |
| `MONGO_PORT` | `27017` | Host port for MongoDB |
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET Core environment |
| `MONGO_DATABASE` | `flowtrack` | MongoDB database name |
| `MONGO_USERS_COLLECTION` | `users` | User collection name |
| `MONGO_TASKS_COLLECTION` | `tasks` | Task collection name |
| `JWT_SECRET` | Required | JWT signing secret, at least 32 UTF-8 bytes |
| `JWT_ISSUER` | `FlowTrack.Api` | JWT issuer |
| `JWT_AUDIENCE` | `FlowTrack.Client` | JWT audience |

## Demo Credentials

When the API runs in the Development environment, it seeds a demo account and a sample
task into MongoDB. Seeding is idempotent and can be disabled with `Demo__Enabled=false`.
The development-only credentials are:

| Email | Password |
|---|---|
| `demo@flowtrack.local` | `FlowTrackDemo1` |

Do not use the development demo credentials or development environment settings in a
deployed environment. Users can also register from the Angular registration page or
`POST /api/auth/register`.

Example registration request:

```http
POST /api/auth/register
Content-Type: application/json
```

```json
{
  "userName": "alex",
  "email": "alex@example.com",
  "password": "StrongPass1"
}
```

The password must contain at least:

- 8 characters
- One uppercase letter
- One lowercase letter
- One number

After registration, authenticate with:

```json
{
  "email": "alex@example.com",
  "password": "StrongPass1"
}
```

## Testing

### Build the .NET solution

```powershell
dotnet build .\FlowTrack.slnx
```

### Run backend unit tests

```powershell
dotnet test .\FlowTrack.UnitTests\FlowTrack.UnitTests.csproj
```

The unit-test project covers:

- Domain entity validation and business rules
- User registration behavior
- Login behavior
- JWT generation and claims
- Create, update, and delete task use cases
- MongoDB infrastructure configuration and BSON mappings

The integration-test project starts the real ASP.NET Core host and uses isolated test
repositories to exercise registration, login, task CRUD, search, authentication, and
cross-user ownership enforcement without requiring a running MongoDB server.
MongoDB driver query execution against a live server remains an environment-level
integration check.

### Run all .NET tests

```powershell
dotnet test .\FlowTrack.slnx
```

### Run Angular tests

```powershell
cd .\FlowTrack.Web
npm test
```

### Build the Angular application

```powershell
npm run build
```

## API Documentation

Swagger/OpenAPI documentation is available when the API runs in the Development environment:

```text
http://localhost:8080/swagger
```

The raw OpenAPI document is available at:

```text
http://localhost:8080/swagger/v1/swagger.json
```

### Authentication in Swagger

1. Register a user through `POST /api/auth/register`.
2. Log in through `POST /api/auth/login`.
3. Copy the returned `accessToken`.
4. Select **Authorize** in Swagger UI.
5. Enter:

```text
Bearer <accessToken>
```

6. Execute the protected task endpoints.

### API endpoints

#### Authentication

| Method | Route | Description | Auth required |
|---|---|---|---|
| `POST` | `/api/auth/register` | Register a user | No |
| `POST` | `/api/auth/login` | Authenticate and issue a JWT | No |

#### Tasks

| Method | Route | Description | Auth required |
|---|---|---|---|
| `GET` | `/api/tasks` | Get a paginated task list | Yes |
| `GET` | `/api/tasks/{id}` | Get a task by ID | Yes |
| `POST` | `/api/tasks` | Create a task | Yes |
| `PUT` | `/api/tasks/{id}` | Update a task | Yes |
| `DELETE` | `/api/tasks/{id}` | Delete a task | Yes |

Task list query parameters include page number, page size, status, sort field, sort direction,
and optional title/description search. Search text is escaped before it is used in a MongoDB
regex. Task list, read, update, and delete operations are restricted to the authenticated user;
requests for another user's task return `404`.

The API returns RFC 7807-compatible `ProblemDetails` responses for validation, authorization, and unexpected internal errors.

## User Story and Interview Walk-through

> As a signed-in user, I want to create, find, update, complete, and remove my tasks so
> that I can manage my own work without exposing it to other users.

The Domain project owns task invariants and status transitions. Application use cases
coordinate workflows through repository interfaces. Infrastructure implements those
interfaces with MongoDB and BCrypt. The API validates HTTP input, authenticates JWTs, and
passes the caller's user ID into each task operation. MongoDB filters enforce ownership
for reads and writes; the Angular client provides registration/login and task management.

For a concise demonstration:

1. Start Docker Compose and sign in with the demo credentials above.
2. Show the dashboard and task list, filter by status or search text, create and edit a task,
   then delete one.
3. Use Swagger to show the unauthenticated registration/login endpoints, then execute a
   protected task request with the returned bearer token.
4. Explain the integration test proving that a second user cannot read, update, or delete
   the first user's task.
5. Show the layered project references, a domain/application unit test, the API integration
   test, and Angular service/component tests.

## Generative AI Prompt and Evaluation

The complete prompt collection and evaluation appendix are organized by project area in
[`docs/genai/`](./docs/genai/):

- [Solution architecture](./docs/genai/01-Solution-Architecture.md)
- [Domain design](./docs/genai/02-Domain-Design.md)
- [Data access](./docs/genai/03-Data-Access.md)
- [Authentication](./docs/genai/04-Authentication.md)
- [Application layer](./docs/genai/05-Application-Layer.md)
- [API layer](./docs/genai/06-Api-Layer.md)
- [Testing](./docs/genai/07-Testing.md)
- [Angular frontend](./docs/genai/08-Angular-Frontend.md)
- [DevOps and AI usage report](./docs/genai/09-DevOps.md)

### Example prompt

> Build a RESTful task-management API in ASP.NET Core 8 with a MongoDB data layer and a
> separate Angular client. Use Clean Architecture with Domain, Application, Infrastructure,
> and API projects. Do not use Entity Framework, Dapper, or Mediator. A task has an ID,
> title, description, status, due date, owner user ID, and creation timestamp. Implement
> registration and login with BCrypt password hashing and JWT bearer authentication.
> Require authentication for all task endpoints and enforce that users can only list, read,
> update, or delete their own tasks in the repository query itself. Add validation for title,
> description, status transitions, pagination, and due dates; use MongoDB filters and escape
> user search text. Return appropriate HTTP status codes and ProblemDetails. Include unit
> tests for domain rules and use cases, hosted API integration tests for auth, CRUD, search,
> and cross-user access, Angular tests for task search and delete, a development-only seeded
> demo account, and setup documentation. Do not claim tests pass unless you run them and
> report the exact commands and results.

### Representative implementation

The API derives the user ID from validated claims rather than accepting an owner ID from
the request. The repository applies the owner filter to the database query:

```csharp
[Authorize]
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
{
    var task = await getTaskByIdUseCase.ExecuteAsync(id, GetUserId(), cancellationToken);
    return task is null ? NotFound() : Ok(ToResponse(task));
}
```

### How the output was validated and improved

- Reviewed dependency direction and confirmed the Domain project has no framework or
  storage dependency; checked that no EF Core, Dapper, or Mediator packages are used.
- Built and tested the solution, then added a hosted-API test for login, CRUD, search,
  anonymous rejection, and cross-user isolation.
- Compared the client query parameters to the API contract and implemented search end to
  end; added a delete service, confirmation, loading/error handling, and tests.
- Reviewed MongoDB filters for ownership on every task read/write, and used
  `Regex.Escape` so free-text searches cannot alter the regex pattern.
- Covered edge cases including invalid/expired tokens, missing tasks, another user's task,
  duplicate registration, invalid task transitions, excessive page sizes, and past due dates.
- For deployment, replace demo credentials, use a managed secret for `Jwt__Secret`, and
  run persistence-level integration tests against the target MongoDB version.
