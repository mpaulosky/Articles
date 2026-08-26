//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Requests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Domain.Abstractions;

using Web.Components.Features.UserManagement.Models;

namespace Web.Components.Features.UserManagement.ManageRoles;

internal sealed record AssignRoleCommand(string UserId, string RoleId) : ICommand<Result>;

internal sealed record RemoveRoleCommand(string UserId, string RoleId) : ICommand<Result>;

internal sealed record GetAvailableRolesQuery : IQuery<Result<IReadOnlyList<RoleDto>>>;

internal sealed record GetUsersWithRolesQuery : IQuery<Result<IReadOnlyList<UserWithRolesDto>>>;
