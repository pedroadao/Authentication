using Authentication.Application.Abstractions;
using Authentication.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IUserService userService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private const string GenericMessage =
        "Se existir uma conta com este e-mail, instruções de redefinição de senha serão enviadas.";

    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userService.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            logger.LogInformation("Solicitação de redefinição de senha para e-mail não cadastrado.");
            return new ForgotPasswordResponse(GenericMessage, null);
        }

        var token = await userService.GeneratePasswordResetTokenAsync(user, cancellationToken);

        logger.LogInformation("Token de redefinição de senha gerado para usuário existente.");

        return new ForgotPasswordResponse(GenericMessage, token);
    }
}
