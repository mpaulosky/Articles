// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     UserManagementTestHost.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Web.Components.Features.UserManagement.Auth0;
using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.ManageRoles;
using Web.Infrastructure.Caching;
using Web.MyMediator;
using Web.MyMediator.Behaviors;

namespace Web.Integration.Tests.Fixtures;

/// <summary>
///     Builds a DI container wired the same way <c>Program.cs</c> wires the mediator pipeline
///     (<c>AddMyMediator</c> plus <see cref="LoggingBehavior{TRequest,TResponse}" /> only, no
///     <c>ValidationBehavior</c>) for <c>UserManagementHandler</c>. Unlike <see cref="MediatorTestHost" />,
///     this handler has no MongoDB dependency, so no container is involved: the real
///     <see cref="UserManagementCacheService" /> runs against real <see cref="IMemoryCache" /> and
///     <see cref="IDistributedCache" /> (in-memory, no Redis needed), and only the Auth0 boundary
///     (<see cref="IManagementApiClientFactory" />) is substituted, since a live Auth0 tenant isn't
///     available in CI.
/// </summary>
internal sealed class UserManagementTestHost : IAsyncDisposable
{
	private readonly ServiceProvider _provider;

	private UserManagementTestHost(ServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	///     Gets the mediator resolved from the underlying DI container.
	/// </summary>
	public IMediator Mediator => _provider.GetRequiredService<IMediator>();

	/// <summary>
	///     Creates a host wired with the given substituted <see cref="IManagementApiClientFactory" />.
	/// </summary>
	/// <param name="managementApiClientFactory">The fake Auth0 client factory the test configured.</param>
	public static UserManagementTestHost Create(IManagementApiClientFactory managementApiClientFactory)
	{
		ArgumentNullException.ThrowIfNull(managementApiClientFactory);

		var services = new ServiceCollection();

		services.AddLogging();
		services.AddMyMediator(typeof(UserManagementHandler).Assembly);
		services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

		services.AddMemoryCache();
		services.AddDistributedMemoryCache();
		services.AddScoped<IUserManagementCacheService, UserManagementCacheService>();
		services.AddSingleton(managementApiClientFactory);

		return new UserManagementTestHost(services.BuildServiceProvider());
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _provider.DisposeAsync().ConfigureAwait(false);
	}
}
