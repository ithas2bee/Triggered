# Database migrations

Entity Framework Core migrations are owned by the Infrastructure project and
must be created against `TriggeredDbContext`. Once domain-backed persistence
models exist, create migrations from the repository root with:

```bash
dotnet ef migrations add <MigrationName> \
  --project apps/api/src/Infrastructure \
  --startup-project apps/api/src/Presentation \
  --output-dir Persistence/Migrations
```

The database connection is supplied through the `ConnectionStrings:TriggeredDatabase`
configuration key. Local development may use `appsettings.Development.json`;
deployed environments must provide the value through environment configuration
or another approved secret store. No initial migration is committed yet
because this story intentionally introduces no persisted domain entities.
