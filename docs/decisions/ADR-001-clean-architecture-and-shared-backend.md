# ADR-001: Clean Architecture and Shared Backend

- **Status:** Accepted
- **Date:** 2026-09-15

## Context

Triggered has mobile and web clients that must provide a consistent product
while the domain and analysis rules evolve. Business rules should not be
coupled to HTTP, UI frameworks, persistence, or a particular client.

## Decision

Use Clean Architecture for the backend with Domain, Application,
Infrastructure, and Presentation layers. Dependencies point toward Domain:

```text
Presentation -> Application -> Domain
Infrastructure -> Application -> Domain
```

Mobile and web communicate with the same ASP.NET Core API and database. Each
client separates presentation, application behavior, and infrastructure/API
communication.

## Consequences

Core rules can evolve and be tested independently of frameworks and clients.
Controllers and UI remain thin, and clients cannot drift into separate
implementations of the domain. The architecture requires explicit boundaries
and may involve more projects or interfaces than a tightly coupled prototype.

