# System Architecture

Triggered uses Clean Architecture to keep business concepts independent from
frameworks, databases, clients, and external services.

## System shape

```text
Mobile (React Native + Expo) \
                              -> Triggered API -> Domain/Application/Infrastructure -> SQL Server
Web (React + Next.js)        /
```

Mobile and web are two interfaces to the same system. They must not implement
separate versions of core business rules.

## Backend layers

```text
Presentation -> Application -> Domain
Infrastructure -> Application -> Domain
```

### Domain

Owns Triggered's business concepts and rules. It must not know about ASP.NET
Core, EF Core, SQL Server, HTTP, React, React Native, Docker, or external
services.

### Application

Coordinates application use cases and orchestration. It defines the
application-facing behavior while keeping technical concerns out of business
rules.

### Infrastructure

Implements persistence, EF Core, SQL Server access, authentication
infrastructure, and other external integrations. It is not a place for
arbitrary business logic.

### Presentation

Exposes application capabilities through the ASP.NET Core HTTP/API boundary.
Controllers and endpoints should translate requests and responses, delegate to
application behavior, and remain thin.

## Frontend boundaries

Web and mobile maintain the same conceptual separation:

```text
Presentation -> Application -> Infrastructure
```

Presentation owns UI, Application owns client-side use-case behavior, and
Infrastructure communicates with the API or other external systems. UI code
must not absorb business rules that belong in the backend domain/application
layers.

## Technology direction

The approved direction is C#, ASP.NET Core, Entity Framework Core, Microsoft
SQL Server, REST/JSON, TypeScript, React, Next.js, React Native, Expo, Docker,
and Ubuntu Server. Authentication is expected to use OAuth/OIDC; the provider
and implementation remain undecided.

