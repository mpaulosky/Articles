// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ResultBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.Abstractions;

public class ResultBehaviorTests
{
	[Fact]
	public void OkAndFailExposeExpectedState()
	{
		// Arrange
		var ok = Result.Ok();
		var fail = Result.Fail("boom", ResultErrorCode.Validation, "details");

		// Act
		var genericOk = Result.Ok("value");
		var genericFail = Result<string>.Fail("missing", ResultErrorCode.NotFound);

		// Assert
		ok.Success.Should().BeTrue();
		ok.Failure.Should().BeFalse();
		fail.Success.Should().BeFalse();
		fail.Failure.Should().BeTrue();
		fail.Error.Should().Be("boom");
		fail.ErrorCode.Should().Be(ResultErrorCode.Validation);
		fail.Details.Should().Be("details");
		genericOk.Success.Should().BeTrue();
		genericOk.Value.Should().Be("value");
		genericFail.Success.Should().BeFalse();
		genericFail.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		genericFail.Value.Should().BeNull();
	}

	[Fact]
	public void FromValueHandlesNullAndImplicitConversion()
	{
		// Arrange
		var valueResult = Result.FromValue("hello");
		var nullResult = Result.FromValue<string>(null);

		// Act
		string? convertedValue = valueResult;
		string? convertedNull = nullResult;

		// Assert
		valueResult.Success.Should().BeTrue();
		convertedValue.Should().Be("hello");
		nullResult.Success.Should().BeFalse();
		convertedNull.Should().BeNull();
		nullResult.Error.Should().Be("Provided value is null.");
	}

	[Fact]
	public void GenericHelpersExerciseAdditionalFactoryAndConversionPaths()
	{
		// Arrange
		var numericResult = Result<int>.FromValue(7);
		var nullNumericResult = Result<int?>.FromValue(null);

		// Act
		Result<int?> implicitResult = 42;
		int? convertedFromResult = numericResult;
		int? convertedFromImplicitResult = implicitResult;
		var toValue = numericResult.ToValue();

		// Assert
		numericResult.Success.Should().BeTrue();
		toValue.Should().Be(7);
		convertedFromResult.Should().Be(7);
		implicitResult.Success.Should().BeTrue();
		convertedFromImplicitResult.Should().Be(42);
		nullNumericResult.Success.Should().BeFalse();
		nullNumericResult.Error.Should().Be("Value cannot be null.");
	}

	[Fact]
	public void FailOverloadsPreserveErrorCodeAndDetails()
	{
		// Arrange
		const string errorMessage = "boom";
		var details = new { Id = 42, Reason = "missing" };

		// Act
		var simpleFail = Result.Fail(errorMessage);
		var genericFail = Result<int>.Fail(errorMessage, ResultErrorCode.Conflict, details);

		// Assert
		simpleFail.Success.Should().BeFalse();
		simpleFail.Error.Should().Be(errorMessage);
		simpleFail.ErrorCode.Should().Be(ResultErrorCode.None);
		genericFail.Success.Should().BeFalse();
		genericFail.Error.Should().Be(errorMessage);
		genericFail.ErrorCode.Should().Be(ResultErrorCode.Conflict);
		genericFail.Details.Should().BeEquivalentTo(details);
	}

	[Fact]
	public void UntypedFailAndNullGenericConversionFollowExpectedPaths()
	{
		// Arrange
		var fail = Result.Fail("forbidden", ResultErrorCode.Unauthorized);
		Result<string>? nullResult = null;

		// Act
		string? convertedNull = nullResult;

		// Assert
		fail.Success.Should().BeFalse();
		fail.Error.Should().Be("forbidden");
		fail.ErrorCode.Should().Be(ResultErrorCode.Unauthorized);
		convertedNull.Should().BeNull();
	}
}