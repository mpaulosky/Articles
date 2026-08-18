//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementCacheKeys.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace Web.Infrastructure.Caching;

/// <summary>Cache key constants for the UserManagement two-tier cache.</summary>
internal static class UserManagementCacheKeys
{
	/// <summary>Key for the list of all users with their roles.</summary>
	public const string AllUsers = "usermgmt:users";

	/// <summary>Key for the list of all available roles.</summary>
	public const string AllRoles = "usermgmt:roles";
}
