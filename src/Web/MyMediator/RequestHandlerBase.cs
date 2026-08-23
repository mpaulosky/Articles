namespace Web.MyMediator;

/// <summary>
///     Non-generic-in-<c>TRequest</c> seam that lets <see cref="PipelineMediator" /> invoke a handler and its
///     pipeline behaviors for a request type it only discovers at runtime, without resorting to <c>dynamic</c>.
/// </summary>
internal abstract class RequestHandlerBase<TResponse>
{
	public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider,
		CancellationToken cancellationToken);
}
