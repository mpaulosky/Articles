// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     PostAuthorBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Domain.ValueObjects;

public class PostAuthorBehaviorTests
{
	[Fact]
	public void EmptyReturnsExpectedDefaultValues()
	{
		// Arrange & Act
		var empty = PostAuthor.Empty;

		// Assert
		empty.Id.Should().BeEmpty();
		empty.Name.Should().BeEmpty();
		empty.Email.Should().BeEmpty();
		empty.Roles.Should().BeEmpty();
	}

	[Fact]
	public void EmptyReturnsStableInstance()
	{
		// Arrange & Act
		var empty1 = PostAuthor.Empty;
		var empty2 = PostAuthor.Empty;

		// Assert
		empty1.Should().BeSameAs(empty2);
	}

	[Fact]
	public void CreateSetsExpectedState()
	{
		// Arrange
		var roles = new[] { "admin", "editor" };

		// Act
		var author = new PostAuthor("author-1", "Ada Lovelace", "ada@example.com", roles);

		// Assert
		author.Id.Should().Be("author-1");
		author.Name.Should().Be("Ada Lovelace");
		author.Email.Should().Be("ada@example.com");
		author.Roles.Should().Equal(roles);
	}

	[Theory]
	[InlineData("user-1", "Alice Smith", "alice@example.com")]
	[InlineData("user-2", "Bob Jones", "bob@example.com")]
	[InlineData("auth0|12345", "Carol White", "carol@example.org")]
	public void ConstructorSetsScalarProperties(string id, string name, string email)
	{
		// Arrange
		var roles = new[] { "Author", "User" };

		// Act
		var author = new PostAuthor(id, name, email, roles);

		// Assert
		author.Id.Should().Be(id);
		author.Name.Should().Be(name);
		author.Email.Should().Be(email);
		author.Roles.Should().Equal(roles);
	}

	[Fact]
	public void DeconstructExtractsAllProperties()
	{
		// Arrange
		var roles = new[] { "Admin" };
		var author = new PostAuthor("user-123", "Grace Hopper", "grace@navy.mil", roles);

		// Act
		var (id, name, email, extractedRoles) = author;

		// Assert
		id.Should().Be("user-123");
		name.Should().Be("Grace Hopper");
		email.Should().Be("grace@navy.mil");
		extractedRoles.Should().Equal(roles);
	}

	[Fact]
	public void WithExpressionCreatesModifiedCopy()
	{
		// Arrange
		var roles = new[] { "Contributor" };
		var original = new PostAuthor("auth0|999", "Original Name", "original@example.com", roles);

		// Act
		var updated = original with { Name = "Updated Name", Email = "updated@example.com" };

		// Assert
		updated.Id.Should().Be(original.Id);
		updated.Name.Should().Be("Updated Name");
		updated.Email.Should().Be("updated@example.com");
		updated.Roles.Should().Equal(roles);

		// Original should remain unchanged
		original.Name.Should().Be("Original Name");
		original.Email.Should().Be("original@example.com");
	}

	[Fact]
	public void PropertyEqualityEquivalentObjectsMatch()
	{
		// Arrange
		var roles = new[] { "Admin", "Writer" };
		var author1 = new PostAuthor("user-1", "Alan Turing", "alan@example.com", roles);
		var author2 = new PostAuthor("user-1", "Alan Turing", "alan@example.com", roles);
		var author3 = new PostAuthor("user-2", "Alan Turing", "alan@example.com", roles);

		// Act & Assert
		author1.Should().BeEquivalentTo(author2);
		author1.Should().NotBeEquivalentTo(author3);
		author1.Id.Should().Be(author2.Id);
		author1.Name.Should().Be(author2.Name);
		author1.Email.Should().Be(author2.Email);
		author1.Roles.Should().Equal(author2.Roles);
	}

	[Fact]
	public void BsonSerializationRoundTripPreservesAllProperties()
	{
		// Arrange
		var roles = new[] { "Admin", "Editor" };
		var original = new PostAuthor("auth0|12345", "Margaret Hamilton", "margaret@nasa.gov", roles);

		// Act
		var bson = original.ToBson();
		var deserialized = BsonSerializer.Deserialize<PostAuthor>(bson);

		// Assert
		deserialized.Should().NotBeNull();
		deserialized.Should().BeEquivalentTo(original);
		deserialized.Id.Should().Be(original.Id);
		deserialized.Name.Should().Be(original.Name);
		deserialized.Email.Should().Be(original.Email);
		deserialized.Roles.Should().Equal(original.Roles);
	}
}
