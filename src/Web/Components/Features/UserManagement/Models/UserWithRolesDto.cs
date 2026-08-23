//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserWithRolesDto.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : Articles
//Project Name :  Web
//=======================================================

namespace Web.Components.Features.UserManagement.Models;

internal sealed record UserWithRolesDto(string UserId, string Email, string Name, IReadOnlyList<string> Roles);
