using Basilisque.DataAccess.EntityFramework.Base.Data;
using Basilisque.Demo.Web.Components;
using Basilisque.Demo.Web.Services;
using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);
// dotnet run must also serve framework assets outside the Development environment.
builder.WebHost.UseStaticWebAssets();
builder.Configuration
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddDemo(builder.Configuration);

var app = builder.Build();
if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await using (var scope = app.Services.CreateAsyncScope())
{
    foreach (var initializer in scope.ServiceProvider.GetServices<IDbInitializer>())
    {
        initializer.BeforeMigration();
        initializer.MigrateDatabase();
        initializer.AfterMigration();
        initializer.SeedData();
    }
}

await app.RunAsync();
