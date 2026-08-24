// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageBackfillMigrationTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using MongoDB.Driver;

using Web.Components.Features.Articles.Entities;
using Web.Components.Features.AuthInfo.Entities;
using Web.Integration.Tests.Fixtures;

namespace Web.Integration.Tests.Data;

[Collection(MongoTestCollectionDefinition.Name)]
public class ArticleImageBackfillMigrationTests
{
	private readonly MongoContainerFixture _fixture;

	public ArticleImageBackfillMigrationTests(MongoContainerFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task RunAsyncPopulatesArticleImagesForDocumentsMissingTheFieldAgainstTheRealMongoContainerAsync()
	{
		// Arrange: persist an article, then strip its "articleImages" field to simulate a
		// document written before that field existed, per ADR-0003.
		var databaseName = $"ArticleImageBackfill-{Guid.NewGuid()}";
		await using var context = _fixture.CreateContext(databaseName);
		var article = Article.Create("Post", "![alt](https://example.com/uploads/a1b2.jpg)",
			new AuthorDto("author-1", "Ada", "ada@example.com"));
		await context.Articles.AddAsync(article, TestContext.Current.CancellationToken);
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		var collection = new MongoClient(_fixture.ConnectionString)
			.GetDatabase(databaseName)
			.GetCollection<BsonDocument>("articles");
		await collection.UpdateOneAsync(
			Builders<BsonDocument>.Filter.Eq("_id", article.Id),
			Builders<BsonDocument>.Update.Unset("articleImages"),
			cancellationToken: TestContext.Current.CancellationToken);

		// Act
		await using var migrationContext = _fixture.CreateContext(databaseName);
		await ArticleImageBackfillMigration.RunAsync(migrationContext, TestContext.Current.CancellationToken);

		// Assert
		await using var verifyContext = _fixture.CreateContext(databaseName);
		var stored = await verifyContext.Articles.AsNoTracking()
			.FirstAsync(a => a.Id == article.Id, TestContext.Current.CancellationToken);
		stored.ArticleImages.Should().ContainSingle(image => image.FileName == "a1b2.jpg");
	}
}
