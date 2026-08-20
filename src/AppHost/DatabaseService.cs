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
			// Also keep local-dev data ephemeral so restarted AppHost instances do not inherit stale root credentials
			// or database state from an earlier run. Reusing a fixed named volume can leave behind auth state that
			// makes the health probe fail even though the Mongo container itself is up.
			return ("8.2.12", null);
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

		var server = builder.AddMongoDB(AppHostConstants.Server)
			.WithImage("mongo", mongoImageTag);

		if (!string.IsNullOrWhiteSpace(mongoDataVolumeName))
			server = server.WithDataVolume(mongoDataVolumeName, isReadOnly: false);

		server = server.WithMongoExpress();

		var database = server.AddDatabase(AppHostConstants.DatabaseName);
		server.WithMongoDbDevCommands();

		return database;
	}
}
