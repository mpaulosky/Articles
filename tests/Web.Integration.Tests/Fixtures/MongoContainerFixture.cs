// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoContainerFixture.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

namespace Web.Integration.Tests.Fixtures;

/// <summary>
///     Starts a single MongoDB container, shared across the whole test assembly, that individual
///     test classes create isolated databases against.
/// </summary>
public sealed class MongoContainerFixture : IAsyncLifetime
{
	// Matches the tag AppHost pins for local development (src/AppHost/DatabaseService.cs)
	// so integration tests validate against the same MongoDB version the app actually runs.
	private const string MongoImageTag = "mongo:8.2.12";

	private readonly MongoDbContainer _container = new MongoDbBuilder(MongoImageTag)
		.Build();

	/// <inheritdoc />
	public async ValueTask InitializeAsync()
	{
		await _container.StartAsync().ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _container.DisposeAsync().ConfigureAwait(false);
	}

	/// <summary>
	///     Creates a context against a database uniquely named for the calling test class, so
	///     parallel test collections never see each other's data despite sharing one container.
	/// </summary>
	/// <param name="databaseName">
	///     A name unique to the calling test class; callers typically pass
	///     <c>$"{nameof(MyTests)}-{Guid.NewGuid()}"</c>.
	/// </param>
	public ArticlesMongoDbContext CreateContext(string databaseName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseMongoDB(_container.GetConnectionString(), databaseName)
			.Options;

		return new ArticlesMongoDbContext(options);
	}
}
