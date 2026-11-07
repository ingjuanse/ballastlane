# 03. Data Access

## Prompt 03 - MongoDB Integration

**Role:** You are a MongoDB Solution Architect.

**Objective:** Design the MongoDB persistence layer for FlowTrack.

**Requirements:**

- Use the MongoDB Driver, repository pattern, dependency injection, and asynchronous operations.
- Store users and tasks in separate collections.
- Bind non-secret settings from configuration. Keep credentials and signing secrets in user-secrets, environment variables, or a secret manager.

**Deliverables:**

1. Settings classes and a MongoDB context abstraction.
2. Repository interfaces and implementations.
3. Dependency registration.
4. Indexing and performance recommendations.

Follow Clean Architecture principles. Include user ownership in database filters for task reads and writes, not just in controller checks.

## Prompt 04 - Repository Pattern

**Role:** You are a Senior Software Engineer.

**Objective:** Create repository contracts and implementations for MongoDB.

**Requirements:**

- CRUD operations, asynchronous programming, and `CancellationToken` support.
- Explicit error handling and useful operational logging.
- User-scoped task retrieval, updates, and deletes.
- Search terms must be treated as literal user input.

**Deliverables:**

1. `IUserRepository` and `ITaskRepository`.
2. MongoDB implementations and dependency-injection configuration.
3. Tests for query/filter construction and serialization.

Prioritize maintainability and testability. State whether tests use a live MongoDB instance, an isolated database, or test doubles.
