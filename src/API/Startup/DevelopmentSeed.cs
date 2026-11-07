using Authentication.Domain.Users;
using Authentication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
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

        await EnsureApplicationAsync(applicationManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "spa-client",
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Cliente de desenvolvimento (SPA)",
            RedirectUris =
            {
                new Uri("https://localhost:5173/callback"),
                // Renovação silenciosa de sessão (iframe oculto, prompt=none) após
                // reload de página, sem exigir novo login.
                new Uri("https://localhost:5173/silent-callback"),
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

        await EnsureApplicationAsync(applicationManager, new OpenIddictApplicationDescriptor
        {
            ClientId = "barbearia-spa-client",
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "Cliente de desenvolvimento (BarbeariaReact)",
            RedirectUris =
            {
                // vite dev (npm run dev).
                new Uri("http://localhost:5174/callback"),
                new Uri("http://localhost:5174/silent-callback"),
                // nginx via docker-compose da Barbearia (porta WEB_PORT).
                new Uri("http://localhost:8082/callback"),
                new Uri("http://localhost:8082/silent-callback"),
            },
            PostLogoutRedirectUris =
            {
                new Uri("http://localhost:5174/"),
                new Uri("http://localhost:8082/"),
            },
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

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        const string adminRole = "Administrador";

        if (await roleManager.FindByNameAsync(adminRole) is null)
            await roleManager.CreateAsync(new IdentityRole<Guid>(adminRole));

        // Login fixo de administração para desenvolvimento local, enquanto o 2FA
        // (planejado para mais adiante) ainda não existe. A role "Administrador"
        // vai no claim "role" do access token (ver AuthorizationEndpoints), que a
        // API da Barbearia usa para restringir os endpoints de administração.
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminEmail = "admin@admin.com";

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrador",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            await userManager.CreateAsync(admin, "Senha123/");
        }

        if (!await userManager.IsInRoleAsync(admin, adminRole))
            await userManager.AddToRoleAsync(admin, adminRole);
    }

    /// <summary>
    /// Cria o client OAuth se ainda não existir, ou atualiza seus dados (ex.: novos
    /// redirect URIs) se já existir — sem isso, um client já seedado em execuções
    /// anteriores nunca receberia os ajustes feitos aqui no código.
    /// </summary>
    private static async Task EnsureApplicationAsync(
        IOpenIddictApplicationManager applicationManager,
        OpenIddictApplicationDescriptor descriptor)
    {
        var existing = await applicationManager.FindByClientIdAsync(descriptor.ClientId!);

        if (existing is null)
        {
            await applicationManager.CreateAsync(descriptor);
            return;
        }

        await applicationManager.UpdateAsync(existing, descriptor);
    }
}
