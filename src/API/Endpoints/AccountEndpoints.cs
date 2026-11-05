using Authentication.API.Contracts;
using Authentication.Application.Users.ForgotPassword;
using Authentication.Application.Users.Register;
using Authentication.Application.Users.ResetPassword;
using Authentication.Domain.Common;
using MediatR;

namespace Authentication.API.Endpoints;

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/forgot-password", ForgotPasswordAsync);
        group.MapPost("/reset-password", ResetPasswordAsync);

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.Email, request.DisplayName, request.Password);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/accounts/{result.Value.Id}", result.Value)
            : MapFailure(result);
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : MapFailure(result);
    }

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Email, request.Token, request.NewPassword);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : MapFailure(result);
    }

    private static IResult MapFailure(Result result)
    {
        var statusCode = result.Error.Code switch
        {
            "User.EmailAlreadyInUse" => StatusCodes.Status409Conflict,
            "User.RegistrationFailed" => StatusCodes.Status400BadRequest,
            "User.InvalidOrExpiredResetToken" => StatusCodes.Status400BadRequest,
            "User.PasswordResetFailed" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            title: result.Error.Code,
            detail: result.Error.Message,
            statusCode: statusCode);
    }
}
