namespace Web.MyMediator;

/// <summary>
///     Resolves the handler and pipeline behaviors for <typeparamref name="TRequest" /> and runs them, fully
///     type-checked at compile time. <see cref="PipelineMediator" /> selects which closed generic instantiation
///     of this wrapper to use reflectively, but never invokes members on it via <c>dynamic</c>.
/// </summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase<TResponse>
	where TRequest : IRequest<TResponse>
{
	public override async Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider,
		CancellationToken cancellationToken)
	{
		var typedRequest = (TRequest)request;
		var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
		var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse();

		Func<TRequest, CancellationToken, Task<TResponse>> pipeline = handler.Handle;

		foreach (var behavior in behaviors)
		{
			var next = pipeline;
			pipeline = (req, ct) => behavior.Handle(req, next, ct);
		}

		return await pipeline(typedRequest, cancellationToken).ConfigureAwait(false);
	}
}
