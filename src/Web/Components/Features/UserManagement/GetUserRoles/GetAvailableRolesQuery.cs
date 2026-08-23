//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetAvailableRolesQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Domain.Abstractions;

using Web.Components.Features.UserManagement.Models;

namespace Web.Components.Features.UserManagement.GetUserRoles;

internal sealed record GetAvailableRolesQuery : IQuery<Result<IReadOnlyList<RoleDto>>>;
