using Authentication.Domain.Common;
using FluentValidation;
using MediatR;

namespace Authentication.Application.Common;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var errors = validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (errors.Count == 0)
            return await next();

        var error = new Error(
            "Validation.Failed",
            string.Join(" ", errors.Select(e => e.ErrorMessage)));

        return (TResponse)CreateFailureResult(typeof(TResponse), error);
    }

    private static object CreateFailureResult(Type responseType, Error error)
    {
        if (responseType == typeof(Result))
            return Result.Failure(error);

        var valueType = responseType.GetGenericArguments()[0];

        var method = typeof(Result)
            .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
            .MakeGenericMethod(valueType);

        return method.Invoke(null, [error])!;
    }
}
