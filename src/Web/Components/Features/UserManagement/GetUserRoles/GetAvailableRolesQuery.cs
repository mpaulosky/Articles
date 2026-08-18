//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetAvailableRolesQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Domain.Abstractions;

namespace Web.Components.Features.UserManagement.GetUserRoles;

internal sealed record GetAvailableRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;

internal sealed record RoleDto(string Id, string Name);
