# Triggered Agent Guidance

## Project

Triggered is a personal food-trigger tracking application. It helps a person
record food, time, and reaction observations, then identify patterns in their
own history. It is not a medical diagnostic system and must not present
observations as medical diagnoses, safety determinations, treatment, or
professional advice.

The MVP is limited to food-trigger tracking:

- Record foods, the time they were consumed, and reported reactions.
- Store and review observations.
- Accumulate history and identify personal response patterns.
- Present an understandable trigger assessment, including **Insufficient
  Data** when evidence is limited.

The conceptual levels are Insufficient Data, Low, Possible Trigger, Moderate,
Likely Trigger, and Strong Trigger Pattern. They describe observed personal
patterns, not medical risk. The analysis algorithm is not yet defined; do not
invent one in documentation or implementation.

Weight, calories, exercise, sleep, medication, wearables, medical records,
provider portals, diagnosis, recommendations, meal planning, nutrition
coaching, social/community features, and restaurant or grocery integrations
are outside the MVP. Future contextual analysis, ingredient correlation, and
AI-assisted insights must wait for explicit stories.

## Architecture and boundaries

US-001 established the repository structure. Preserve it:

```text
apps/api/{src/{Domain,Application,Infrastructure,Presentation},tests}
apps/web/{src/{presentation,application,infrastructure},tests}
apps/mobile/{src/{presentation,application,infrastructure},tests}
packages/{domain,application,api-client,ui,shared}
tests/{integration,end-to-end}
docs/{product,architecture,development,decisions}
docker/{development,staging,production}
.github/workflows
```

The backend follows Clean Architecture:

```text
Presentation -> Application -> Domain
Infrastructure -> Application -> Domain
```

Domain contains technology-independent business concepts and rules. It must
not depend on ASP.NET Core, Entity Framework Core, SQL Server, HTTP, React,
React Native, Docker, or external services. Application coordinates use cases.
Infrastructure implements persistence and other technical integrations.
Presentation exposes the HTTP/API boundary; controllers and endpoints stay
thin and contain no business rules.

Web and mobile use TypeScript, React, Next.js, React Native, and Expo as
appropriate. They share the backend and database, and each keeps presentation,
application behavior, and infrastructure/API communication separate. Core
business rules must not be duplicated between clients.

The approved technology direction is C#, ASP.NET Core, EF Core, SQL Server,
REST/JSON, TypeScript, React, Next.js, React Native, Expo, Docker, and Ubuntu
Server. Authentication is expected to use OAuth/OIDC, but no provider or
implementation has been selected.

## Implementation rules

- Read this file, `.github/copilot-instructions.md`, and relevant `docs/`
  files before changing code.
- Implement only the requested user-story scope.
- Prefer small focused classes and methods, clear names, low coupling, high
  cohesion, dependency inversion, testability, and maintainability.
- Avoid duplicated business logic, giant classes or methods, and abstractions
  that do not provide a meaningful benefit.
- Do not add generic `Helpers`, `Utilities`, or `Misc` folders without an
  explicit architectural decision.
- If a story requires an architectural decision that is not documented,
  identify the gap instead of silently choosing a major direction.
- Add or update appropriate tests for behavior changes. Do not remove unrelated
  tests.
- Do not add functionality that is merely a possible future feature.

## Agent workflow

For each story:

1. Read the applicable guidance and documentation.
2. Understand the story and its acceptance criteria.
3. Implement only the requested scope.
4. Add appropriate tests.
5. Run available validation and review the complete diff.
6. Report exactly what changed, what was validated, and any unresolved
   architectural questions.

Do not initialize applications, add dependencies, or redesign the repository
unless a story explicitly requires it.

