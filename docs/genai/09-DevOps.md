# 09. Deployment and Documentation

## Prompt 19 - Dockerization

**Role:** You are a DevOps Engineer.

**Objective:** Containerize the ASP.NET Core API, MongoDB, and Angular application.

Deliver Dockerfiles, `docker-compose.yml`, environment configuration, network setup, and deployment instructions. Use health checks and persistent data volumes. Keep signing keys and production credentials out of the image and source repository.

## Prompt 20 - Technical Documentation

**Role:** You are a Senior Technical Writer.

**Objective:** Create professional FlowTrack documentation for technical reviewers and software architects.

Include project overview, user story, architecture, technologies, setup instructions, API documentation, testing strategy, security decisions, demo-data instructions, and deployment guidance. Ensure claims match the code and commands that were actually verified.

## Prompt 21 - Generative AI Usage Report

**Role:** You are a Software Engineering Consultant.

**Objective:** Create a technical appendix describing the use of Generative AI during FlowTrack implementation.

Include:

1. Objectives of AI usage.
2. Prompts used and generated artifacts.
3. Validation process and corrections performed.
4. Architectural decisions retained and rejected.
5. Security validations.
6. Lessons learned.

Clearly distinguish reusable prompt templates from a verbatim prompt-history transcript. Do not claim that a prompt or generated artifact was used or validated if it was not.

## FlowTrack validation summary

- **Objectives:** Use AI as an implementation and review aid while keeping architecture, security, and behavior subject to developer review.
- **Artifacts:** The solution includes the Domain, Application, Infrastructure, API, Angular, and test projects. The prompt templates in this folder are organized by those areas.
- **Validation:** `dotnet test FlowTrack.slnx` runs unit and hosted-API tests. `dotnet build FlowTrack.slnx -c Release` checks the .NET solution. From `FlowTrack.Web`, `npm test -- --watch=false --browsers=ChromeHeadless` and `npm run build` validate the client.
- **Corrections made:** Task API authorization and repository-level owner filters were added; search was connected end to end; client-side delete behavior and tests were added; a development-only seed and hosted API tests replaced the prior empty integration smoke test.
- **Decisions retained:** Layered projects, use-case classes, repository abstractions, MongoDB.Driver, BCrypt, JWT bearer authentication, and Angular standalone components.
- **Security checks:** No JWT signing key is committed in appsettings files. API tests check anonymous and expired-token rejection, and ensure another user cannot read, update, or delete a task. MongoDB filter tests verify owner scoping and escaped search patterns.
- **Limitations and lessons:** API hosted tests use isolated repositories; MongoDB query definitions and BSON mappings are unit tested, but running persistence tests against a live MongoDB service is still recommended before deployment. AI output must be checked against actual dependency direction, endpoint behavior, and test results rather than accepted based on plausible code.
