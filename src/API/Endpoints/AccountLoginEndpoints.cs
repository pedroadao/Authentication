using System.Net;
using Authentication.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

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
        var fields = await ReadFieldsAsync(request);
        var email = fields.GetValueOrDefault("email", StringValues.Empty).ToString();
        var password = fields.GetValueOrDefault("password", StringValues.Empty).ToString();
        var returnUrl = fields.GetValueOrDefault("returnUrl", StringValues.Empty).ToString();

        if (string.IsNullOrEmpty(returnUrl))
            returnUrl = "/";

        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);
        var wantsJson = AcceptsJson(request);

        if (!result.Succeeded)
        {
            var error = result.IsLockedOut
                ? "Conta temporariamente bloqueada devido a múltiplas tentativas inválidas."
                : "E-mail ou senha inválidos.";

            if (wantsJson)
                return Results.Json(new { error }, statusCode: StatusCodes.Status401Unauthorized);

            return Results.Redirect($"/account/login?returnUrl={WebUtility.UrlEncode(returnUrl)}&error={WebUtility.UrlEncode(error)}");
        }

        if (wantsJson)
            return Results.Json(new { succeeded = true });

        return Results.Redirect(returnUrl);
    }

    /// <summary>
    /// Indica se a chamada veio de um cliente JS (SPA) que espera uma resposta JSON
    /// em vez do redirect HTML padrão do formulário de login.
    /// </summary>
    private static bool AcceptsJson(HttpRequest request)
    {
        if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return true;

        return request.Headers.Accept.Any(value =>
            value is not null && value.Contains("application/json", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lê os campos do formulário de login. Alguns navegadores/extensões (ex.: autofill
    /// de senha) reenviam o submit via fetch/XHR sem o header Content-Type correto;
    /// nesses casos, interpretamos o corpo bruto como application/x-www-form-urlencoded
    /// em vez de falhar com "Incorrect Content-Type".
    /// </summary>
    private static async Task<Dictionary<string, StringValues>> ReadFieldsAsync(HttpRequest request)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            return form.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
            return [];

        return QueryHelpers.ParseQuery(body).ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
