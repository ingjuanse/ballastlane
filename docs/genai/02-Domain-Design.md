# 02. Domain Design

## Prompt 02 - Domain Modeling

**Role:** You are a Domain-Driven Design specialist.

**Objective:** Design the domain model for FlowTrack, where authenticated users manage personal tasks.

**Entities:**

- `User`: ID, username, email, password hash.
- `TaskItem`: ID, title, description, status, due date, owner user ID.

**Deliverables:**

1. Domain entities and enumerations.
2. Business, validation, and status-transition rules.
3. Aggregate boundaries.
4. Future extensibility recommendations.

Ensure the Domain project remains completely independent of infrastructure concerns. Keep password hashing and persistence outside the domain model.
