//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     AssignRoleCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Domain.Abstractions;

namespace Web.Components.Features.UserManagement.AddUserRoles;

internal sealed record AssignRoleCommand(string UserId, string RoleId) : IRequest<Result>;
