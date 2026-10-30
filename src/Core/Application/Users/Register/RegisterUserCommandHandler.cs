using Authentication.Application.Abstractions;
using Authentication.Domain.Common;
using Authentication.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Authentication.Application.Users.Register;

public sealed class RegisterUserCommandHandler(
    IUserService userService,
    ILogger<RegisterUserCommandHandler> logger)
    : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var emailInUse = await userService.ExistsByEmailAsync(request.Email, cancellationToken);

        if (emailInUse)
        {
            logger.LogInformation("Tentativa de registro com e-mail já existente.");
            return Result.Failure<UserDto>(UserErrors.EmailAlreadyInUse);
        }

        var result = await userService.CreateAsync(
            request.Email,
            request.DisplayName,
            request.Password,
            cancellationToken);

        if (result.IsFailure)
            return Result.Failure<UserDto>(result.Error);

        var user = result.Value;

        logger.LogInformation("Novo usuário registrado com sucesso.");

        return new UserDto(user.Id, user.Email!, user.DisplayName);
    }
}
