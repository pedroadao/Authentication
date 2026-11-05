using Authentication.Application.Abstractions;
using Authentication.Application.Users.ResetPassword;
using Authentication.Domain.Common;
using Authentication.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Authentication.Application.UnitTests.Users.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ILogger<ResetPasswordCommandHandler> _logger =
        Substitute.For<ILogger<ResetPasswordCommandHandler>>();

    private ResetPasswordCommandHandler CreateHandler() => new(_userService, _logger);

    [Fact]
    public async Task Handle_ComEmailInexistente_DeveRetornarTokenInvalido()
    {
        var command = new ResetPasswordCommand("inexistente@example.com", "token", "NovaSenha123");

        _userService.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((ApplicationUser?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.InvalidOrExpiredResetToken);

        await _userService.DidNotReceive().ResetPasswordAsync(
            Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComTokenInvalido_DeveRetornarFalha()
    {
        var command = new ResetPasswordCommand("usuario@example.com", "token-invalido", "NovaSenha123");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = command.Email, DisplayName = "Fulano" };

        _userService.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        _userService.ResetPasswordAsync(user, command.Token, command.NewPassword, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(UserErrors.PasswordResetFailed("Token inválido.")));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.PasswordResetFailed");
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveRedefinirSenha()
    {
        var command = new ResetPasswordCommand("usuario@example.com", "token-valido", "NovaSenha123");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = command.Email, DisplayName = "Fulano" };

        _userService.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        _userService.ResetPasswordAsync(user, command.Token, command.NewPassword, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
