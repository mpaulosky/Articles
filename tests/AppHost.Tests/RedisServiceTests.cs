// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RedisServiceTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost.Tests
// =============================================

using Microsoft.Extensions.Logging;

namespace AppHost;

public class RedisServiceTests
{
	[Fact]
	public void GetRedisResourceSettingsUsesRedisLatestImageAndPasswordParameter()
	{
		// Arrange
		// Act
		var settings = RedisService.GetRedisResourceSettings();

		// Assert
		settings.ImageName.Should().Be("redis");
		settings.ImageTag.Should().Be("latest");
		settings.PasswordParameterName.Should().Be("redis-password");
	}

	[Fact]
	public void AddRedisServicesRegistersTheRedisCacheResourceAndClearCacheCommand()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		builder.Configuration["Redis:Password"] = "overridden";

		// Act
		_ = builder.AddRedisServices();

		// Assert
		var resource = builder.Resources.OfType<RedisResource>()
			.Single(resource => resource.Name == AppHostConstants.RedisCache);
		var command = resource.Annotations.OfType<ResourceCommandAnnotation>()
			.Single(annotation => annotation.Name == "clear-cache");

		command.DisplayName.Should().Be("Clear Cache");
		command.ConfirmationMessage.Should().Be("Are you sure you want to clear the cache?");
		command.DisplayDescription.Should().Be("This command will clear all cached data in the Redis cache.");
	}

	[Fact]
	public void ClearCacheCommandUpdatesStateWithoutLoggingWhenInformationIsDisabled()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		_ = builder.AddRedisServices();
		var resource = builder.Resources.OfType<RedisResource>()
			.Single(resource => resource.Name == AppHostConstants.RedisCache);
		var command = resource.Annotations.OfType<ResourceCommandAnnotation>()
			.Single(annotation => annotation.Name == "clear-cache");
		var snapshot = CreateSnapshot();
		var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
		var context =
			CreateUpdateCommandStateContext(snapshot.WithHealthReports(ImmutableArray<HealthReportSnapshot>.Empty),
				serviceProvider);

		// Act
		var state = command.UpdateState(context);

		// Assert
		state.Should().Be(ResourceCommandState.Enabled);
	}

	[Fact]
	public void ClearCacheCommandDisablesWhenResourceIsDegraded()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		_ = builder.AddRedisServices();
		var resource = builder.Resources.OfType<RedisResource>()
			.Single(resource => resource.Name == AppHostConstants.RedisCache);
		var command = resource.Annotations.OfType<ResourceCommandAnnotation>()
			.Single(annotation => annotation.Name == "clear-cache");
		var snapshot = CreateSnapshot();
		var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
		var context = CreateUpdateCommandStateContext(
			snapshot.WithHealthReports([new HealthReportSnapshot("redis", HealthStatus.Degraded, "degraded", null)]),
			serviceProvider);

		// Act
		var state = command.UpdateState(context);

		// Assert
		state.Should().Be(ResourceCommandState.Disabled);
	}

	[Fact]
	public void ClearCacheCommandDisablesWhenHealthStatusIsUnexpected()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		_ = builder.AddRedisServices();
		var resource = builder.Resources.OfType<RedisResource>()
			.Single(resource => resource.Name == AppHostConstants.RedisCache);
		var command = resource.Annotations.OfType<ResourceCommandAnnotation>()
			.Single(annotation => annotation.Name == "clear-cache");
		var snapshot = CreateSnapshot();
		SetProperty(snapshot, nameof(CustomResourceSnapshot.HealthStatus), (HealthStatus)999);
		var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
		var context = CreateUpdateCommandStateContext(snapshot, serviceProvider);

		// Act
		var state = command.UpdateState(context);

		// Assert
		state.Should().Be(ResourceCommandState.Disabled);
	}

	[Fact]
	public async Task ClearCacheCommandUpdatesStateAndFailsWhenNoConnectionStringIsAvailable()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		_ = builder.AddRedisServices();
		var resource = builder.Resources.OfType<RedisResource>()
			.Single(resource => resource.Name == AppHostConstants.RedisCache);
		var command = resource.Annotations.OfType<ResourceCommandAnnotation>()
			.Single(annotation => annotation.Name == "clear-cache");
		var snapshot = CreateSnapshot();
		var serviceProvider = new ServiceCollection()
			.AddLogging(logging => logging.Services.AddSingleton<ILoggerProvider>(new AlwaysEnabledLoggerProvider()))
			.BuildServiceProvider();
		var healthyContext =
			CreateUpdateCommandStateContext(snapshot.WithHealthReports(ImmutableArray<HealthReportSnapshot>.Empty),
				serviceProvider);
		var unhealthyContext = CreateUpdateCommandStateContext(
			snapshot.WithHealthReports([new HealthReportSnapshot("redis", HealthStatus.Unhealthy, "down", null)]),
			serviceProvider);
		var executeContext = CreateExecuteCommandContext("redis", serviceProvider);
		var method =
			typeof(RedisService).GetMethod("OnRunClearCacheCommandAsync", BindingFlags.NonPublic | BindingFlags.Static);
		method.Should().NotBeNull();
		var fakeBuilder = Substitute.For<IResourceBuilder<RedisResource>>();
		fakeBuilder.Resource.Returns(new RedisResource("redis"));

		// Act
		var healthyState = command.UpdateState(healthyContext);
		var unhealthyState = command.UpdateState(unhealthyContext);
		var execute = (Task<ExecuteCommandResult>)method.Invoke(null, [fakeBuilder, executeContext])!;
		Func<Task> awaitExecute = async () => await execute.ConfigureAwait(false);

		// Assert
		healthyState.Should().Be(ResourceCommandState.Enabled);
		unhealthyState.Should().Be(ResourceCommandState.Disabled);
		await awaitExecute.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage(
				"The endpoint `tcp` is not defined for the resource `redis`. The resource has no endpoints defined.");
	}

	[Fact]
	public async Task ClearCacheCommandAnnotationInvokesTheRedisClearHandler()
	{
		// Arrange
		var builder = DistributedApplication.CreateBuilder();
		var redisBuilder = builder.AddRedisServices();
		SetProperty(redisBuilder, "Resource", new RedisResource("redis"));
		var resource = builder.Resources.OfType<RedisResource>()
			.Single(resource => resource.Name == AppHostConstants.RedisCache);
		var command = resource.Annotations.OfType<ResourceCommandAnnotation>()
			.Single(annotation => annotation.Name == "clear-cache");
		var executeContext = CreateExecuteCommandContext("redis", new ServiceCollection().BuildServiceProvider());

		// Act
		Func<Task> act = async () => await command.ExecuteCommand!(executeContext).ConfigureAwait(false);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage(
				"The endpoint `tcp` is not defined for the resource `redis`. The resource has no endpoints defined.");
	}

	private static UpdateCommandStateContext CreateUpdateCommandStateContext(
		CustomResourceSnapshot snapshot,
		IServiceProvider serviceProvider)
	{
		var context =
			(UpdateCommandStateContext)Activator.CreateInstance(typeof(UpdateCommandStateContext), nonPublic: true)!;
		SetProperty(context, nameof(UpdateCommandStateContext.ResourceSnapshot), snapshot);
		SetProperty(context, nameof(UpdateCommandStateContext.ServiceProvider), serviceProvider);
		return context;
	}

	private static ExecuteCommandContext CreateExecuteCommandContext(
		string resourceName,
		IServiceProvider serviceProvider)
	{
		var context = (ExecuteCommandContext)Activator.CreateInstance(typeof(ExecuteCommandContext), nonPublic: true)!;
		SetProperty(context, nameof(ExecuteCommandContext.ResourceName), resourceName);
		SetProperty(context, nameof(ExecuteCommandContext.ServiceProvider), serviceProvider);
		SetProperty(context, nameof(ExecuteCommandContext.CancellationToken), CancellationToken.None);
		return context;
	}

	private static CustomResourceSnapshot CreateSnapshot()
	{
		var snapshot = (CustomResourceSnapshot)Activator.CreateInstance(typeof(CustomResourceSnapshot), nonPublic: true)!;
		SetProperty(snapshot, nameof(CustomResourceSnapshot.State), new ResourceStateSnapshot("Running", null));
		return snapshot;
	}

	private sealed class AlwaysEnabledLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
	{
		public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new AlwaysEnabledLogger();

		public void Dispose()
		{
		}
	}

	private sealed class AlwaysEnabledLogger : Microsoft.Extensions.Logging.ILogger
	{
		IDisposable Microsoft.Extensions.Logging.ILogger.BeginScope<TState>(TState state)
		{
			return new NoopDisposable();
		}

		bool Microsoft.Extensions.Logging.ILogger.IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

		void Microsoft.Extensions.Logging.ILogger.Log<TState>(
			Microsoft.Extensions.Logging.LogLevel logLevel,
			Microsoft.Extensions.Logging.EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
		}
	}

	private sealed class NoopDisposable : IDisposable
	{
		public void Dispose()
		{
		}
	}

	private static void SetProperty<T>(T target, string propertyName, object? value)
	{
		var targetType = target!.GetType();
		var property =
			targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property?.SetMethod is not null)
		{
			property.SetValue(target, value);
			return;
		}

		var field = targetType.GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
		field.Should().NotBeNull();
		field.SetValue(target, value);
	}
}