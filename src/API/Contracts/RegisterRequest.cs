namespace Authentication.API.Contracts;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);
