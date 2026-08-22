using FluentAssertions;

using Web.MyMediator;

namespace Web.Tests.MyMediator;

public class UnitTests
{
	[Fact]
	public void UnitValue_ComparedToAnotherUnit_ExpectedEqual()
	{
		// Arrange
		var left = Unit.Value;
		var right = default(Unit);

		// Act
		var equals = left.Equals(right);
		var objectEquals = left.Equals((object)right);
		var operatorEquals = left == right;

		// Assert
		left.Should().Be(right);
		equals.Should().BeTrue();
		objectEquals.Should().BeTrue();
		operatorEquals.Should().BeTrue();
		(left != right).Should().BeFalse();
	}

	[Fact]
	public void Equals_ObjectIsNonUnit_ExpectedFalse()
	{
		// Arrange
		var unit = Unit.Value;
		object value = new object();

		// Act
		var result = unit.Equals(value);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public void GetHashCode_WhenCalled_ExpectedZero()
	{
		// Arrange
		var unit = Unit.Value;

		// Act
		var hashCode = unit.GetHashCode();

		// Assert
		hashCode.Should().Be(0);
	}

	[Fact]
	public void ToString_WhenCalled_ExpectedParentheses()
	{
		// Arrange
		var unit = Unit.Value;

		// Act
		var result = unit.ToString();

		// Assert
		result.Should().Be("()");
	}

	[Fact]
	public void EqualityOperators_WhenComparedAcrossUnits_ExpectedAlwaysEqualAndNeverNotEqual()
	{
		// Arrange
		var left = Unit.Value;
		var right = default(Unit);

		// Act
		var equals = left == right;
		var notEquals = left != right;

		// Assert
		equals.Should().BeTrue();
		notEquals.Should().BeFalse();
	}
}
