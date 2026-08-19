// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticlesMongoDbContextFactory.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Microsoft.EntityFrameworkCore;

namespace Web.Data;

/// <summary>
///     Creates configured MongoDB EF Core contexts for repositories and runtime wiring.
/// </summary>
public sealed class ArticlesMongoDbContextFactory : IDbContextFactory<ArticlesMongoDbContext>
{
	private readonly IConfiguration _configuration;

	/// <summary>
	///     Initializes a new instance of the <see cref="ArticlesMongoDbContextFactory" /> class.
	/// </summary>
	/// <param name="configuration">The application configuration.</param>
	public ArticlesMongoDbContextFactory(IConfiguration configuration)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	/// <inheritdoc />
	public ArticlesMongoDbContext CreateDbContext()
	{
		var connectionString = _configuration.GetConnectionString("articlesdb")
		                       ?? _configuration["MONGODB_CONNECTION_STRING"]
		                       ?? "mongodb://localhost:27017";
		var databaseName = _configuration["MONGODB_DATABASE_NAME"] ?? "articlesdb";

		var options = new DbContextOptionsBuilder<ArticlesMongoDbContext>()
			.UseMongoDB(connectionString, databaseName)
			.Options;

		return new ArticlesMongoDbContext(options);
	}

	/// <summary>
	///     Creates a configured instance of the data context.
	/// </summary>
	/// <returns>A configured MongoDB-backed context.</returns>
	public ArticlesMongoDbContext Create()
	{
		return CreateDbContext();
	}
}
