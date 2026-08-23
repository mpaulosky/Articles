// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostResourceModelTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace AppHost;

public class AppHostResourceModelTests
{
	private static async Task<DistributedApplication> BuildAppAsync()
	{
		var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>().ConfigureAwait(false);
		return await appHost.BuildAsync().ConfigureAwait(false);
	}

	[Fact]
	public async Task AppHostBuildsWithExpectedResources()
	{
		// Act
		await using var app = await BuildAppAsync();
		var model = app.Services.GetRequiredService<DistributedApplicationModel>();

		// Assert
		model.Resources.Select(static r => r.Name).Should().Contain(["RedisCache", "Server", "articlesdb", "web"]);
	}

	[Fact]
	public async Task WebResourceReferencesRedisAndMongoDb()
	{
		// Act
		await using var app = await BuildAppAsync();
		var model = app.Services.GetRequiredService<DistributedApplicationModel>();
		var web = model.Resources.Single(static r => r.Name == "web");

		// Assert
		var referencedResourceNames = web.Annotations
			.OfType<ResourceRelationshipAnnotation>()
			.Where(static a => a.Type == "Reference")
			.Select(static a => a.Resource.Name)
			.ToList();

		referencedResourceNames.Should().Contain(["RedisCache", "articlesdb"]);
	}

	[Fact]
	public async Task WebResourceHasExpectedEnvironmentVariables()
	{
		// Act
		await using var app = await BuildAppAsync();
		var model = app.Services.GetRequiredService<DistributedApplicationModel>();
		var web = model.Resources.OfType<ProjectResource>().Single(static r => r.Name == "web");

#pragma warning disable CS0618 // No non-obsolete replacement exists yet for resolving environment variables in tests.
		var config = await web.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
		var configDictionary = config.ToDictionary(static kv => kv.Key, static kv => kv.Value);

		// Assert
		configDictionary.Should().ContainKey("ConnectionStrings__articlesdb");
		configDictionary.Should().ContainKey("ConnectionStrings__Server");
		configDictionary.Should().ContainKey("MONGODB_CONNECTION_STRING");
		configDictionary["MONGODB_DATABASE_NAME"].Should().Be("articlesdb");
		configDictionary["GITHUB_REPOSITORY"].Should().Be("mpaulosky/Articles");
		configDictionary["GITHUB_REPOSITORY_URL"].Should().Be("https://github.com/mpaulosky/Articles.git");
	}

	[Fact]
	public async Task WebResourceHasHttpHealthCheckConfigured()
	{
		// Act
		await using var app = await BuildAppAsync();
		var model = app.Services.GetRequiredService<DistributedApplicationModel>();
		var web = model.Resources.Single(static r => r.Name == "web");

		// Assert
		web.Annotations.OfType<HealthCheckAnnotation>().Should().NotBeEmpty();
	}
}
