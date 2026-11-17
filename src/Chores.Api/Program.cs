using Chores.Api.Data;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

// A file under data/ by default, the directory a Docker volume mounts on
string databasePath = Path.GetFullPath(builder.Configuration["Database:Path"] ?? "data/chores.db");
builder.Services.AddDbContext<ChoresDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));

WebApplication app = builder.Build();

Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ChoresDbContext>().Database.Migrate();
}

app.UseStaticFiles();
app.MapOpenApi();

RouteGroupBuilder api = app.MapGroup("/api");
api.MapGet("/health", () => TypedResults.Ok());
// Keeps unknown API paths out of the front-end fallback below
api.MapFallback(() => TypedResults.NotFound()).ExcludeFromDescription();

app.MapFallbackToFile("index.html");

app.Run();
