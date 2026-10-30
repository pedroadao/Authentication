using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Authentication.API.Endpoints;

/// <summary>
/// Endpoint de recurso protegido de exemplo, que exige um access token válido
/// emitido pelo servidor OpenIddict (Bearer scheme).
/// </summary>
public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new
            {
                Id = user.FindFirstValue(Claims.Subject),
                Email = user.FindFirstValue(Claims.Email),
                Name = user.FindFirstValue(Claims.Name),
            }))
            .RequireAuthorization()
            .WithTags("Me");

        return app;
    }
}
