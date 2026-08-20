using FluentValidation;
using FluentValidation.Results;

namespace Web.MyMediator.Behaviors;

/// <summary>
///     Runs all registered <see cref="IValidator{T}" /> instances for <typeparamref name="TRequest" /> before the
///     handler executes, throwing <see cref="ValidationException" /> on failure. Not registered by default in this
///     application: request handlers already validate via an injected <see cref="IValidator{T}" /> and return
///     <c>Result.Fail</c>, and no caller currently catches <see cref="ValidationException" />.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(
		TRequest request,
		Func<TRequest, CancellationToken, Task<TResponse>> continuation,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(continuation);

		var failures = new List<ValidationFailure>();
		foreach (var validator in validators)
		{
			var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
			failures.AddRange(result.Errors);
		}

		if (failures.Count > 0)
		{
			throw new ValidationException(failures);
		}

		return await continuation(request, cancellationToken).ConfigureAwait(false);
	}
}
