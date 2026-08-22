using FluentAssertions;

using Web.MyMediator;

namespace Web.Tests.MyMediator;

public class UnitTests
{
	[Fact]
	public void Value_IsSingletonLikeAndHasStableEquality()
	{
		// Arrange
		var left = Unit.Value;
		var right = Unit.Value;

		// Act
		var areEqual = left == right;
		var objectEquals = left.Equals((object)right);

		// Assert
		left.Should().Be(right);
		areEqual.Should().BeTrue();
		objectEquals.Should().BeTrue();
		left.GetHashCode().Should().Be(right.GetHashCode());
	}

	[Fact]
	public void ToString_ReturnsEmptyCallSyntaxRepresentation()
	{
		// Arrange
		var value = Unit.Value;

		// Act
		var text = value.ToString();

		// Assert
		text.Should().Be("()");
	}

	[Fact]
	public void EqualityAndInequality_AreAlwaysTrueForEquivalentValues()
	{
		// Arrange
		var first = Unit.Value;
		var second = Unit.Value;

		// Act
		var isEqual = first == second;
		var isNotEqual = first != second;

		// Assert
		isEqual.Should().BeTrue();
		isNotEqual.Should().BeFalse();
	}
}
