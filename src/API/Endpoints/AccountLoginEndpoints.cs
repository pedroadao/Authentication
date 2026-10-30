using System.Net;
using Authentication.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Authentication.API.Endpoints;

/// <summary>
/// Página mínima de login usada para autenticar o "resource owner" durante
/// o fluxo Authorization Code + PKCE (endpoint /connect/authorize redireciona
/// para aqui quando o usuário ainda não possui sessão de cookie ativa).
/// </summary>
public static class AccountLoginEndpoints
{
    public static IEndpointRouteBuilder MapAccountLoginEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/account/login", ShowLoginForm);
        app.MapPost("/account/login", SubmitLoginAsync);

        return app;
    }

    private static IResult ShowLoginForm(string? returnUrl, string? error)
    {
        var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl ?? "/");
        var errorHtml = string.IsNullOrEmpty(error)
            ? string.Empty
            : $"<p style=\"color:red\">{WebUtility.HtmlEncode(error)}</p>";

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt-br">
            <head><meta charset="utf-8"><title>Login</title></head>
            <body>
                <h1>Entrar</h1>
                {errorHtml}
                <form method="post" action="/account/login">
                    <input type="hidden" name="returnUrl" value="{encodedReturnUrl}" />
                    <label>E-mail <input type="email" name="email" required /></label><br />
                    <label>Senha <input type="password" name="password" required /></label><br />
                    <button type="submit">Entrar</button>
                </form>
            </body>
            </html>
            """;

        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> SubmitLoginAsync(
        HttpRequest request,
        SignInManager<ApplicationUser> signInManager)
    {
        var form = await request.ReadFormAsync();
        var email = form["email"].ToString();
        var password = form["password"].ToString();
        var returnUrl = form["returnUrl"].ToString();

        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = "/";

        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            var error = result.IsLockedOut
                ? "Conta temporariamente bloqueada devido a múltiplas tentativas inválidas."
                : "E-mail ou senha inválidos.";

            return Results.Redirect($"/account/login?returnUrl={WebUtility.UrlEncode(returnUrl)}&error={WebUtility.UrlEncode(error)}");
        }

        return Results.Redirect(returnUrl);
    }
}
