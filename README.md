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

## License
The Basilisque framework (including this repository) is licensed under the [Apache License, Version 2.0](LICENSE.txt).