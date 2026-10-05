// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleImageBackfillHostedService.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Microsoft.EntityFrameworkCore;

namespace Web.Data;

/// <summary>
///     Runs <see cref="ArticleImageBackfillMigration" /> once in the background when the application
///     starts, without delaying startup and without failing it if MongoDB isn't reachable yet.
/// </summary>
public sealed partial class ArticleImageBackfillHostedService : BackgroundService
{
	private readonly IDbContextFactory<ArticlesMongoDbContext> _contextFactory;
	private readonly ILogger<ArticleImageBackfillHostedService> _logger;

	/// <summary>
	///     Initializes a new instance of the <see cref="ArticleImageBackfillHostedService" /> class.
	/// </summary>
	/// <param name="contextFactory">The factory used to create a MongoDB data context.</param>
	/// <param name="logger">The logger used to report a failed backfill attempt.</param>
	public ArticleImageBackfillHostedService(
		IDbContextFactory<ArticlesMongoDbContext> contextFactory,
		ILogger<ArticleImageBackfillHostedService> logger)
	{
		_contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			var context = await _contextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
			await using (context.ConfigureAwait(false))
			{
				await ArticleImageBackfillMigration.RunAsync(context, stoppingToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogBackfillFailed(_logger, ex);
		}
	}

	/// <summary>
	///     Returns immediately rather than the base <see cref="BackgroundService" /> behavior of waiting
	///     for <see cref="ExecuteAsync" /> to finish, so a slow or unreachable database never delays
	///     application shutdown for this best-effort, one-time migration.
	/// </summary>
	public override Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	[LoggerMessage(Level = LogLevel.Warning, Message = "ArticleImage backfill migration did not complete at startup.")]
	private static partial void LogBackfillFailed(ILogger logger, Exception exception);
}
