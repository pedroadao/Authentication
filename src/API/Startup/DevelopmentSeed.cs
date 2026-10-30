using Authentication.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Authentication.API.Startup;

/// <summary>
/// Aplica migrations e garante a existência de um client OAuth de exemplo,
/// apenas em ambiente de desenvolvimento, para permitir testar o fluxo
/// Authorization Code + PKCE localmente sem configuração manual adicional.
/// </summary>
public static class DevelopmentSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();

        if (await applicationManager.FindByClientIdAsync("spa-client") is null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "spa-client",
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "Cliente de desenvolvimento (SPA)",
                RedirectUris =
                {
                    new Uri("https://localhost:5173/callback"),
                    // Callback padrão usado pelo helper OAuth 2.0 do Postman.
                    new Uri("https://oauth.pstmn.io/v1/callback"),
                },
                PostLogoutRedirectUris = { new Uri("https://localhost:5173/") },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "offline_access",
                },
                Requirements = { Requirements.Features.ProofKeyForCodeExchange },
            });
        }
    }
}
