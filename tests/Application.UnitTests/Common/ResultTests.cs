using Authentication.Domain.Common;
using FluentAssertions;

namespace Authentication.Application.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error SampleError = new("Sample.Error", "Mensagem de erro de exemplo.");

    [Fact]
    public void Success_DeveCriarResultadoComIsSuccessVerdadeiro()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_DeveCriarResultadoComIsFailureVerdadeiro()
    {
        var result = Result.Failure(SampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Failure_ComValor_AoAcessarValue_DeveLancarExcecao()
    {
        var result = Result.Failure<string>(SampleError);

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConstrutorImplicito_DeveCriarResultadoDeSucesso()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Success_ComErroNoneEIsSuccessFalso_DeveLancarExcecao()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }
}
