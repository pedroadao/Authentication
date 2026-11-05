namespace Authentication.API.Contracts;

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
