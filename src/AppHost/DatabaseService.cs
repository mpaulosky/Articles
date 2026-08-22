// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DatabaseService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : Articles
// Project Name :  AppHost
// =======================================================

namespace AppHost;

/// <summary>
///   Extension methods for adding and configuring MongoDB resources with Aspire features.
/// </summary>
public static class DatabaseService
{
	/// <summary>
	///   Gets the MongoDB container settings used by the AppHost for local development.
	/// </summary>
	public static (string ImageTag, string? DataVolumeName) MongoDbResourceSettings
	{
		get
		{
			// Keep the MongoDB image pinned to a known-good tag so local AppHost startup stays deterministic.
			// Persist local-dev data in a named volume so seeded categories/articles survive AppHost restarts.
			// This is safe to reuse across runs only because AddMongoDbServices pins the root username/password
			// below — MongoDB only reads MONGO_INITDB_ROOT_* on the very first startup against an empty data
			// directory, so a fresh, randomly generated password on a later run would otherwise no longer match
			// the credentials already initialized in the volume and the health probe would fail.
			return ("8.2.12", "articles-mongo-data");
		}
	}

	/// <summary>
	///   Adds MongoDB services to the distributed application builder, including resource tagging, grouping, and improved
	///   seeding logic.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns>The MongoDB database resource builder.</returns>
	public static IResourceBuilder<MongoDBDatabaseResource> AddMongoDbServices(
		this IDistributedApplicationBuilder builder)
	{
		var (mongoImageTag, mongoDataVolumeName) = MongoDbResourceSettings;

		// Fixed, non-secret local-dev credentials — required so the root user stays valid across AppHost
		// restarts when the data volume below is reused. See the comment on MongoDbResourceSettings.
		var mongoUserName = builder.AddParameter("mongo-username", "mongoadmin");
		var mongoPassword = builder.AddParameter("mongo-password", "articles-local-dev", secret: true);

		var server = builder.AddMongoDB(AppHostConstants.Server, userName: mongoUserName, password: mongoPassword)
			.WithImage("mongo", mongoImageTag);

		if (!string.IsNullOrWhiteSpace(mongoDataVolumeName))
			server = server.WithDataVolume(mongoDataVolumeName, isReadOnly: false);

		server = server.WithMongoExpress();

		var database = server.AddDatabase(AppHostConstants.DatabaseName);
		server.WithMongoDbDevCommands();

		return database;
	}
}
