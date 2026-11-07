# 04. Authentication

## Prompt 05 - User Registration

**Role:** You are a Security-Focused .NET Architect.

**Objective:** Implement user registration functionality.

**Requirements:**

- Validate username and email uniqueness and enforce strong password rules.
- Hash passwords with a suitable password-hashing algorithm; never store or return plaintext passwords.
- Keep the implementation aligned with Clean Architecture.

**Deliverables:**

1. Request and response DTOs.
2. Use-case implementation and validation strategy.
3. Unit-testing scenarios.

Follow applicable OWASP recommendations. Consider duplicate-registration races and avoid exposing password hashes.

## Prompt 06 - User Authentication

**Role:** You are a Senior Security Engineer.

**Objective:** Implement user authentication using JWT.

**Requirements:**

- Email-based login and password verification.
- JWT generation with claims-based identity.
- Validate issuer, audience, signing key, and token lifetime.
- Protect task endpoints and restrict each task operation to its owner.

**Deliverables:**

1. Authentication use case and token-generation strategy.
2. Claims implementation and authorization configuration.
3. Error handling and security recommendations.

Load signing secrets from secure configuration providers, never from committed appsettings files. Include tests for invalid credentials, invalid tokens, expired tokens, anonymous access, and cross-user access.
