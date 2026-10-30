using Authentication.Application.Abstractions;
using Authentication.Application.Users.Register;
using Authentication.Domain.Common;
using Authentication.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Authentication.Application.UnitTests.Users.Register;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ILogger<RegisterUserCommandHandler> _logger =
        Substitute.For<ILogger<RegisterUserCommandHandler>>();

    private RegisterUserCommandHandler CreateHandler() => new(_userService, _logger);

    [Fact]
    public async Task Handle_ComEmailJaExistente_DeveRetornarFalha()
    {
        var command = new RegisterUserCommand("existente@example.com", "Fulano", "Senha123");

        _userService.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailAlreadyInUse);

        await _userService.DidNotReceive().CreateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ComDadosValidos_DeveCriarUsuarioERetornarDto()
    {
        var command = new RegisterUserCommand("novo@example.com", "Fulano de Tal", "Senha123");
        var userId = Guid.NewGuid();

        _userService.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);

        _userService.CreateAsync(command.Email, command.DisplayName, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new ApplicationUser
            {
                Id = userId,
                Email = command.Email,
                DisplayName = command.DisplayName,
            }));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be(command.Email);
        result.Value.DisplayName.Should().Be(command.DisplayName);
    }

    [Fact]
    public async Task Handle_QuandoCriacaoFalha_DeveRetornarFalhaComDetalhes()
    {
        var command = new RegisterUserCommand("novo@example.com", "Fulano de Tal", "Senha123");

        _userService.ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);

        _userService.CreateAsync(command.Email, command.DisplayName, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ApplicationUser>(UserErrors.RegistrationFailed("Senha fraca.")));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RegistrationFailed");
    }
}
