using Authentication.Application.Abstractions;
using Authentication.Application.Users.ForgotPassword;
using Authentication.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Authentication.Application.UnitTests.Users.ForgotPassword;

public sealed class ForgotPasswordCommandHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ILogger<ForgotPasswordCommandHandler> _logger =
        Substitute.For<ILogger<ForgotPasswordCommandHandler>>();

    private ForgotPasswordCommandHandler CreateHandler() => new(_userService, _logger);

    [Fact]
    public async Task Handle_ComEmailInexistente_DeveRetornarMensagemGenericaSemToken()
    {
        var command = new ForgotPasswordCommand("inexistente@example.com");

        _userService.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((ApplicationUser?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResetToken.Should().BeNull();

        await _userService.DidNotReceive().GeneratePasswordResetTokenAsync(
            Arg.Any<ApplicationUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComEmailExistente_DeveRetornarToken()
    {
        var command = new ForgotPasswordCommand("usuario@example.com");
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = command.Email, DisplayName = "Fulano" };

        _userService.FindByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        _userService.GeneratePasswordResetTokenAsync(user, Arg.Any<CancellationToken>())
            .Returns("token-gerado");

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResetToken.Should().Be("token-gerado");
    }
}
