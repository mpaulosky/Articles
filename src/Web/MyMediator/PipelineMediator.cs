namespace Web.MyMediator;

public sealed class PipelineMediator(IServiceProvider provider) : IMediator
{
	public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		var requestType = request.GetType();
		var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
		var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));

		dynamic handler = provider.GetRequiredService(handlerType);
		var behaviors = provider.GetServices(behaviorType).Cast<object>().Reverse().ToList();

		Func<IRequest<TResponse>, CancellationToken, Task<TResponse>> pipeline =
			(req, ct) => handler.Handle((dynamic)req, ct);

		foreach (dynamic behavior in behaviors)
		{
			var next = pipeline;
			pipeline = (req, ct) => behavior.Handle((dynamic)req, ct, next);
		}

		return pipeline(request, cancellationToken);
	}

	public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
		where TNotification : INotification
	{
		cancellationToken.ThrowIfCancellationRequested();

		var handlers = provider.GetServices<INotificationHandler<TNotification>>();
		await Task.WhenAll(handlers.Select(handler => handler.Handle(notification, cancellationToken))).ConfigureAwait(false);
	}
}
