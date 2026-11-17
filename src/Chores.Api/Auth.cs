using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Chores.Api;

/// <summary>One shared household password issuing a cookie, plus one API key for AI tooling, see ADR 0001.</summary>
public static class Auth
{
    private const string ApiKeyScheme = "ApiKey";

    private static readonly PasswordHasher<object> Hasher = new();
    // PasswordHasher hashes per user; there is only the household
    private static readonly object Household = new();

    /// <summary>The value to configure as Auth:PasswordHash for a household password.</summary>
    public static string HashPassword(string password) => Hasher.HashPassword(Household, password);

    /// <param name="keysDirectory">Where the keys that encrypt the cookie live; they must outlive the process, or every restart logs everyone out.</param>
    public static IServiceCollection AddHouseholdAuth(this IServiceCollection services, DirectoryInfo keysDirectory)
    {
        services.AddDataProtection().PersistKeysToFileSystem(keysDirectory);
        services.AddOptions<ApiKeyOptions>(ApiKeyScheme).BindConfiguration("Auth");
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "chores-session";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromDays(365);
                // An API answers 401 rather than redirecting to a login page
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            })
            .AddScheme<ApiKeyOptions, ApiKeyHandler>(ApiKeyScheme, null);
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme, ApiKeyScheme)
                .RequireAuthenticatedUser()
                .Build());
        return services;
    }

    public static void MapAuthEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/login", async Task<Results<NoContent, UnauthorizedHttpResult>> (LoginRequest request, HttpContext context, IConfiguration configuration) =>
        {
            string? hash = configuration["Auth:PasswordHash"];
            if (string.IsNullOrEmpty(hash) || Hasher.VerifyHashedPassword(Household, hash, request.Password) == PasswordVerificationResult.Failed)
            {
                return TypedResults.Unauthorized();
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme));
            await context.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });
            return TypedResults.NoContent();
        }).AllowAnonymous();

        api.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.NoContent();
        });

        // Lets the front end ask whether it is logged in
        api.MapGet("/session", () => TypedResults.NoContent());
    }

    private sealed record LoginRequest(string Password);

    private sealed class ApiKeyOptions : AuthenticationSchemeOptions
    {
        /// <summary>The key AI tooling sends in the X-Api-Key header; when unset, no key is accepted.</summary>
        public string? ApiKey { get; set; }
    }

    private sealed class ApiKeyHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<ApiKeyOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Api-Key", out var sent))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (string.IsNullOrEmpty(Options.ApiKey)
                || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sent.ToString()), Encoding.UTF8.GetBytes(Options.ApiKey)))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
