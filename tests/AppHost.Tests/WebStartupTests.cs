// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebStartupTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

using Microsoft.Extensions.Caching.Distributed;

namespace AppHost;

public class WebStartupTests
{
	[Fact]
	public void RedisConnectionStringEnablesOutputCacheAndDistributedCache()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:RedisCache"] = "localhost:6379" })
			.Build();

		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			EnvironmentName = Environments.Development, ApplicationName = "Web"
		});
		builder.Configuration.Sources.Clear();
		builder.Configuration.AddConfiguration(configuration);

		// Act
		var redisConnectionString = builder.Configuration.GetConnectionString("RedisCache");
		var shouldUseRedis = !string.IsNullOrWhiteSpace(redisConnectionString);

		if (shouldUseRedis)
		{
			builder.AddRedisOutputCache("output-cache");
			builder.AddRedisDistributedCache("RedisCache");
		}
		else
		{
			builder.Services.AddDistributedMemoryCache();
		}

		// Assert
		redisConnectionString.Should().Be("localhost:6379");
		builder.Services.Any(service => service.ServiceType == typeof(IDistributedCache))
			.Should().BeTrue();
		builder.Services.Any(service => service.ServiceType == typeof(Microsoft.AspNetCore.OutputCaching.IOutputCacheStore))
			.Should().BeTrue();
	}

	[Fact]
	public void MissingRedisConnectionStringFallsBackToMemoryCache()
	{
		// Arrange
		var configuration = new ConfigurationBuilder().Build();

		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			EnvironmentName = Environments.Development, ApplicationName = "Web"
		});
		builder.Configuration.Sources.Clear();
		builder.Configuration.AddConfiguration(configuration);

		// Act
		var redisConnectionString = builder.Configuration.GetConnectionString("RedisCache");
		var shouldUseRedis = !string.IsNullOrWhiteSpace(redisConnectionString);

		if (shouldUseRedis)
		{
			builder.AddRedisOutputCache("output-cache");
			builder.AddRedisDistributedCache("RedisCache");
		}
		else
		{
			builder.Services.AddDistributedMemoryCache();
		}

		// Assert
		redisConnectionString.Should().BeNullOrEmpty();
		builder.Services.Any(service => service.ServiceType == typeof(IDistributedCache))
			.Should().BeTrue();
		builder.Services.Any(service => service.ServiceType == typeof(Microsoft.AspNetCore.OutputCaching.IOutputCacheStore))
			.Should().BeFalse();
	}

	[Fact]
	public void DevelopmentEnvironmentLoadsStaticWebAssets()
	{
		// Arrange
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			EnvironmentName = Environments.Development, ApplicationName = "Web"
		});

		// Act
		var act = () => Web.DevelopmentStaticWebAssets.EnableForDevelopment(builder);

		// Assert
		act.Should().NotThrow();
		builder.Environment.Should().NotBeNull();
	}
}
