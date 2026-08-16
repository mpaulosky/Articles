// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     DatabaseServiceTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost;

public class DatabaseServiceTests
{
	[Fact]
	public void GetMongoDbResourceSettingsUsesLatestMongoImageWithoutPersistentVolume()
	{
		// Arrange
		// Act
		var settings = DatabaseService.GetMongoDbResourceSettings();

		// Assert
		settings.ImageTag.Should().Be("8.2.12");
		settings.DataVolumeName.Should().BeNull();
	}

	[Fact]
	public void AddMongoDbServicesAttachesMongoDevelopmentCommandsToServerResource()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var executionContextField = typeof(DistributedApplicationBuilder)
			.GetField("<ExecutionContext>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		executionContextField.Should().NotBeNull();
		executionContextField.SetValue(builder,
			new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run));

		// Act
		_ = builder.AddMongoDbServices();

		// Assert
		var serverResource = builder.Resources.OfType<MongoDBServerResource>()
			.Single(resource => resource.Name == AppHostConstants.Server);
		var commands = serverResource.Annotations.OfType<ResourceCommandAnnotation>().Select(annotation => annotation.Name)
			.ToList();

		commands.Should().Contain("clear-articles-data");
		commands.Should().Contain("seed-articles-data");
		commands.Should().Contain("show-articles-stats");
	}

	[Fact]
	public void AddMongoDbServicesSkipsDevelopmentCommandsOutsideRunMode()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var executionContextField = typeof(DistributedApplicationBuilder)
			.GetField("<ExecutionContext>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		executionContextField.Should().NotBeNull();
		executionContextField.SetValue(builder,
			new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish));

		// Act
		_ = builder.AddMongoDbServices();

		// Assert
		var serverResource = builder.Resources.OfType<MongoDBServerResource>()
			.Single(resource => resource.Name == AppHostConstants.Server);
		var commands = serverResource.Annotations.OfType<ResourceCommandAnnotation>().Select(annotation => annotation.Name)
			.ToList();

		commands.Should().BeEmpty();
	}
}