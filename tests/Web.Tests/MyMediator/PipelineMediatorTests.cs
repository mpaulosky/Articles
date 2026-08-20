using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Web.MyMediator;

namespace Web.Tests.MyMediator;

public class PipelineMediatorTests
{
	[Fact]
	public async Task SendInvokesHandlerWhenNoBehaviorsAreRegistered()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddScoped<IRequestHandler<PingRequest, string>, PingHandler>();
		var provider = services.BuildServiceProvider();
		var mediator = new PipelineMediator(provider);

		// Act
		var result = await mediator.Send(new PingRequest("hello"), TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("pong:hello");
	}

	[Fact]
	public async Task SendRunsRegisteredBehaviorsInOrderAroundTheHandler()
	{
		// Arrange
		var callOrder = new List<string>();
		var services = new ServiceCollection();
		services.AddScoped<IRequestHandler<PingRequest, string>, PingHandler>();
		services.AddSingleton<IPipelineBehavior<PingRequest, string>>(new RecordingBehavior("first", callOrder));
		services.AddSingleton<IPipelineBehavior<PingRequest, string>>(new RecordingBehavior("second", callOrder));
		var provider = services.BuildServiceProvider();
		var mediator = new PipelineMediator(provider);

		// Act
		var result = await mediator.Send(new PingRequest("hello"), TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("pong:hello");
		callOrder.Should().ContainInOrder("first:before", "second:before", "second:after", "first:after");
	}

	[Fact]
	public async Task SendThrowsArgumentNullExceptionWhenRequestIsNull()
	{
		// Arrange
		var provider = new ServiceCollection().BuildServiceProvider();
		var mediator = new PipelineMediator(provider);

		// Act
		Func<Task> act = async () => await mediator.Send<string>(null!, TestContext.Current.CancellationToken).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	[Fact]
	public async Task PublishInvokesAllRegisteredNotificationHandlers()
	{
		// Arrange
		var received = new List<string>();
		var services = new ServiceCollection();
		services.AddSingleton<INotificationHandler<PingNotification>>(new RecordingNotificationHandler("handler-1", received));
		services.AddSingleton<INotificationHandler<PingNotification>>(new RecordingNotificationHandler("handler-2", received));
		var provider = services.BuildServiceProvider();
		var mediator = new PipelineMediator(provider);

		// Act
		await mediator.Publish(new PingNotification("hi"), TestContext.Current.CancellationToken);

		// Assert
		received.Should().BeEquivalentTo(["handler-1:hi", "handler-2:hi"]);
	}

	[Fact]
	public async Task PublishThrowsWhenCancellationIsAlreadyRequested()
	{
		// Arrange
		var provider = new ServiceCollection().BuildServiceProvider();
		var mediator = new PipelineMediator(provider);
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Act
		Func<Task> act = async () => await mediator.Publish(new PingNotification("hi"), cts.Token).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	// PipelineMediator.Send dispatches through `dynamic`, so the C# runtime binder must be able to see these
	// types from the Web assembly's call site - they cannot be private nested classes.
	public sealed record PingRequest(string Message) : IRequest<string>;

	public sealed class PingHandler : IRequestHandler<PingRequest, string>
	{
		public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(request);

			return Task.FromResult($"pong:{request.Message}");
		}
	}

	public sealed class RecordingBehavior(string name, List<string> callOrder) : IPipelineBehavior<PingRequest, string>
	{
		public async Task<string> Handle(
			PingRequest request,
			Func<PingRequest, CancellationToken, Task<string>> continuation,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(continuation);

			callOrder.Add($"{name}:before");
			var response = await continuation(request, cancellationToken).ConfigureAwait(false);
			callOrder.Add($"{name}:after");
			return response;
		}
	}

	private sealed record PingNotification(string Message) : INotification;

	private sealed class RecordingNotificationHandler(string name, List<string> received) : INotificationHandler<PingNotification>
	{
		public Task Handle(PingNotification notification, CancellationToken cancellationToken)
		{
			received.Add($"{name}:{notification.Message}");
			return Task.CompletedTask;
		}
	}
}
