namespace Web.MyMediator;

public interface IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	Task<TResponse> Handle(
		TRequest request,
		CancellationToken cancellationToken,
		Func<TRequest, CancellationToken, Task<TResponse>> next);
}
