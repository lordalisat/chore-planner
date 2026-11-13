var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseStaticFiles();
app.MapOpenApi();

var api = app.MapGroup("/api");
api.MapGet("/health", () => TypedResults.Ok());
// Keeps unknown API paths out of the front-end fallback below
api.MapFallback(() => TypedResults.NotFound()).ExcludeFromDescription();

app.MapFallbackToFile("index.html");

app.Run();
