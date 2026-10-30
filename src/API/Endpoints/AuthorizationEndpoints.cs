using System.Collections.Immutable;
using System.Security.Claims;
using Authentication.Domain.Users;
using Microsoft.AspNetCore; // GetOpenIddictServerRequest() (OpenIddictServerAspNetCoreHelpers)
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Authentication.API.Endpoints;

/// <summary>
/// Implementa os endpoints do protocolo OAuth 2.0/OpenID Connect (connect/*),
/// delegando emissão/validação de tokens ao OpenIddict. Segue o padrão
/// oficial dos exemplos do OpenIddict (openiddict-samples).
/// </summary>
public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/connect/authorize", ["GET", "POST"], (Delegate)AuthorizeAsync);
        app.MapPost("/connect/token", ExchangeAsync);
        app.MapMethods("/connect/userinfo", ["GET", "POST"], (Delegate)UserinfoAsync);
        app.MapMethods("/connect/logout", ["GET", "POST"], (Delegate)LogoutAsync);

        return app;
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager)
    {
        var request = httpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("Requisição OpenID Connect inválida.");

        var authenticateResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded)
        {
            return Results.Challenge(
                authenticationSchemes: [CookieAuthenticationDefaults.AuthenticationScheme],
                properties: new AuthenticationProperties
                {
                    RedirectUri = httpContext.Request.PathBase + httpContext.Request.Path + httpContext.Request.QueryString,
                });
        }

        var user = await signInManager.UserManager.GetUserAsync(authenticateResult.Principal)
            ?? throw new InvalidOperationException("Não foi possível resolver o usuário autenticado.");

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Email, user.Email)
            .SetClaim(Claims.Name, user.DisplayName)
            .SetClaims(Claims.Role, [.. await signInManager.UserManager.GetRolesAsync(user)]);

        identity.SetScopes(request.GetScopes());
        identity.SetResources("authentication-api");
        identity.SetDestinations(GetDestinations);

        return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager)
    {
        var request = httpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("Requisição OpenID Connect inválida.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var authenticateResult = await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = authenticateResult.Principal?.GetClaim(Claims.Subject);

            if (userId is null || await signInManager.UserManager.FindByIdAsync(userId) is not { } user)
            {
                return Results.Forbid(
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "O token não é mais válido ou o usuário associado não existe mais.",
                    }));
            }

            var identity = new ClaimsIdentity(
                authenticationType: TokenValidationParameters,
                nameType: Claims.Name,
                roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, user.Id.ToString())
                .SetClaim(Claims.Email, user.Email)
                .SetClaim(Claims.Name, user.DisplayName)
                .SetClaims(Claims.Role, [.. await signInManager.UserManager.GetRolesAsync(user)]);

            identity.SetScopes(authenticateResult.Principal!.GetScopes());
            identity.SetResources("authentication-api");
            identity.SetDestinations(GetDestinations);

            return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("Tipo de fluxo (grant type) não suportado.");
    }

    private static async Task<IResult> UserinfoAsync(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager)
    {
        var principal = (await httpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal
            ?? throw new InvalidOperationException("Token de acesso ausente ou inválido.");

        var userId = principal.GetClaim(Claims.Subject);

        if (userId is null || await signInManager.UserManager.FindByIdAsync(userId) is not { } user)
        {
            return Results.Forbid(
                authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.Id.ToString(),
        };

        if (principal.HasScope(Scopes.Email))
            claims[Claims.Email] = user.Email!;

        if (principal.HasScope(Scopes.Profile))
            claims[Claims.Name] = user.DisplayName;

        return Results.Ok(claims);
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.SignOut(
            authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties
            {
                RedirectUri = "/",
            });
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.Email:
                yield return Destinations.AccessToken;

                if (claim.Subject?.HasScope(Scopes.Profile) == true && claim.Type == Claims.Name)
                    yield return Destinations.IdentityToken;

                if (claim.Subject?.HasScope(Scopes.Email) == true && claim.Type == Claims.Email)
                    yield return Destinations.IdentityToken;

                break;

            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                break;

            default:
                yield return Destinations.AccessToken;
                break;
        }
    }

    private const string TokenValidationParameters = "OpenIddict";
}
