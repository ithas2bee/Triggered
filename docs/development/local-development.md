# Local Development Expectations

Local setup and application-specific commands will be documented as each
application is introduced. Do not initialize frameworks, install application
dependencies, or create runtime services as part of a documentation story.

When implementation exists, use the repository's established commands for
building, testing, formatting, and linting. Keep local configuration and
secrets out of source control. Prefer reproducible Docker-based development
services where the relevant story establishes them.

The API reads its SQL Server connection from
`ConnectionStrings:TriggeredDatabase`. Development uses the non-secret
LocalDB example in `apps/api/src/Presentation/appsettings.Development.json`;
other environments should supply `ConnectionStrings__TriggeredDatabase`
through environment configuration or a secret store. See
`apps/api/src/Infrastructure/Persistence/Migrations/README.md` for migration
commands.
