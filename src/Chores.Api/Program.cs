using Chores.Api;
using Chores.Api.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

if (args is ["hash-password"])
{
    Console.Error.Write("Household password: ");
    Console.WriteLine(Auth.HashPassword(Console.ReadLine() ?? ""));
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

// A file under data/ by default, the directory a Docker volume mounts on
string databasePath = Path.GetFullPath(builder.Configuration["Database:Path"] ?? "data/chores.db");
builder.Services.AddDbContext<ChoresDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddHouseholdAuth(new DirectoryInfo(Path.Combine(Path.GetDirectoryName(databasePath)!, "keys")));
// HTTPS ends at the home server's reverse proxy, whose address inside Docker isn't known up front, so any proxy is trusted
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

WebApplication app = builder.Build();

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ChoresDbContext>().Database.Migrate();
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.MapOpenApi();

// Covers the fallback too, so unknown API paths answer anonymous requests with 401, not 404
RouteGroupBuilder api = app.MapGroup("/api").RequireAuthorization();
api.MapGet("/health", () => TypedResults.Ok()).AllowAnonymous();
api.MapAuthEndpoints();
// Keeps unknown API paths out of the front-end fallback below
api.MapFallback(() => TypedResults.NotFound()).ExcludeFromDescription();

app.MapFallbackToFile("index.html");

app.Run();
