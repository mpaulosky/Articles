// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MediatorTestHost.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Web.Data;
using Web.MyMediator;
using Web.MyMediator.Behaviors;

namespace Web.Integration.Tests.Fixtures;

/// <summary>
///     Builds a DI container wired the same way <c>Program.cs</c> wires the mediator pipeline
///     (<c>AddMyMediator</c> plus <see cref="LoggingBehavior{TRequest,TResponse}" /> only, no
///     <c>ValidationBehavior</c>) against a real MongoDB database, so handler tests dispatch through
///     <see cref="IMediator" /> instead of constructing handlers directly.
/// </summary>
public sealed class MediatorTestHost : IAsyncDisposable
{
	private readonly ServiceProvider _provider;

	private MediatorTestHost(ServiceProvider provider)
	{
		_provider = provider;
	}

	/// <summary>
	///     Gets the mediator resolved from the underlying DI container.
	/// </summary>
	public IMediator Mediator => _provider.GetRequiredService<IMediator>();

	/// <summary>
	///     Creates a host wired against a database uniquely named for the calling test, on the
	///     container shared by <paramref name="fixture" />.
	/// </summary>
	/// <param name="fixture">The shared MongoDB container fixture.</param>
	/// <param name="databaseName">
	///     A name unique to the calling test; callers typically pass
	///     <c>$"{nameof(MyTests)}-{Guid.NewGuid()}"</c>.
	/// </param>
	public static MediatorTestHost Create(MongoContainerFixture fixture, string databaseName)
	{
		ArgumentNullException.ThrowIfNull(fixture);
		ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

		var services = new ServiceCollection();

		services.AddLogging();
		services.AddMyMediator(typeof(ArticleRepository).Assembly);
		services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

		services.AddDbContextFactory<ArticlesMongoDbContext>(options => options
			.UseMongoDB(fixture.ConnectionString, databaseName)
			// Each test builds its own container with its own internal EF Core service provider,
			// same as MongoContainerFixture.CreateContext; intentional isolation, not a leak.
			.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));
		services.AddScoped<ArticleRepository>();
		services.AddScoped<CategoryRepository>();

		return new MediatorTestHost(services.BuildServiceProvider());
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _provider.DisposeAsync().ConfigureAwait(false);
	}
}
