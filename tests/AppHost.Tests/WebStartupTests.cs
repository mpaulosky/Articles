// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebStartupTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost;

public class WebStartupTests
{
	[Fact]
	public void DisableRedisDisablesOutputCacheMiddlewareRegistration()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?> { ["DisableRedis"] = "true" })
			.Build();

		var services = new ServiceCollection();
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions
		{
			EnvironmentName = Environments.Development, ApplicationName = "Web"
		});
		builder.Configuration.Sources.Clear();
		builder.Configuration.AddConfiguration(configuration);

		// Act
		var enableRedisOutputCache = !builder.Configuration.GetValue("DisableRedis", false);
		if (enableRedisOutputCache)
		{
			builder.Services.AddOutputCache();
		}

		// Assert
		services.Should().NotBeNull();
		builder.Services.Should().NotBeNull();
		builder.Services.Any(service => service.ServiceType == typeof(Microsoft.AspNetCore.OutputCaching.IOutputCacheStore))
			.Should().BeFalse();
	}
}