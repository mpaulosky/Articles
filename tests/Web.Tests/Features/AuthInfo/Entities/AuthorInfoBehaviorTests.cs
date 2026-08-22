// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AuthorInfoBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using FluentAssertions;

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

using Web.Components.Features.AuthInfo.Entities;

namespace Web.Tests.Features.AuthInfo.Entities;

public class AuthorInfoBehaviorTests
{
	[Fact]
	public void EmptyReturnsExpectedState()
	{
		// Arrange

		// Act
		var result = AuthorDto.Empty;

		// Assert
		result.UserId.Should().BeEmpty();
		result.Name.Should().BeEmpty();
	}

	[Fact]
	public void ConstructorSetsProperties()
	{
		// Arrange
		const string userId = "auth0|12345";
		const string name = "John Doe";

		// Act
		var result = new AuthorDto(userId, name);

		// Assert
		result.UserId.Should().Be(userId);
		result.Name.Should().Be(name);
	}

	[Fact]
	public void PropertyEqualityWorks()
	{
		// Arrange
		var author1 = new AuthorDto("auth0|12345", "John Doe");
		var author2 = new AuthorDto("auth0|12345", "John Doe");
		var author3 = new AuthorDto("auth0|67890", "Jane Smith");

		// Act & Assert
		author1.Should().BeEquivalentTo(author2);
		author1.Should().NotBeEquivalentTo(author3);
		author1.UserId.Should().Be(author2.UserId);
		author1.Name.Should().Be(author2.Name);
		author1.UserId.Should().NotBe(author3.UserId);
	}

	[Fact]
	public void BsonSerializationRoundTrip()
	{
		// Arrange
		var original = new AuthorDto("auth0|12345", "John Doe");

		// Act
		var bson = original.ToBson();
		var deserialized = BsonSerializer.Deserialize<AuthorDto>(bson);

		// Assert
		deserialized.Should().BeEquivalentTo(original);
		deserialized.UserId.Should().Be(original.UserId);
		deserialized.Name.Should().Be(original.Name);
		deserialized.Email.Should().Be(original.Email);
	}
}
