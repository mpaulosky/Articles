// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Enum.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

namespace Domain.Enums;

/// <summary>
///     Defines application role names used for authorization decisions.
/// </summary>
public enum Roles
{
	/// <summary>
	///     Administrator role with elevated permissions.
	/// </summary>
	Admin = 0,

	/// <summary>
	///     Author role for users who can create article content.
	/// </summary>
	Author = 10
}

/// <summary>
///     Defines canonical category keys for seeded article categories.
/// </summary>
public enum CategoryNames
{
	/// <summary>
	///     ASP.NET Core category key.
	/// </summary>
	AspNetCore = 0,

	/// <summary>
	///     Blazor Server category key.
	/// </summary>
	BlazorServer = 1,

	/// <summary>
	///     Blazor WebAssembly category key.
	/// </summary>
	BlazorWasm = 2,

	/// <summary>
	///     Entity Framework Core category key.
	/// </summary>
	EntityFrameworkCore = 3,

	/// <summary>
	///     .NET MAUI category key.
	/// </summary>
	NetMaui = 4,

	/// <summary>
	///     Fallback category key for uncategorized or miscellaneous content.
	/// </summary>
	Other = 5
}