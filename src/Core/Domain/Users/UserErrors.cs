using Authentication.Domain.Common;

namespace Authentication.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailAlreadyInUse = new(
        "User.EmailAlreadyInUse",
        "Já existe um usuário cadastrado com este e-mail.");

    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "E-mail ou senha inválidos.");

    public static readonly Error LockedOut = new(
        "User.LockedOut",
        "Conta temporariamente bloqueada devido a múltiplas tentativas inválidas.");

    public static Error RegistrationFailed(string details) => new(
        "User.RegistrationFailed",
        details);

    public static readonly Error NotFound = new(
        "User.NotFound",
        "Usuário não encontrado.");

    public static readonly Error InvalidOrExpiredResetToken = new(
        "User.InvalidOrExpiredResetToken",
        "Token de redefinição de senha inválido ou expirado.");

    public static Error PasswordResetFailed(string details) => new(
        "User.PasswordResetFailed",
        details);
}
