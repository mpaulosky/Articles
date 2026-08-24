// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageBackfillHostedServiceTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using System.Reflection;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Web.Data;

namespace Web.Tests.Data;

public class ArticleImageBackfillHostedServiceTests
{
	[Fact]
	public void ConstructorThrowsArgumentNullExceptionWhenContextFactoryIsNull()
	{
		// Act
		var act = () => new ArticleImageBackfillHostedService(null!,
			Substitute.For<ILogger<ArticleImageBackfillHostedService>>());

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void ConstructorThrowsArgumentNullExceptionWhenLoggerIsNull()
	{
		// Act
		var act = () => new ArticleImageBackfillHostedService(
			Substitute.For<IDbContextFactory<ArticlesMongoDbContext>>(), null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task ExecuteAsyncDoesNotThrowWhenTheContextFactoryFailsAsync()
	{
		// Arrange: simulates MongoDB being unreachable at startup (e.g. not yet up, or a
		// WebApplicationFactory-hosted test with no live database), which must not crash the host.
		var contextFactory = Substitute.For<IDbContextFactory<ArticlesMongoDbContext>>();
		contextFactory.CreateDbContextAsync(Arg.Any<CancellationToken>())
			.Returns<Task<ArticlesMongoDbContext>>(_ => throw new InvalidOperationException("no database"));
		var logger = Substitute.For<ILogger<ArticleImageBackfillHostedService>>();
		var service = new ArticleImageBackfillHostedService(contextFactory, logger);
		var executeAsync = typeof(ArticleImageBackfillHostedService)
			.GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

		// Act
		var act = () => (Task)executeAsync.Invoke(service, [TestContext.Current.CancellationToken])!;

		// Assert
		await act.Should().NotThrowAsync();
	}
}
