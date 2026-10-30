using Authentication.Domain.Common;
using MediatR;

namespace Authentication.Application.Users.Register;

public sealed record RegisterUserCommand(
    string Email,
    string DisplayName,
    string Password) : IRequest<Result<UserDto>>;
