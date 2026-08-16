// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostConstants.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost
// =============================================

namespace AppHost;

/// <summary>
///   Provides AppHost resource names shared across resource registration helpers.
/// </summary>
public static class AppHostConstants
{
	/// <summary>
	///   The logical MongoDB server resource name.
	/// </summary>
	public const string Server = "Server";

	/// <summary>
	///   The MongoDB database name used by the application.
	/// </summary>
	public const string DatabaseName = "articlesdb";

	/// <summary>
	///   The Redis cache resource name.
	/// </summary>
	public const string RedisCache = "RedisCache";
}
