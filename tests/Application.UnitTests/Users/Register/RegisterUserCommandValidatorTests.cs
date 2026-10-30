using Authentication.Application.Users.Register;
using FluentAssertions;

namespace Authentication.Application.UnitTests.Users.Register;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ComDadosValidos_NaoDeveTerErros()
    {
        var command = new RegisterUserCommand("usuario@example.com", "Usuário Teste", "Senha123");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Nome", "Senha123")]
    [InlineData("email-invalido", "Nome", "Senha123")]
    [InlineData("usuario@example.com", "", "Senha123")]
    [InlineData("usuario@example.com", "Nome", "curta1A")]
    [InlineData("usuario@example.com", "Nome", "semmaiuscula1")]
    [InlineData("usuario@example.com", "Nome", "SEMMINUSCULA1")]
    [InlineData("usuario@example.com", "Nome", "SemNumero")]
    public void Validate_ComDadosInvalidos_DeveTerErros(string email, string displayName, string password)
    {
        var command = new RegisterUserCommand(email, displayName, password);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
