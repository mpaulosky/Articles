//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetUsersWithRolesQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Domain.Abstractions;

namespace Web.Components.Features.UserManagement.GetUserWithRoles;

internal sealed record GetUsersWithRolesQuery : IQuery<Result<IReadOnlyList<UserWithRolesDto>>>;

internal sealed record UserWithRolesDto(string UserId, string Email, string Name, IReadOnlyList<string> Roles);
