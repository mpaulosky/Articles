using System.Collections.Concurrent;

namespace Web.MyMediator;

public sealed class PipelineMediator(IServiceProvider provider) : IMediator
{
	private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), Type> WrapperTypeCache = new();

	public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var requestType = request.GetType();
		var wrapperType = WrapperTypeCache.GetOrAdd((requestType, typeof(TResponse)),
			key => typeof(RequestHandlerWrapper<,>).MakeGenericType(key.RequestType, key.ResponseType));

		var wrapper = (RequestHandlerBase<TResponse>)(Activator.CreateInstance(wrapperType)
			?? throw new InvalidOperationException($"Unable to create request handler wrapper for '{requestType}'."));

		return wrapper.Handle(request, provider, cancellationToken);
	}

	public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
		where TNotification : INotification
	{
		cancellationToken.ThrowIfCancellationRequested();

		var handlers = provider.GetServices<INotificationHandler<TNotification>>();
		await Task.WhenAll(handlers.Select(handler => handler.Handle(notification, cancellationToken))).ConfigureAwait(false);
	}
}
