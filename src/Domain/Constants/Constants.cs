// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Constants.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

namespace Domain.Constants;

/// <summary>
///     Provides shared application constants used across the Articles solution.
/// </summary>
public static class ApplicationConstants
{
	/// <summary>
	///     The authorization policy name for administrator-only access.
	/// </summary>
	public const string AdminPolicy = "AdminOnly";

	/// <summary>
	///     The logical MongoDB server resource name.
	/// </summary>
	public const string Server = "Server";

	/// <summary>
	///     The MongoDB database name used by the application.
	/// </summary>
	public const string DatabaseName = "articlesdb";

	/// <summary>
	///     The default CORS policy name.
	/// </summary>
	public const string DefaultCorsPolicy = "DefaultPolicy";

	/// <summary>
	///     The output cache resource name.
	/// </summary>
	public const string OutputCache = "output-cache";

	/// <summary>
	///     The Redis cache resource name.
	/// </summary>
	public const string RedisCache = "RedisCache";

	/// <summary>
	///     The web application resource name.
	/// </summary>
	public const string Website = "WebApp";

	/// <summary>
	///     The category cache entry name.
	/// </summary>
	public const string CategoryCacheName = "CategoryData";

	/// <summary>
	///     The article cache entry name.
	/// </summary>
	public const string ArticleCacheName = "ArticleData";
}