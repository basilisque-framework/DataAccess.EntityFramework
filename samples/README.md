# Entity Framework demo

A small **Blazor Web App (Interactive Server)** that serves as both an
application template and a feature laboratory.

**Local development only. Use a dedicated demo database.** The application
automatically applies its migrations at startup, allows physical deletion and
can clear all demo rows and audit events. It has no authentication or
authorization. The Alice/Bob selector is a demonstration of scoped user context,
not an identity system. Keep the server bound to loopback.

## Quick start

From the repository root, using a .NET 10 SDK or newer:

```powershell
dotnet run --project .\samples\Demo.Web\Demo.Web.csproj -- --urls http://localhost:5187
```

Open <http://localhost:5187>. SQLite is the default and needs no external server.
No environment variable or launch profile is required: the host explicitly enables
static web assets so Blazor also works with `dotnet run` in the default
`Production` environment. This environment name does not make the demo suitable
for production use.
A welcome document is seeded into an empty demo database with Alice's stamps.
The default SQLite file is `basilisque-demo.db`, relative to the process working
directory. For a deterministic location, set an absolute path:

```powershell
$env:Database__ConnectionStrings__SQLite = 'Data Source=C:\Temp\basilisque-demo.db'
dotnet run --project .\samples\Demo.Web\Demo.Web.csproj -- --urls http://localhost:5187
```

The parent directory must already exist. Local SQLite files under this sample
directory are ignored by Git.

Open [Basilisque.DataAccess.EntityFramework.Demo.slnf](Basilisque.DataAccess.EntityFramework.Demo.slnf)
to work with the demo and its referenced library projects independently of the
library tests. This is a filter of the shared
[root solution](../Basilisque.DataAccess.EntityFramework.slnx), which builds and
tests all library and demo projects in CI. Demo projects are not packable.

## Try the features

1. As **Alice**, enter a title and content, then create a document.
2. Switch to **Bob**, change the title and save the selected document.
   Creation stamps remain Alice's; modification stamps change to Bob.
3. Soft-delete the selected document. It disappears from **Active**. Choose
   **Deleted**, select it again, and inspect the last operation's before/after
   deletion values.
4. Restore it. The deletion time and actor are cleared. Select **All** to see it.
5. Confirm physical deletion and delete the selected document permanently.
   The row disappears, but its key, time and actor remain in the audit table.
6. Repeat with **No user**. Timestamps still work; new non-nullable Guid stamps
   remain `Guid.Empty`, existing user stamps are preserved, and deletion actors
   can be null.
7. Use **Clear demo data** only after confirming. This intentionally uses
   transactional bulk deletion, bypassing per-row interceptors, to remove both
   demo documents and demo audit events. It never drops the database or its
   migration history. Restarting an entirely empty demo seeds a welcome row again.

The document table shows creation, modification and deletion times in UTC alongside
the respective users. The last successful operation shows the exact persisted stamp values before and
after each action. SQL commands appear in the application console; sensitive-data
logging is not enabled. Database-operation errors are logged and displayed as a
failure, not silently replaced by empty results.

## Providers

All three providers support the demonstrated shared features. Select one
**before startup** and restart to switch. Do not change providers within an
existing Blazor circuit or reuse a provider's database/migrations for another.

### SQLite

```powershell
$env:Database__Provider = 'SQLite'
dotnet run --project .\samples\Demo.Web\Demo.Web.csproj -- --urls http://localhost:5187
```

### SQL Server

The default connection targets Windows LocalDB (`MSSQLLocalDB`), if installed.
Otherwise supply your own dedicated SQL Server demo connection:

```powershell
$env:Database__Provider = 'SqlServer'
$env:Database__ConnectionStrings__SqlServer = 'Server=localhost;Database=BasilisqueEfDemo;Integrated Security=true;TrustServerCertificate=true'
dotnet run --project .\samples\Demo.Web\Demo.Web.csproj -- --urls http://localhost:5187
```

`TrustServerCertificate` is only a local-development convenience. SQL Server
temporal tables are **not enabled** in this first demo; the hard-delete audit is
an independent relational feature.

### PostgreSQL

```powershell
$env:Database__Provider = 'PostgreSQL'
$env:Database__ConnectionStrings__PostgreSQL = 'Host=localhost;Database=basilisque_ef_demo;Username=postgres'
dotnet run --project .\samples\Demo.Web\Demo.Web.csproj -- --urls http://localhost:5187
```

Configure credentials locally through environment variables or .NET user secrets;
never commit passwords. For example:

```powershell
dotnet user-secrets set 'Database:ConnectionStrings:PostgreSQL' 'Host=localhost;Database=basilisque_ef_demo;Username=YOUR_USER;Password=YOUR_LOCAL_PASSWORD' --project .\samples\Demo.Web\Demo.Web.csproj
```

SQL Server/PostgreSQL require an accessible server and permissions to create or
migrate the demo database. No containers or services are started automatically.
If the selected provider cannot connect or migrate, startup fails explicitly;
there is no SQLite fallback.

Configuration defaults live in [appsettings.json](appsettings.json). The web
project links this shared file into its build/publish output. Runtime overrides
are applied in order: user secrets, environment variables, command-line arguments.
Connection strings use Basilisque's `Database:ConnectionStrings:<Provider>` shape,
not ASP.NET Core's unrelated root `ConnectionStrings` section.

## Structure and Basilisque integration

