namespace Web.MyMediator.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(
		TRequest request,
		CancellationToken cancellationToken,
		Func<TRequest, CancellationToken, Task<TResponse>> next)
	{
		logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
		var response = await next(request, cancellationToken).ConfigureAwait(false);
		logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
		return response;
	}
}
