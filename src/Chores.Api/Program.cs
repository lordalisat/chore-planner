using Chores.Api;
using Chores.Api.Data;
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

WebApplication app = builder.Build();

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ChoresDbContext>().Database.Migrate();
}

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
