using Authentication.API.Endpoints;
using Authentication.API.Startup;
using Authentication.Application;
using Authentication.Infrastructure;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build();
});

const string SpaCorsPolicy = "SpaCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(SpaCorsPolicy, policy => policy
        // https://localhost:5173: AuthenticationReact. http://localhost:5174: BarbeariaReact,
        // que também autentica diretamente contra este provedor (Authorization Code + PKCE).
        .WithOrigins("https://localhost:5173", "http://localhost:5174")
        .AllowAnyMethod()
        .WithHeaders("Content-Type", "Accept", "Authorization", "X-Requested-With")
        .AllowCredentials());
});

builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});

if (builder.Environment.IsDevelopment())
    builder.Services.AddSwaggerDocumentation(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler("/error");
app.Map("/error", () => Results.Problem(statusCode: StatusCodes.Status500InternalServerError));

app.UseHttpsRedirection();

app.UseCors(SpaCorsPolicy);

if (app.Environment.IsDevelopment())
    app.UseSwaggerDocumentation();

app.UseAuthentication();
app.UseAuthorization();

app.MapAccountEndpoints();
app.MapAccountLoginEndpoints();
app.MapAuthorizationEndpoints();
app.MapMeEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DevelopmentSeed.SeedAsync(scope.ServiceProvider);
}

app.Run();
