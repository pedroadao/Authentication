using Authentication.Domain.Common;
using MediatR;

namespace Authentication.Application.Users.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<Result>;
