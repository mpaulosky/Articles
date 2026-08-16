// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ObjectIdExtensionsBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.ValueObjects;

public class ObjectIdExtensionsBehaviorTests
{
	[Fact]
	public void TryParseObjectIdReturnsFalseForBlankOrInvalidValues()
	{
		// Arrange
		var validHex = "507f191e810c19729de860ea";

		// Act
		var valid = ObjectIdExtensions.TryParseObjectId(validHex, out var parsedId);
		var blank = ObjectIdExtensions.TryParseObjectId("   ", out var blankId);
		var invalid = ObjectIdExtensions.TryParseObjectId("not-an-object-id", out var invalidId);

		// Assert
		valid.Should().BeTrue();
		parsedId.Should().NotBe(ObjectId.Empty);
		blank.Should().BeFalse();
		blankId.Should().Be(ObjectId.Empty);
		invalid.Should().BeFalse();
		invalidId.Should().Be(ObjectId.Empty);
	}

	[Fact]
	public void ParseObjectIdThrowsForInvalidValues()
	{
		// Arrange
		var validHex = "507f191e810c19729de860ea";

		// Act
		Action act = () => ObjectIdExtensions.ParseObjectId(validHex);
		Action invalidAct = () => ObjectIdExtensions.ParseObjectId("invalid");

		// Assert
		act.Should().NotThrow();
		invalidAct.Should().Throw<FormatException>();
	}

	[Fact]
	public void DeterministicIdReturnsStableObjectIdsForSlotValues()
	{
		// Arrange

		// Act
		var first = ObjectIdExtensions.DeterministicId(1);
		var same = ObjectIdExtensions.DeterministicId(1);
		var large = ObjectIdExtensions.DeterministicId(123456789);

		// Assert
		first.Should().Be(same);
		first.ToString().Should().Be("000000000000000000000001");
		large.ToString().Should().HaveLength(24);
		large.ToString().Should().NotBe(first.ToString());
	}

	[Fact]
	public void DeterministicIdThrowsForSlotBelowOne()
	{
		// Arrange

		// Act
		Action act = () => ObjectIdExtensions.DeterministicId(0);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}