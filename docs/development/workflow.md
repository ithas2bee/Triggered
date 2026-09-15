# Development Workflow

## Delivery flow

```text
Issue/Epic -> User Story -> Copilot Agent -> Feature Branch
  -> Implementation -> Automated Validation -> Pull Request
  -> Review -> development -> staging -> main
```

`development` is the active integration branch, `staging` is the release
candidate and testing branch, and `main` is production-ready. Feature branches
should contain focused work for an individual story. Unfinished work must not
be merged directly into `main`.

## Agent workflow

Agents read `AGENTS.md`, the repository Copilot instructions, and relevant
product, architecture, and decision documents before implementation. They
implement only the story's scope, add appropriate tests, run available
validation, review the full diff, and report exactly what changed.

Agents are implementation agents, not independent architects. Missing major
architectural decisions must be identified rather than silently invented.

## Testing and validation

Behavior changes should include focused tests consistent with the affected
area. Before submitting a pull request:

- Run the relevant existing tests and validation.
- Review the complete diff for accidental scope expansion.
- Confirm that dependency direction and product boundaries remain intact.
- Report commands run and their results.

Documentation-only changes do not require application builds when no
documentation test exists, but they must still be checked for links,
consistency, and unintended code changes.

