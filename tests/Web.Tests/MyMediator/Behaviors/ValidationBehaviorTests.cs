using FluentAssertions;

using FluentValidation;

using Web.MyMediator;
using Web.MyMediator.Behaviors;

namespace Web.Tests.MyMediator.Behaviors;

public class ValidationBehaviorTests
{
	[Fact]
	public async Task HandleInvokesContinuationWhenNoValidatorsAreRegistered()
	{
		// Arrange
		var behavior = new ValidationBehavior<PingRequest, string>([]);

		// Act
		var result = await behavior.Handle(
			new PingRequest(""),
			(req, _) => Task.FromResult($"pong:{req.Message}"),
			TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("pong:");
	}

	[Fact]
	public async Task HandleInvokesContinuationWhenAllValidatorsPass()
	{
		// Arrange
		var behavior = new ValidationBehavior<PingRequest, string>([new PingRequestValidator()]);

		// Act
		var result = await behavior.Handle(
			new PingRequest("hello"),
			(req, _) => Task.FromResult($"pong:{req.Message}"),
			TestContext.Current.CancellationToken);

		// Assert
		result.Should().Be("pong:hello");
	}

	[Fact]
	public async Task HandleThrowsValidationExceptionAggregatingFailuresFromAllValidators()
	{
		// Arrange
		var behavior = new ValidationBehavior<PingRequest, string>(
			[new PingRequestValidator(), new PingRequestAlwaysFailsValidator()]);

		// Act
		Func<Task> act = async () => await behavior.Handle(
			new PingRequest(""),
			(req, _) => Task.FromResult($"pong:{req.Message}"),
			TestContext.Current.CancellationToken).ConfigureAwait(false);

		// Assert
		var exception = await act.Should().ThrowAsync<ValidationException>();
		exception.Which.Errors.Should().HaveCount(2);
	}

	[Fact]
	public async Task HandleThrowsArgumentNullExceptionWhenContinuationIsNull()
	{
		// Arrange
		var behavior = new ValidationBehavior<PingRequest, string>([]);

		// Act
		Func<Task> act = async () => await behavior.Handle(
			new PingRequest("hello"),
			null!,
			TestContext.Current.CancellationToken).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<ArgumentNullException>();
	}

	private sealed record PingRequest(string Message) : IRequest<string>;

	private sealed class PingRequestValidator : AbstractValidator<PingRequest>
	{
		public PingRequestValidator()
		{
			RuleFor(request => request.Message).NotEmpty();
		}
	}

	private sealed class PingRequestAlwaysFailsValidator : AbstractValidator<PingRequest>
	{
		public PingRequestAlwaysFailsValidator()
		{
			RuleFor(request => request.Message).Must(_ => false).WithMessage("Always fails");
		}
	}
}
