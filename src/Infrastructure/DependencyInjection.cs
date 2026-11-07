using Authentication.Application.Abstractions;
using Authentication.Domain.Users;
using Authentication.Infrastructure.Identity;
using Authentication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System.Security.Cryptography.X509Certificates;

namespace Authentication.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("AppDb")
            ?? throw new InvalidOperationException("Connection string 'AppDb' não configurada.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserService, UserService>();

        services
            .AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore().UseDbContext<AppDbContext>();
            })
            .AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo")
                    .SetEndSessionEndpointUris("connect/logout");

                options
                    .AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow();

                // Sem isso, o access_token é um JWE (criptografado) que só este processo
                // consegue abrir, então um resource server externo (ex.: Barbearia, que usa
                // JwtBearer genérico, não OpenIddict.Validation) nunca valida o token. O
                // token continua assinado (não falsificável) e só interno a este servidor.
                options.DisableAccessTokenEncryption();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.OfflineAccess);

                if (environment.IsDevelopment())
                {
                    options
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }
                else
                {
                    var signingCertPath = configuration["OpenIddict:SigningCertificatePath"]
                        ?? throw new InvalidOperationException(
                            "OpenIddict:SigningCertificatePath não configurado para o ambiente atual.");
                    var signingCertPassword = configuration["OpenIddict:SigningCertificatePassword"]
                        ?? throw new InvalidOperationException(
                            "OpenIddict:SigningCertificatePassword não configurado para o ambiente atual.");
                    var encryptionCertPath = configuration["OpenIddict:EncryptionCertificatePath"]
                        ?? throw new InvalidOperationException(
                            "OpenIddict:EncryptionCertificatePath não configurado para o ambiente atual.");
                    var encryptionCertPassword = configuration["OpenIddict:EncryptionCertificatePassword"]
                        ?? throw new InvalidOperationException(
                            "OpenIddict:EncryptionCertificatePassword não configurado para o ambiente atual.");

                    options
                        .AddSigningCertificate(X509CertificateLoader.LoadPkcs12FromFile(signingCertPath, signingCertPassword))
                        .AddEncryptionCertificate(X509CertificateLoader.LoadPkcs12FromFile(encryptionCertPath, encryptionCertPassword));
                }

                options
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
