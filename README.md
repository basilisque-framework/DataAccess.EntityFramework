<!--
   Copyright 2025 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
-->
# Basilisque - Data Access Entity Framework

## Overview
This project provides functionality for data access with Entity Framework Core.

[![NuGet Basilisque.DataAccess.EntityFramework.SqlServer](https://img.shields.io/badge/NuGet_Basilisque.DataAccess.EntityFramework.SqlServer-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.DataAccess.EntityFramework.SqlServer)  
[![NuGet Basilisque.DataAccess.EntityFramework.SQLite](https://img.shields.io/badge/NuGet_Basilisque.DataAccess.EntityFramework.SQLite-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.DataAccess.EntityFramework.SQLite)  
[![NuGet Basilisque.DataAccess.EntityFramework.PostgreSQL](https://img.shields.io/badge/NuGet_Basilisque.DataAccess.EntityFramework.PostgreSQL-latest-%23004880.svg?logo=nuget)](https://www.nuget.org/packages/Basilisque.DataAccess.EntityFramework.PostgreSQL)  
[![License](https://img.shields.io/badge/License-Apache%20License%202.0-%23D22128.svg?logo=apache&logoColor=%23D22128)](LICENSE.txt)
[![SonarCloud](https://img.shields.io/badge/SonarCloud-main-%23F3702A.svg?logo=sonarcloud&logoColor=%23F3702A)](https://sonarcloud.io/project/overview?id=basilisque-framework_DataAccess.EntityFramework)  

<!--## Description
This project contains functionality for registering services at the IServiceCollection.  
It also contains a source generator, that generates code to registers all services of the target project.  
The services simply have to be marked with an attribute.
The attribute can also be attached to base classes or interfaces. This means that all implementations of the base class or interface will be automatically registered.

The generated code is marked as partial. This means it can be easily extended with custom logic.

## Getting Started
Install the NuGet package [Basilisque.DataAccess](https://www.nuget.org/packages/Basilisque.DataAccess).  
Installing the package will also install the package Basilisque.DataAccess.CodeAnalysis as a child dependency. This contains the source generator.

Now you're ready to [register your first service](https://github.com/basilisque-framework/DataAccess/wiki/Getting-Started).


## Features
- Generates code that registers all marked services of a project.  
  This means anyone who uses your project can call a single method to register all of your provided services.

- The generated code automatically calls the registration methods of all dependencies that also use Basilisque.DataAccess.  
  This means if you use Basilisque.DataAccess in multiple related projects, you have to call the registration method only once and all dependencies will be registered, too.

- Configuration of the registration
  * Lifetime (Transient, Scoped, Singleton)
  * Type as that the service will be registered can be specified
  * ImplementsITypeName  
  When the service implements an interface with the same name but starting with an I, it will be registered as this interface (e.g. 'PersonService' as 'IPersonService').

- Marker attributes on interfaces and base classes.  
  This means for example, that you can register all implementations of an interface with the same configuration and you don't have to worry about registration whenever you create a new implementation of it.

- Custom marker attributes  
  You can create custom attributes with predefined configuration by inheriting from the provided attributes. Whenever you use your attribute you can be sure, that the same configuraiton will be used.

- Multiple registrations of the same service  
  A service can have multiple registrations with different configurations.

## Examples
All implementations of this interface will be registered as singleton:
```csharp
[RegisterServiceSingleton()]
public interface IMyInterface
{
}
```
You can get the same result writing it like this:
```csharp
[RegisterService(RegistrationScope.Singleton)]
public interface IMyInterface
{
}
```

For details see the __[wiki](https://github.com/basilisque-framework/DataAccess/wiki)__.

-->
## Interactive demo

The [sample application](samples/README.md) is a small Blazor Interactive Server
feature laboratory and application template for SQLite, SQL Server and PostgreSQL.
It demonstrates Basilisque dependency registration, migrations, scoped user
stamping, soft deletion, restore and hard-delete auditing without a separate
JavaScript framework. See the [demo solution filter](samples/Basilisque.DataAccess.EntityFramework.Demo.slnf)
for the complete example.

## Building and testing

The [root solution](Basilisque.DataAccess.EntityFramework.slnx) contains all library
and demo projects. Both GitHub build workflows use it to build and test the full
repository. Demo projects are not packable; NuGet packaging remains limited to
the library projects, and application publishing is disabled by default.

```powershell
dotnet build .\Basilisque.DataAccess.EntityFramework.slnx
dotnet test --solution .\Basilisque.DataAccess.EntityFramework.slnx
```

For focused development, open the
[library filter](src/Basilisque.DataAccess.EntityFramework.slnf) or
[demo filter](samples/Basilisque.DataAccess.EntityFramework.Demo.slnf).
Both reference the same root solution. The demo filter includes its required
library projects but excludes the library test projects.

## Entity stamping and dependency injection
`BaseDbContext<TDbContext>` automatically configures and runs entity stamping.
Entities implementing the interfaces in `Basilisque.DataAccess.EntityFramework.Base.Stamping`
can receive creation and modification timestamps and user IDs.

The generated dependency-registration chain registers the scoped stamping interceptor,
the timestamp handler, and a user-stamp dispatcher with closed handlers for `Guid`,
`string`, `int`, and `long`. It also registers the
Core user-context services through the library's dependencies; no separate Core
registration is required.

For example, register the base library and its dependencies with:

```csharp
Basilisque.DataAccess.EntityFramework.Base.IServiceCollectionExtensions.RegisterServices(services);
```

Applications normally use their own generated registration entry point, which
includes the base library through the dependency chain. Calling
`IDependencyRegistrator.RegisterServices(services)` directly only registers that
assembly's services, not its dependencies.

Resolve DbContexts from a dependency-injection scope. Set the current user through
`IWritableUserContext<Guid>` from that same scope before saving changes. When no user
is available, timestamps are still applied, but user stamp values are not changed.
The `IDbProviderServiceProvider` wrapper is transient so each context resolves the
interceptor and user context from its own scope. Singleton connection-string builders
only use the wrapper's configuration-section metadata.

Core provides user contexts through its open generic registrations. For the four
standard key types, the EF registration additionally supplies closed, typed factories
for the default Core context and its read/write proxies. They share the same scoped
`WritableUserContext<TKey>` instance; existing closed registrations are preserved.
If the open generic Core implementation has already been customized, its registration
is retained instead of adding a closed default. Later customizations for standard
key types should replace the corresponding closed registrations.

The dispatcher discovers the user-key types from configured CLR and shadow stamp
properties in the EF model and selects handlers from `IEnumerable<IUserStampHandler>`
in the current scope. It does not construct generic types dynamically.
For example, set the current string user in the context's scope:

```csharp
using Basilisque.Core.Auth;
using Microsoft.Extensions.DependencyInjection;

scope.ServiceProvider.GetRequiredService<IWritableUserContext<string>>().UserId = "user-123";
```

For another key type, explicitly register a closed handler under the non-generic
`IUserStampHandler` interface after the generated registration chain:

```csharp
using Basilisque.DataAccess.EntityFramework.Base.Stamping;

services.AddScoped<IUserStampHandler>(sp =>
    new UserStampHandler<MyUserId>(sp.GetRequiredService<IUserContext<MyUserId>>()));
```

The corresponding `IUserContext<MyUserId>` must also be available. Core's open generic
services support it in normal .NET deployments; Native AOT deployments must provide
statically reachable closed context/proxy registrations for custom key types.
Registering only `IUserStampHandler<MyUserId>` does not add it to the dispatcher's
non-generic handler collection.

Applications can replace a standard handler by adding an `IUserStampHandler` with
the same `UserKeyType` after the default registrations. The last registration for
each key type wins and is invoked once per save. If any configured user-key type has
no handler, saving throws an explicit exception identifying the missing key type
and the registration contract.
Registration of custom user-context implementations, proxies, or convenience
interfaces is the application's responsibility.

Design-time factories register the interceptor independently and do not require
application user-context services. No stamp handlers are registered by default at
design time, so explicitly supplied seed values are not overwritten by stamping.
The standard handler and generic Core context/proxy factories use statically closed
types. This removes dynamic generic construction from user-stamp dispatch, but does
not establish Native AOT or trimming compatibility for EF model creation, queries,
providers, or the complete library. Those deployments require separate publish and
execution validation.

## Soft delete
Soft deletion is opt-in. `ISoftDelete` is the default and provides
`DateTimeOffset? DeletedAt` and `Guid? DeletedBy`. Use `ISoftDelete<TKey>` for other
value-type user keys, such as `int` or `long`, and `ISoftDeleteOfRef<TKey>` for
reference-type keys, such as `string`. Both variants have nullable user keys.
Use `ISoftDeleteTimestamp` when only the deletion time is needed.

`DeletedAt` is the sole source of deletion state: null means active, non-null means
deleted. The interfaces provide a read-only, calculated `IsDeleted` convenience
property; no separate flag is persisted. Default interface members are accessed
through the interface unless the entity also declares its own calculated property.
In LINQ queries, use `DeletedAt`, not the calculated `IsDeleted` property.

```csharp
using Basilisque.DataAccess.EntityFramework.Base.SoftDelete;
using Basilisque.DataAccess.EntityFramework.Base.Stamping;

public class Document : ISoftDelete, IStampChanges
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
    public Guid ModifiedBy { get; set; }
}
```

`BaseDbContext<TDbContext>` configures soft deletion during model finalization, after
application model configuration. The existing save interceptor converts tracked
`Remove` and `RemoveRange` operations into updates of the deletion time. Creation
stamps and unrelated stored columns are preserved, including when deleting an
attached stub containing only its key.

The deletion time is functional state and is set independently of audit stamping.
The deleting user is populated by the existing stamp handlers and scoped Core user contexts.
`Guid`, `string`, `int`, and `long` work without extra handler registration; other
user-key types use the same explicit `IUserStampHandler` registration as ordinary
stamping. Nullable deletion keys use the underlying key type for dispatch and user
contexts: for example, `Guid? DeletedBy` uses `UserStampHandler<Guid>` and
`IUserContext<Guid>`, not a separate nullable-key context.
With modification stamps configured, soft deletion sets `ModifiedAt`
and `ModifiedBy` too, using the same timestamp/user as a `Remove` deletion. Without a
current user, the deletion still works and `DeletedBy` can remain null. Active rows
have null deletion time and user values. Repeating a
soft delete on an already deleted, loaded entity does not change its deletion audit.
Setting `DeletedAt` directly also soft-deletes an entity and preserves the supplied
time rather than replacing it with the save time.

Shadow properties are supported:

```csharp
modelBuilder.Entity<DocumentWithoutInterfaces>()
    .UseShadowSoftDelete<DocumentWithoutInterfaces, Guid>();
```

Use `UseShadowSoftDelete()` for the default nullable Guid user key,
`UseShadowSoftDeleteOfRef<TEntity, string>()` for a nullable string user key, or
`UseShadowSoftDeleteTimestamp()` for only the deletion time.
Ordinary DbContexts must call `modelBuilder.ApplySoftDeleteConfigurations()`
after their application model configuration and install the existing
`UseEFCoreStamping(serviceProvider)` interceptor.

### Querying deleted data
The named `Basilisque:SoftDelete` filter excludes deleted rows from LINQ queries.
Disable only this filter to keep application filters, such as tenant isolation:

```csharp
var includingDeleted = context.Set<Document>()
    .IgnoreQueryFilters([SoftDeleteModelBuilderExtensions.QueryFilterName]);
```

Existing named filters are preserved. Anonymous application filters are retained
under the reserved name `Basilisque:ApplicationQueryFilter`, because EF Core cannot
combine anonymous and named filters. `IgnoreQueryFilters()` without names disables
all filters, including tenant filters; use it with care.

Query filters are not an authorization boundary. Already tracked entities, `Local`,
and cached results from `Find` are not hidden by a filter. EF's normal required-
navigation/filter behavior also applies.

### Physical deletion and restore
To physically delete an opted-in entity, explicitly allow hard deletion around
the save operation:

```csharp
using (SoftDeleteSuppressor.AllowHardDelete())
{
    context.Remove(document);
    await context.SaveChangesAsync();
}
```

The bypass supports nested scopes and flows across awaits. It does not disable
query filters. `StampSuppressor.Suppress()` suppresses user and creation/modification
stamping, not soft deletion: the deletion time is still set and the row is retained
and hidden. Non-opted-in entities keep normal EF deletion
behavior.

To restore an entity, load it with the soft-delete filter disabled, set `DeletedAt`
to null, and save. `DeletedBy` is cleared automatically, including with stamping
suppressed. Modification stamps update unless suppressed. No deletion history is
retained; historical auditing is a separate concern.

### Relationships and limitations
Soft deletion does not cascade to related entities. Owned data is retained with
its owner. Tracked cascade deletions or foreign-key changes on other dependents
are rejected before saving rather than physically deleting, soft deleting, or
severing relationships implicitly. Configure tracked relationships with
`DeleteBehavior.ClientNoAction` and handle dependents explicitly. Explicitly disabling
`CascadeDeleteTiming` is respected; deferred cascades are validated without forcing
unrelated orphan processing. Unloaded dependents are not affected by the update.

Configure inheritance on the root entity. Owned types cannot independently opt in.
All writable CLR interface properties must be mapped. A calculated `IsDeleted`
property must not be mapped; explicitly ignore it if it was previously configured.
Custom user-key types may require an EF value converter. Add a migration for the new
nullable persisted properties.

Soft deletion intercepts tracked `SaveChanges` operations only. `ExecuteDelete`,
`ExecuteUpdate`, raw SQL, and external database writers bypass the interceptor and
its audit logic. Do not use those APIs for audited soft deletion without implementing
the equivalent updates explicitly.

Design-time contexts keep the model filter and removal conversion, including setting
the deletion time for `Remove`. Their default interceptor has no stamp handlers, so
user and creation/modification values are not automatically stamped. Explicitly
supplied deletion times are preserved; active entities have their deletion user
cleared. Native AOT/trimming compatibility
of model configuration and the EF provider still requires separate validation.

### Design-time service lifetimes
Each `BaseDesignTimeDbContextFactory<TDbContext>.CreateDbContext` call creates its own
service provider and scope. The returned context owns both until it is disposed;
subsequent calls and different factory instances do not share configuration or services.
Use `using` or `await using` for contexts created manually. Use `await using` if
their services require asynchronous disposal.

`BaseDbContext<TDbContext>` implements this ownership automatically. Derived contexts
that override `Dispose` or `DisposeAsync` must call the corresponding base method.
Contexts outside this hierarchy must implement `IDbContextDesignTimeLifetimeOwner`.
Its `SetDesignTimeServiceLifetime` method accepts an `IDesignTimeServiceLifetime`
which must be stored exactly once and detached before disposal. After disposing the
context itself, dispose that lifetime in a `finally` block, using `DisposeAsync`
in the asynchronous path. Detaching first prevents recursive disposal because the
service scope also tracks the context. Contexts without this contract are rejected
with an explicit exception; failed creation releases the newly created services.

## Hard-delete audit and temporal tables

Temporal history retains row versions, but a physical DELETE does not update the
last row's modification actor. Hard-delete auditing records that separate event in
an ordinary, non-temporal table. It does not modify or link to SQL Server period
columns. Application timestamps and temporal transaction timestamps need not match.

### Opt-in and configuration

Use the interfaces and builders from
`Basilisque.DataAccess.EntityFramework.Relational.HardDelete`.
No per-entity audit class is required:

```csharp
public class Document : IHardDeleteAudited
{
    public Guid Id { get; set; }
}
```

`IHardDeleteAudited` uses a nullable Guid actor. `IHardDeleteAudited<TUser>` selects
another non-nullable actor key type; the stored actor is always nullable.
The entity's primary key is inferred independently from the actor type.
Alternatively, enable auditing with the Fluent API, which also overrides interface
defaults:

```csharp
modelBuilder.Entity<Document>().UseHardDeleteAudit(audit => audit
    .ToTable("DocumentDeletions", "audit")
    .HasUserKey<Guid>());
```

With the SQL Server extensions in
`Basilisque.DataAccess.EntityFramework.SqlServer.HardDelete`, configure both features
in one table callback:

```csharp
modelBuilder.Entity<Document>().ToTable("Documents", table =>
{
    table.IsTemporalWithHardDeleteAudit(
        temporal => temporal.UseHistoryTable("DocumentsHistory"),
        audit => audit.ToTable("DocumentDeletions", "audit"));
});
```

`table.UseHardDeleteAudit(...)` can also accompany the standard `table.IsTemporal(...)`.
EF's temporal builder does not publicly expose the source entity builder, so these
entry points live on the table builder, without accessing private EF fields.
The audit feature itself does not require temporal tables.

Relational `BaseDbContext<TDbContext>` automatically installs the model convention
and ordered interceptors. Its generated dependency-registration chain supplies actor
providers for Guid, string, int, and long using the same Core user contexts as
stamping. Applications using ordinary DbContexts should register the Relational
library's generated services, call `optionsBuilder.UseHardDeleteAudit(scopedServices)`
(which already includes `UseEFCoreStamping`), and install
`configurationBuilder.AddHardDeleteAuditConvention()` in `ConfigureConventions`.
Alternatively, call `modelBuilder.ApplyHardDeleteAuditConfigurations()` at the end
of `OnModelCreating`.

For custom actor types register a scoped `IHardDeleteAuditUserProvider`, for example
`HardDeleteAuditUserProvider<MyUserKey>`, and configure an EF conversion if needed.
Missing providers fail explicitly when an audited deletion is attempted.
Design-time contexts can build the audit model without actor providers; executing
audited hard deletes still requires those providers.
The SQLite, PostgreSQL, and SQL Server design-time factories inherit from
`Relational.Design.RelationalBaseDesignTimeDbContextFactory<TDbContext>`, which
registers the scoped audit interceptor. Each provider calls the relational
`ConfigureProviderServices` implementation, including when composed as child factories.
Custom relational factories should inherit from this intermediate base class and
call `base.ConfigureProviderServices(...)` if they override that method.
Factories deriving directly from the non-relational Base design-time factory must register
`HardDeleteAuditInterceptor` in `ConfigureServices` themselves. Runtime contexts
require its normal DI registration and fail during configuration if it is missing;
the context never constructs a replacement interceptor.

The late convention uses the final entity table name and schema; by default the
audit table is `<TableName>DeleteAudit` in the same schema. Add a normal EF migration
to create it. Each event contains `AuditId`, the entity key, `DeletedAt`, and nullable
`DeletedBy`, with no foreign key to the deleted row or actor.

### Querying events and composite keys

Simple keys use a shared generic CLR type with a separate EF entity name and table
for each audited entity:

```csharp
var events = await context.HardDeleteAudits<Document, Guid>()
    .Where(audit => audit.EntityKey == documentId)
    .ToListAsync();
```

For non-Guid actors use `HardDeleteAudits<TEntity, TKey, TNullableUser>()`, for
example `HardDeleteAudits<Document, Guid, int?>()`.

Composite primary keys work without a key class. Their dynamic columns are named
`EntityKey_` followed by the source property name:

```csharp
var events = await context.HardDeleteAudits<TenantDocument>()
    .Where(audit =>
        EF.Property<Guid>(audit, "EntityKey_TenantId") == tenantId &&
        EF.Property<int>(audit, "EntityKey_Number") == documentNumber)
    .ToListAsync();
```

For typed composite queries, provide a key value object with a public parameterless
constructor and public writable properties. Map every primary-key component once:

```csharp
public class DocumentKey
{
    public Guid TenantId { get; set; }
    public int Number { get; set; }
}

modelBuilder.Entity<TenantDocument>().UseHardDeleteAudit(audit =>
    audit.HasEntityKey<DocumentKey>(key =>
    {
        key.Property(value => value.TenantId, document => document.TenantId);
        key.Property(value => value.Number, document => document.Number);
    }));

var events = await context.HardDeleteAudits<TenantDocument, DocumentKey>()
    .Where(audit => audit.EntityKey.TenantId == tenantId &&
                    audit.EntityKey.Number == documentNumber)
    .ToListAsync();
```

The key object is mapped as a required complex property, not an owned entity.
Key converters, comparison, length, precision and Unicode settings are copied from
the source key properties. Both typed and dynamic queries run in the database.

### Transaction and scope guarantees

Auditing runs after soft-delete conversion and stamping. Soft deletes and restores
create no physical deletion events; `SoftDeleteSuppressor.AllowHardDelete()` does.
No current actor results in SQL NULL. Stamp suppression preserves the event and
timestamp but omits its actor.

Audit inserts and DELETE statements execute in one `SaveChanges` transaction.
Failures and cancellations discard generated tracked audit entries so a retry
does not duplicate them. Existing EF transactions require automatic savepoints;
ambient/enlisted transactions and disabled automatic transactions are rejected.
After an external transaction rollback, reload/clear the context as with ordinary
EF writes. The `SaveChanges` affected-entry count includes audit inserts.

Only tracked physical deletions are covered. Bulk APIs, raw SQL, database-side
cascades, and external writers bypass this feature. Immediate tracked cascades are
captured; deferred `OnSaveChanges` cascade/orphan timing is rejected for audited
models. Never-saved entities removed from the tracker create no event.

Owned entities, inheritance, shared source CLR types, table sharing, entity splitting,
and keyless entities are rejected rather than incompletely audited. Generated audit
entities cannot be manually inserted, changed or deleted through the ChangeTracker;
this is not a database security boundary or a tamper-proof audit log.
The feature uses runtime generic construction and reflection for inferred audit
types; Native AOT, trimming, and compiled models are not currently guaranteed.

## License
The Basilisque framework (including this repository) is licensed under the [Apache License, Version 2.0](LICENSE.txt).