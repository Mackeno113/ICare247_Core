// File    : ValidationBehavior.cs
// Module  : CQRS Pipeline
// Layer   : Application
// Purpose : MediatR pipeline behavior — tự động validate request bằng FluentValidation trước khi tới handler.

using FluentValidation;
using MediatR;

namespace ICare247.Application.Behaviors;

/// <summary>
/// Pipeline behavior tự động chạy FluentValidation validators cho mọi MediatR request.
/// Nếu có validation error → throw <see cref="ValidationException"/> (bắt bởi ExceptionHandlingMiddleware → 400).
/// Nếu không có validator → pass through.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Không có validator → pass through
        if (!_validators.Any())
            return await next(cancellationToken);

        // Chạy tất cả validators song song
        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Gom tất cả lỗi
        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        // Có lỗi → throw (ExceptionHandlingMiddleware bắt → 400 ProblemDetails)
        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
