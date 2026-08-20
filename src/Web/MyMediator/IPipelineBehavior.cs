namespace Web.MyMediator;

public interface IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	Task<TResponse> Handle(
		TRequest request,
		Func<TRequest, CancellationToken, Task<TResponse>> continuation,
		CancellationToken cancellationToken);
}
