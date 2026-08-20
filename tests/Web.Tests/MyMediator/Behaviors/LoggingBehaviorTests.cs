using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Web.MyMediator;
using Web.MyMediator.Behaviors;

namespace Web.Tests.MyMediator.Behaviors;

public class LoggingBehaviorTests
{
	[Fact]
	public async Task HandleInvokesContinuationAndReturnsItsResponse()
	{
		// Arrange
		var logger = Substitute.For<ILogger<LoggingBehavior<PingRequest, string>>>();
		var behavior = new LoggingBehavior<PingRequest, string>(logger);
		var request = new PingRequest("hello");

		// Act
		var result = await behavior.Handle(
			request,
			(req, _) => Task.FromResult($"pong:{req.Message}"),
			TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("pong:hello");
	}

	[Fact]
	public async Task HandleThrowsArgumentNullExceptionWhenContinuationIsNull()
	{
		// Arrange
		var logger = Substitute.For<ILogger<LoggingBehavior<PingRequest, string>>>();
		var behavior = new LoggingBehavior<PingRequest, string>(logger);

		// Act
		Func<Task> act = async () => await behavior.Handle(
			new PingRequest("hello"),
			null!,
			TestContext.Current.CancellationToken).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	// Generic argument of ILogger<LoggingBehavior<PingRequest, string>>, so NSubstitute's Castle proxy
	// needs it to be accessible - it cannot be a private nested type.
	public sealed record PingRequest(string Message) : IRequest<string>;
}
