// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoContainerFixtureTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.AuthInfo.Entities;

namespace Web.Integration.Tests.Fixtures;

[Collection(MongoTestCollectionDefinition.Name)]
public class MongoContainerFixtureTests
{
	private readonly MongoContainerFixture _fixture;

	public MongoContainerFixtureTests(MongoContainerFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task CreateContext_RoundTripsAnArticle_AgainstTheRealMongoContainerAsync()
	{
		// Arrange
		var databaseName = $"{nameof(MongoContainerFixtureTests)}-{Guid.NewGuid()}";
		await using var context = _fixture.CreateContext(databaseName);
		var article = Article.Create("Title", "Content", new AuthorDto("user-1", "Author", "author@example.com"));

		// Act
		await context.Articles.AddAsync(article, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		await using var readContext = _fixture.CreateContext(databaseName);
		var stored = await readContext.Articles
			.AsNoTracking()
			.SingleAsync(a => a.Id == article.Id, TestContext.Current.CancellationToken);

		// Assert
		stored.Title.Should().Be("Title");
	}
}
