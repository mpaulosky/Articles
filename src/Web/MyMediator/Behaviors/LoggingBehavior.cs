namespace Web.MyMediator.Behaviors;

public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	public async Task<TResponse> Handle(
		TRequest request,
		Func<TRequest, CancellationToken, Task<TResponse>> continuation,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(continuation);

		LogHandling(typeof(TRequest).Name);
		var response = await continuation(request, cancellationToken).ConfigureAwait(false);
		LogHandled(typeof(TRequest).Name);
		return response;
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
	private partial void LogHandling(string requestName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName}")]
	private partial void LogHandled(string requestName);
}
