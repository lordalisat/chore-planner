using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Chores.Api.Tests;

/// <summary>Runs the API against a throwaway web root standing in for the built front end.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string IndexHtml = "<!doctype html><title>Chores test page</title>";

    private readonly DirectoryInfo _webRoot = Directory.CreateTempSubdirectory("chores-wwwroot-");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        File.WriteAllText(Path.Combine(_webRoot.FullName, "index.html"), IndexHtml);
        builder.UseWebRoot(_webRoot.FullName);
        // Development would layer the project's own wwwroot (a built front end, if any) over this web root
        builder.UseEnvironment("Testing");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _webRoot.Delete(recursive: true);
    }
}
