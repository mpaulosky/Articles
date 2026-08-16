// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     PostAuthorBehaviorTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain.Tests
// =============================================

namespace Domain.ValueObjects;

public class PostAuthorBehaviorTests
{
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
}