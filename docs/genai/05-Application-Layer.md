# 05. Application Layer

## Prompt 07 - Create Task Use Case

**Role:** You are a Clean Architecture consultant.

**Objective:** Implement the Create Task use case.

**Business rules:** Title and due date are mandatory; the task must belong to an authenticated user; status must be valid.

**Deliverables:** Input/output DTOs, validation logic, use-case implementation, and unit tests.

Keep application rules independent of infrastructure. Derive the owner from the authenticated identity rather than trusting a client-supplied user ID.

## Prompt 08 - Task Query Use Cases

**Role:** You are a Senior Backend Engineer.

**Objective:** Implement task retrieval functionality.

**Requirements:** Retrieve a user's tasks and a task by ID; support filtering, sorting, literal text search, and bounded pagination.

**Deliverables:** Use cases, DTOs, mapping strategy, error handling, and unit tests.

Prioritize performance and ensure the repository query is scoped to the requesting user.

## Prompt 09 - Update Task Use Case

**Role:** You are a Senior Application Architect.

**Objective:** Implement task update functionality.

**Requirements:** Verify the task belongs to the caller, validate the requested values and status transition, and preserve audit-friendly fields.

**Deliverables:** Use-case implementation, validation logic, unit tests, and error-handling strategy.

## Prompt 10 - Delete Task Use Case

**Role:** You are a Senior Backend Engineer.

**Objective:** Implement task deletion functionality.

**Requirements:** Validate identifiers, verify ownership and existence, and return meaningful outcomes.

**Deliverables:** Use-case implementation, validation rules, unit tests, and error-handling strategy.
