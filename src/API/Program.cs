using Authentication.API.Endpoints;
using Authentication.API.Startup;
using Authentication.Application;
using Authentication.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
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
