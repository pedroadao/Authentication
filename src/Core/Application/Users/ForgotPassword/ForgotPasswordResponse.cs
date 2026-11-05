namespace Authentication.Application.Users.ForgotPassword;

/// <summary>
/// ResetToken só é preenchido quando a conta existe. Em produção este token
/// deve ser enviado por e-mail, e não retornado na resposta HTTP; ele é
/// exposto aqui por não haver, ainda, um serviço de e-mail integrado.
/// </summary>
public sealed record ForgotPasswordResponse(string Message, string? ResetToken);
