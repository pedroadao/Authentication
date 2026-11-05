using Authentication.Application.Abstractions;
using Authentication.Domain.Common;
using Authentication.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserService userService,
    ILogger<ResetPasswordCommandHandler> logger)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userService.FindByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            logger.LogInformation("Tentativa de redefinição de senha para e-mail não cadastrado.");
            return Result.Failure(UserErrors.InvalidOrExpiredResetToken);
        }

        var result = await userService.ResetPasswordAsync(
            user,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (result.IsFailure)
            return result;

        logger.LogInformation("Senha redefinida com sucesso.");

        return Result.Success();
    }
}
