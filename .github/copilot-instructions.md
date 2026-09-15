# Copilot Instructions

Triggered is the canonical project name for the application, repository,
backend API, web app, mobile app, and documentation. Read the repository-level
[`AGENTS.md`](../AGENTS.md) first; it is the authoritative implementation
guidance. These instructions supplement it.

## Before changing code

Read the relevant product and architecture documents, then confirm that the
requested change belongs to the MVP. Keep changes focused on the user story.
Preserve the US-001 directory structure and existing architectural boundaries.

## Important product boundary

Triggered records a user's food, time, and reported reaction observations and
describes patterns in that user's history. It does not diagnose conditions,
determine medical safety, prescribe treatment, or provide medical advice.
Never turn a personal pattern or trigger level into a medical claim.

## Technical expectations

Keep backend dependencies directed from Presentation and Infrastructure through
Application to Domain. Keep controllers and endpoints thin. Keep web and
mobile UI separate from use-case behavior and API communication. Do not
duplicate core business rules across clients.

Use the approved technology direction documented in
[`docs/architecture/system-overview.md`](../docs/architecture/system-overview.md).
Do not select an authentication provider or invent an analysis algorithm
without an explicit decision or story.

## Delivery expectations

Write focused, maintainable code and appropriate tests. Run the repository's
available validation, inspect the full diff, and report validation results.
Do not expand scope into excluded or future features. If a major architectural
choice is missing, call it out rather than silently making it.

