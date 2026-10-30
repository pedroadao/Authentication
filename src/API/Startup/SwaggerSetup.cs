using Microsoft.OpenApi;

namespace Authentication.API.Startup;

/// <summary>
/// Documentação Swagger/OpenAPI da API, incluindo o esquema de segurança
/// OAuth 2.0 (Authorization Code + PKCE) usado pelos endpoints protegidos.
/// Habilitada apenas em ambiente de desenvolvimento.
/// </summary>
public static class SwaggerSetup
{
    private const string OAuth2SecurityScheme = "oauth2";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Authentication API",
                Version = "v1",
                Description = "API de autenticação OAuth 2.0 / OpenID Connect (OpenIddict + ASP.NET Core Identity).",
            });

            var baseUrl = configuration["Swagger:BaseUrl"] ?? "https://localhost:8443";

            options.AddSecurityDefinition(OAuth2SecurityScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{baseUrl}/connect/authorize"),
                        TokenUrl = new Uri($"{baseUrl}/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"] = "Identificação OpenID Connect",
                            ["profile"] = "Nome e dados de perfil",
                            ["email"] = "Endereço de e-mail",
                            ["offline_access"] = "Emissão de refresh token",
                        },
                    },
                },
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference(OAuth2SecurityScheme, document), [] },
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Authentication API v1");
            options.OAuthClientId("spa-client");
            options.OAuthUsePkce();
            options.OAuthScopeSeparator(" ");
        });

        return app;
    }
}
