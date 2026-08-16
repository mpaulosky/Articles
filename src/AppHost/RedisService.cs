// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RedisServices.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : Articles
// Project Name :  AppHost
// =======================================================

using StackExchange.Redis;

namespace AppHost;

/// <summary>
/// Provides configuration helpers and extension methods for Redis-backed application services.
/// </summary>
public static class RedisService
{
	/// <summary>
	/// Returns the default Redis image metadata used by the AppHost configuration.
	/// </summary>
	public static (string ImageName, string ImageTag, string PasswordParameterName) GetRedisResourceSettings()
	{
		return ("redis", "latest", "redis-password");
	}

	/// <summary>
	/// Adds Redis services to the distributed application builder with a clear cache command and persistent lifetime.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns>An <see cref="IResourceBuilder{RedisResource}"/> for further configuration.</returns>
	public static IResourceBuilder<RedisResource> AddRedisServices(this IDistributedApplicationBuilder builder)
	{
		var cache = builder.AddRedis(AppHostConstants.RedisCache)
			.WithClearCommand()
			.WithLifetime(ContainerLifetime.Persistent);
		return cache;
	}

	/// <summary>
	/// Adds a custom clear-cache command to the Redis resource builder.
	/// </summary>
	/// <param name="builder">The Redis resource builder.</param>
	/// <returns>The updated <see cref="IResourceBuilder{RedisResource}"/>.</returns>
	private static IResourceBuilder<RedisResource> WithClearCommand(this IResourceBuilder<RedisResource> builder)
	{
		builder.WithCommand(
			name: "clear-cache",
			displayName: "Clear Cache",
			executeCommand: context => OnRunClearCacheCommandAsync(builder, context),
			commandOptions: new CommandOptions
			{
				UpdateState = OnUpdateResourceState,
				ConfirmationMessage = "Are you sure you want to clear the cache?",
				Description = "This command will clear all cached data in the Redis cache.",
			}
		);
		return builder;
	}

	/// <summary>
	/// Executes the clear-cache command, flushing all data from the Redis cache.
	/// </summary>
	/// <param name="builder">The Redis resource builder.</param>
	/// <param name="context">The command execution context.</param>
	/// <returns>A task representing the asynchronous operation, with the command result.</returns>
	private static async Task<ExecuteCommandResult> OnRunClearCacheCommandAsync(
		IResourceBuilder<RedisResource> builder,
		ExecuteCommandContext context)
	{
		var connectionString = await builder.Resource.GetConnectionStringAsync() ?? throw new InvalidOperationException(
			$"Unable to get the '{context.ResourceName}' connection string.");

		await using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
		var database = connection.GetDatabase();
		await database.ExecuteAsync("FLUSHALL");
		return CommandResults.Success();
	}

	/// <summary>
	/// Updates the state of the clear-cache command based on the health status of the resource.
	/// </summary>
	/// <param name="context">The update command state context.</param>
	/// <returns>The new <see cref="ResourceCommandState"/> for the command.</returns>
	private static ResourceCommandState OnUpdateResourceState(UpdateCommandStateContext context)
	{
		var logger = context.ServiceProvider.GetRequiredService<ILogger<Program>>();
		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Updating resource state: {ResourceSnapshot}", context.ResourceSnapshot);
		}
		return context.ResourceSnapshot.HealthStatus is HealthStatus.Healthy ? ResourceCommandState.Enabled : ResourceCommandState.Disabled;
	}
}
