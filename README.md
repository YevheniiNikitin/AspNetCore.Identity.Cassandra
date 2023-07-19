[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Identity.Cassandra.Niki.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/AspNetCore.Identity.Cassandra.Niki/)

# ASP.NET Core Identity Cassandra
[Apache Cassandra](https://cassandra.apache.org/) data store adapter for [ASP.NET Core Identity](https://github.com/dotnet/aspnetcore/tree/main/src/Identity), which allows you to build ASP.NET Core web applications, including membership, login, and user data. With this library, you can store your user's membership related data on Apache Cassandra.  
Inspired by existing [AspNetCore.Identity.RaveDB](https://github.com/ElemarJR/AspNetCore.Identity.RavenDB) implementation.

### Installation

You can install AspNetCore.Identity.Cassandra via NuGet. Run the following command in the NuGet Package Manager Console:
```code
PM> Install-Package AspNetCore.Identity.Cassandra.Niki
```

### Usage

To use the AspNetCore.Identity.Cassandra, follow these steps:

1. Create your own User and Role entities:
```csharp
[Table("roles", Keyspace = "identity")]
public class ApplicationRole : CassandraIdentityRole
{
    public ApplicationRole()
        : base(Guid.NewGuid())
    { }
}

[Table("users", Keyspace = "identity")]
public class ApplicationUser : CassandraIdentityUser
{
    public ApplicationUser()
        : base(Guid.NewGuid())
    { }
}
```

2. Configure options for Cassandra in the `appsettings.json` file:
```json
{
  "Cassandra": {
    "ContactPoints": [
      "127.0.0.1"
    ],
    "Port": 9042,
    "RetryCount": 5,
    "Credentials": {
      "UserName": "cassandra",
      "Password": "cassandra"
    },
    "KeyspaceName": "kanyafields",
    "Replication": {
      "class": "SimpleStrategy",
      "replication_factor": "2"
    },
    "Query": {
      "ConsistencyLevel": "One",
      "TracingEnabled": false,
      "PageSize": 25
    }
  }
}
```

3. Configure the following services:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddCassandra(Configuration);

    services.AddIdentity<ApplicationUser, ApplicationRole>()
        .AddCassandraErrorDescriber<CassandraErrorDescriber>()
        .UseCassandraStores<Cassandra.ISession>()
        .AddDefaultTokenProviders();

    // will create all Identity related tables (if not exist) in the Cassandra database
    // on the application startup
    services.AddHostedService<CassandraIdentityInfrastructureInitializer<ApplicationUser, ApplicationRole>>();

    ...
}
```

4. Resolve `UserManager`, `RoleManager` or `SignInManager` wherever you need them:
```csharp
public IndexModel(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    SignInManager<ApplicationUser> signInManager,
    IEmailSender emailSender)
{
    _userManager = userManager;
    _roleManager = roleManager;
    _signInManager = signInManager;
    _emailSender = emailSender;
}
```
