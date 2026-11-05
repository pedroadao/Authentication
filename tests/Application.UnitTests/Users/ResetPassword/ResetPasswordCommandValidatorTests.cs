using Authentication.Application.Users.ResetPassword;
using FluentAssertions;

namespace Authentication.Application.UnitTests.Users.ResetPassword;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoDeveTerErros()
    {
        var command = new ResetPasswordCommand("usuario@example.com", "token-valido", "NovaSenha123");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "token", "NovaSenha123")]
    [InlineData("email-invalido", "token", "NovaSenha123")]
    [InlineData("usuario@example.com", "", "NovaSenha123")]
    [InlineData("usuario@example.com", "token", "curta1A")]
    [InlineData("usuario@example.com", "token", "semmaiuscula1")]
    [InlineData("usuario@example.com", "token", "SEMMINUSCULA1")]
    [InlineData("usuario@example.com", "token", "SemNumero")]
    public void Validate_ComDadosInvalidos_DeveTerErros(string email, string token, string newPassword)
    {
        var command = new ResetPasswordCommand(email, token, newPassword);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
