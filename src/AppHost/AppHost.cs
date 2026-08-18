// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHost.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost
// =============================================

using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Configure resources
var redisCache = builder.AddRedisServices();
var mongoDb = builder.AddMongoDbServices();

// Web project with health check and resource dependencies
builder.AddProject<Projects.Web>("web")
	// Ensure the app binds to HTTPS on port 5137 to match the secure local dev profile.
	// Also, explicitly pass the Mongo connection string and database name to keep the Web app running
	// even when the AppHost is launched in watch mode and .NET is not resolving the project reference
	// the same way it does for the dashboard resource graph.
	//.WithEnvironment("ASPNETCORE_URLS", "https://localhost:5137")
	.WithEnvironment("ConnectionStrings__articlesdb", mongoDb)
	.WithEnvironment("ConnectionStrings__Server", mongoDb)
	.WithEnvironment("MONGODB_CONNECTION_STRING", mongoDb)
	.WithEnvironment("MONGODB_DATABASE_NAME", DatabaseName)
	.WithEnvironment("GITHUB_REPOSITORY", "mpaulosky/Articles")
	.WithEnvironment("GITHUB_REPOSITORY_URL", "https://github.com/mpaulosky/Articles.git")
	.WithExternalHttpEndpoints()
	.WithHttpHealthCheck("/health")
	.WithReference(redisCache)
	.WithReference(mongoDb);

builder.Build().Run();
