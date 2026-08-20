//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RemoveRoleCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Domain.Abstractions;

namespace Web.Components.Features.UserManagement;

internal sealed record RemoveRoleCommand(string UserId, string RoleId) : ICommand<Result>;
