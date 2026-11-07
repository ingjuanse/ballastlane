# 06. API Layer

## Prompt 11 - Authentication Controller

**Role:** You are a Senior ASP.NET Core API Architect.

**Objective:** Create the authentication endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`

Add input validation, OpenAPI documentation, standardized responses, and correct HTTP status codes. Deliver the controller, DTOs, exception handling, and Swagger documentation.

## Prompt 12 - Task Controller

**Role:** You are a REST API specialist.

**Objective:** Design and implement task endpoints:

- `GET /api/tasks`
- `GET /api/tasks/{id}`
- `POST /api/tasks`
- `PUT /api/tasks/{id}`
- `DELETE /api/tasks/{id}`

Require JWT authorization. Derive the owner from claims and enforce ownership at both the application and persistence boundaries. Add model validation, endpoint documentation, appropriate response codes, and error handling.

## Prompt 13 - Global Error Handling

**Role:** You are a .NET Platform Engineer.

**Objective:** Implement centralized exception handling for validation, authentication, business-rule, and unexpected errors.

Deliver middleware, ProblemDetails responses, logging recommendations, and tests. Do not expose stack traces, secrets, or internal exception details to clients.
