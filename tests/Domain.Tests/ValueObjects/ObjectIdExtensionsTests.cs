// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ObjectIdExtensionsTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.ValueObjects;

public class ObjectIdExtensionsTests
{
	[Fact]
	public void TryParseObjectId_Valid24CharHex_ReturnsTrueAndSetsObjectId()
	{
		// Arrange
		const string validHex = "507f191e810c19729de860ea";

		// Act
		var result = ObjectIdExtensions.TryParseObjectId(validHex, out var parsedId);

		// Assert
		result.Should().BeTrue();
		parsedId.Should().Be(ObjectId.Parse(validHex));
		parsedId.Should().NotBe(ObjectId.Empty);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t\n")]
	public void TryParseObjectId_NullOrEmptyOrWhitespace_ReturnsFalseAndSetsEmptyObjectId(string? hex)
	{
		// Arrange

		// Act
		var result = ObjectIdExtensions.TryParseObjectId(hex, out var parsedId);

		// Assert
		result.Should().BeFalse();
		parsedId.Should().Be(ObjectId.Empty);
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("123")]
	[InlineData("507f191e810c19729de860e")]
	[InlineData("507f191e810c19729de860eaa")]
	[InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
	public void TryParseObjectId_InvalidHexOrWrongLength_ReturnsFalseAndSetsEmptyObjectId(string hex)
	{
		// Arrange

		// Act
		var result = ObjectIdExtensions.TryParseObjectId(hex, out var parsedId);

		// Assert
		result.Should().BeFalse();
		parsedId.Should().Be(ObjectId.Empty);
	}

	[Fact]
	public void ParseObjectId_Valid24CharHex_ReturnsParsedObjectId()
	{
		// Arrange
		const string validHex = "507f191e810c19729de860ea";

		// Act
		var parsedId = ObjectIdExtensions.ParseObjectId(validHex);

		// Assert
		parsedId.Should().Be(ObjectId.Parse(validHex));
		parsedId.ToString().Should().Be(validHex);
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("123")]
	[InlineData("507f191e810c19729de860e")]
	[InlineData("507f191e810c19729de860eaa")]
	[InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
	public void ParseObjectId_InvalidHexOrWrongLength_ThrowsFormatException(string hex)
	{
		// Arrange

		// Act
		Action act = () => ObjectIdExtensions.ParseObjectId(hex);

		// Assert
		act.Should().Throw<FormatException>();
	}

	[Fact]
	public void ParseObjectId_Null_ThrowsArgumentNullException()
	{
		// Arrange
		string? hex = null;

		// Act
		Action act = () => ObjectIdExtensions.ParseObjectId(hex!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void DeterministicId_ValidSlot_Generates24CharHex()
	{
		// Arrange
		const int slot = 1;

		// Act
		var id = ObjectIdExtensions.DeterministicId(slot);

		// Assert
		id.ToString().Should().Be("000000000000000000000001");
	}

	[Fact]
	public void DeterministicId_SameSlot_ReturnsEqualObjectIds()
	{
		// Arrange
		const int slot = 42;

		// Act
		var first = ObjectIdExtensions.DeterministicId(slot);
		var second = ObjectIdExtensions.DeterministicId(slot);

		// Assert
		first.Should().Be(second);
		first.ToString().Should().Be(second.ToString());
	}

	[Fact]
	public void DeterministicId_DifferentSlots_ReturnsDifferentObjectIds()
	{
		// Arrange
		const int slot1 = 1;
		const int slot2 = 2;

		// Act
		var first = ObjectIdExtensions.DeterministicId(slot1);
		var second = ObjectIdExtensions.DeterministicId(slot2);

		// Assert
		first.Should().NotBe(second);
		first.ToString().Should().NotBe(second.ToString());
	}

	[Theory]
	[InlineData(1, "000000000000000000000001")]
	[InlineData(10, "00000000000000000000000A")]
	[InlineData(16, "000000000000000000000010")]
	[InlineData(255, "0000000000000000000000FF")]
	[InlineData(123456789, "0000000000000000075BCD15")]
	[InlineData(9999999, "00000000000000000098967F")]
	public void DeterministicId_VariousValidSlots_GeneratesExpectedPaddedHex(int slot, string expectedHex)
	{
		// Arrange

		// Act
		var id = ObjectIdExtensions.DeterministicId(slot);

		// Assert
		id.ToString().Should().Be(expectedHex.ToLowerInvariant());
		id.ToString().Should().HaveLength(24);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	[InlineData(int.MinValue)]
	public void DeterministicId_ZeroOrNegativeSlot_ThrowsArgumentOutOfRangeException(int slot)
	{
		// Arrange

		// Act
		Action act = () => ObjectIdExtensions.DeterministicId(slot);

		// Assert
		act.Should().Throw<ArgumentOutOfRangeException>();
	}
}