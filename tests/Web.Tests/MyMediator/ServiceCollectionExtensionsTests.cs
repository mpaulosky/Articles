using FluentAssertions;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using Web.MyMediator;
using Web.MyMediator.Behaviors;

namespace Web.Tests.MyMediator;

public class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddMyMediatorThrowsArgumentNullExceptionWhenAssemblyIsNull()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		var act = () => services.AddMyMediator(null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void AddMyMediatorRegistersMediatorRequestHandlersNotificationHandlersAndValidators()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddMyMediator(typeof(ServiceCollectionExtensionsTests).Assembly);
		var provider = services.BuildServiceProvider();

		// Assert
		provider.GetService<IMediator>().Should().BeOfType<PipelineMediator>();
		provider.GetService<IRequestHandler<PingRequest, string>>().Should().BeOfType<PingHandler>();
		provider.GetService<INotificationHandler<PingNotification>>().Should().BeOfType<PingNotificationHandler>();
		provider.GetService<IValidator<PingRequest>>().Should().BeOfType<PingRequestValidator>();
	}

	[Fact]
	public void AddMyMediatorSkipsBuiltInBehaviorsNamespaceWhenScanningPipelineBehaviors()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddMyMediator(typeof(LoggingBehavior<,>).Assembly);

		// Assert
		services.Should().NotContain(
			descriptor => descriptor.ImplementationType == typeof(LoggingBehavior<,>));
	}

	private sealed record PingRequest(string Message) : IRequest<string>;

	private sealed class PingHandler : IRequestHandler<PingRequest, string>
	{
		public Task<string> Handle(PingRequest request, CancellationToken cancellationToken)
		{
			return Task.FromResult($"pong:{request.Message}");
		}
	}

	private sealed record PingNotification(string Message) : INotification;

	private sealed class PingNotificationHandler : INotificationHandler<PingNotification>
	{
		public Task Handle(PingNotification notification, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}

	private sealed class PingRequestValidator : AbstractValidator<PingRequest>
	{
		public PingRequestValidator()
		{
			RuleFor(request => request.Message).NotEmpty();
		}
	}
}
