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
the timestamp handler, and a user-stamp dispatcher with open generic
`IUserStampHandler<TKey>` / `UserStampHandler<TKey>` registration. It also registers the
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

Core already provides user contexts for other key types through its open generic
registrations. The dispatcher discovers the user-key types from configured CLR and
shadow stamp properties in the EF model and resolves their handlers from the current
scope. No additional handler registration is needed for string, int, or other
supported user-key types. For example, set the current string user in the context's scope:

```csharp
using Basilisque.Core.Auth;
using Microsoft.Extensions.DependencyInjection;

scope.ServiceProvider.GetRequiredService<IWritableUserContext<string>>().UserId = "user-123";
```

Applications can replace a handler for a specific key type by registering a closed
`IUserStampHandler<TKey>` implementation.
Registration of custom user-context implementations, proxies, or convenience
interfaces is the application's responsibility.

Design-time factories register the interceptor independently and do not require
application user-context services. No stamp handlers are registered by default at
design time, so explicitly supplied seed values are not overwritten by stamping.
The runtime dispatcher dynamically closes generic handler types; Native AOT and
trimmed deployments require separate compatibility validation.

## License
The Basilisque framework (including this repository) is licensed under the [Apache License, Version 2.0](LICENSE.txt).