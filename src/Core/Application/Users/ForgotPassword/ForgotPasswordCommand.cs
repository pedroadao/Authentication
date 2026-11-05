using Authentication.Domain.Common;
using MediatR;

namespace Authentication.Application.Users.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<ForgotPasswordResponse>>;
