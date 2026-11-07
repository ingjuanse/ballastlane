# 07. Testing

## Prompt 14 - Domain Tests

**Role:** You are a Test Automation Architect.

**Objective:** Create comprehensive xUnit and FluentAssertions tests for domain entities.

**Coverage:** User validation, task validation, task status/business rules, and invalid transitions.

Deliver a test suite, naming strategy, and coverage recommendations. Keep these tests deterministic and independent of databases and network services.

## Prompt 15 - Application Tests

**Role:** You are a Senior QA Engineer.

**Objective:** Create unit tests for application use cases using xUnit, Moq, and FluentAssertions.

**Coverage:** Create, query, update, and delete tasks; register users; log in users; invalid input; missing resources; repository failures; and owner scoping.

Follow Arrange-Act-Assert. Verify both returned outcomes and whether dependencies were called with the correct user scope.

## Validation checklist for generated tests

- Confirm tests fail for the corresponding defect before accepting a fix.
- Avoid tests that only assert constants or `true`.
- Separate use-case tests from hosted API tests and persistence-adapter tests.
- Report the exact commands, frameworks, and results; do not claim unrun coverage.
