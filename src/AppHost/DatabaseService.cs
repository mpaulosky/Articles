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
			return ("8.2.12", "articles-mongo-data");
		}
	}

	/// <summary>
	///   Gets the configured MongoDB root password used for local development.
	/// </summary>
	/// <returns>The MongoDB root password, or the default development password when no environment override is supplied.</returns>
	public static string GetMongoRootPassword()
	{
		return Environment.GetEnvironmentVariable("MONGO_INITDB_ROOT_PASSWORD")
			?? "articles-local-dev";
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
		var mongoUserName = builder.AddParameter("mongo-username", "mongoadmin");
		var mongoPassword = builder.AddParameter("mongo-password", GetMongoRootPassword(), secret: true);

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