| Project | Responsibility |
|---|---|
| [Demo.Data](Demo.Data) | Entity, relational `BaseDbContext`, initializer and small application scenarios |
| [Demo.SQLite](Demo.SQLite) | SQLite dependency, generated design-time factory and SQLite migrations |
| [Demo.SqlServer](Demo.SqlServer) | SQL Server dependency, generated factory and SQL Server migrations |
| [Demo.PostgreSQL](Demo.PostgreSQL) | PostgreSQL dependency, generated factory and PostgreSQL migrations |
| [Demo.Web](Demo.Web) | Blazor UI, configuration and per-operation DI scope |
| [Demo.Tests](Demo.Tests) | SQLite behavior tests and offline model/migration checks for all providers |

- [Directory.Packages.props](Directory.Packages.props) imports the repository's
  existing central versions, `Basilisque.CommonFrameworkBuild`,
  `Basilisque.DependencyInjection`, and test conventions. The demo is not packable
  and does not inherit the library's package README/packaging setup. CommonBuild
  XML API-document generation is disabled for these application projects.
  CommonFrameworkBuild is appropriate here as a framework-owned sample; an
  unrelated consuming application need not adopt framework build settings.
- [Directory.Build.targets](Directory.Build.targets) references the repository's
  EF source generator as an analyzer. Provider projects explicitly import their
  provider build props because `ProjectReference` does not import NuGet build
  assets. This mirrors the automatic imports that installed provider packages
  supply to a normal consuming application.
- [DemoRegistration.cs](Demo.Web/Services/DemoRegistration.cs) calls the **web
  application's generated registration entry point**, including its dependency
  chain. DbContext registration, annotated scenario services, provider services,
  stamping, scoped Core user contexts and audit interceptors are not reimplemented.
- Each provider assembly gets a **generated `MigrationAssemblyProvider`**.
  Because the host references all three assemblies, it explicitly selects one
  after the generated registration chain. The library's provider configurators
  use this selection; no parallel `UseSqlite`/`UseSqlServer` configuration is added.
- [DemoDbInitializer.cs](Demo.Data/DemoDbInitializer.cs) derives from the library's
  relational `DbInitializer`. Its inherited `IDbInitializer` annotation supplies
  registration. Startup invokes the four lifecycle methods in order in a
  dedicated scope. The initializer API is synchronous; interactive UI actions
  use EF's asynchronous APIs.
- [DemoSession.cs](Demo.Web/Services/DemoSession.cs) creates an **async DI scope per
  operation**, sets `IWritableUserContext<Guid>` within that scope, then resolves
  the scenarios/DbContext from the same scope. It never holds a DbContext for the
  lifetime of a Blazor circuit and never shares a tracked context across users.
- [DocumentScenarios.cs](Demo.Data/DocumentScenarios.cs) contains ordinary,
  explicit EF operations. Read views disable only the named soft-delete filter.
  Client-side ordering of the audit events avoids SQLite's `DateTimeOffset`
  ordering limitation. This intentionally small local demo reads all demo rows;
  a production application should add paging.

The UI labels features by their library layer rather than duplicating Base and
Relational application projects. There is no repository abstraction hiding the
EF/library calls.

## Migration workflow

From the **samples directory**:

```powershell
Set-Location .\samples
dotnet tool restore
dotnet build .\Basilisque.DataAccess.EntityFramework.Demo.slnf
```

The pinned local `dotnet-ef` version matches the repository's EF Core version.
Each provider project is its own EF tooling startup project. Its generated
factory discovers the shared [appsettings.json](appsettings.json) from this
directory and reads environment overrides (not the web project's user secrets).

After changing the shared model, add a migration for **each** provider:

```powershell
$env:Database__Provider = 'SQLite'
dotnet ef migrations add DescribeYourChange --project .\Demo.SQLite --startup-project .\Demo.SQLite --output-dir Migrations

$env:Database__Provider = 'SqlServer'
dotnet ef migrations add DescribeYourChange --project .\Demo.SqlServer --startup-project .\Demo.SqlServer --output-dir Migrations

$env:Database__Provider = 'PostgreSQL'
dotnet ef migrations add DescribeYourChange --project .\Demo.PostgreSQL --startup-project .\Demo.PostgreSQL --output-dir Migrations
```

These commands generate migrations without needing a live database. Use the same
provider selection to run `dotnet ef migrations script` or
`dotnet ef migrations has-pending-model-changes` for that project. Clear the
temporary provider override afterwards if returning to SQLite:

```powershell
Remove-Item Env:Database__Provider
```

Do not use `EnsureCreated` alongside this migration workflow. Automatic startup
migration is convenient for a single-process local demo, not a production
deployment strategy.

## Validation

From the repository root:

```powershell
dotnet build .\samples\Basilisque.DataAccess.EntityFramework.Demo.slnf
dotnet test --project .\samples\Demo.Tests\Demo.Tests.csproj
```

The TUnit tests use real migrations against isolated temporary SQLite files, not
EF InMemory. They check stamping, user-scope isolation, deletion/filter/restore
behavior, hard-delete audit, reset, validation and generated initializer/seeding.
For **all three providers**, offline tests also verify provider resolution,
migration assembly selection, absence of pending model changes and SQL script
generation. These offline checks do **not** replace live SQL Server/PostgreSQL
tests; use the UI walkthrough against your dedicated server databases too.
