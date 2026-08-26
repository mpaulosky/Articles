//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Globalization;

using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;

using Domain.Abstractions;

using Microsoft.Extensions.Configuration;

using Web.Components.Features.UserManagement.AddUserRoles;
using Web.Components.Features.UserManagement.Auth0;
using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.GetUserRoles;
using Web.Components.Features.UserManagement.GetUserWithRoles;
using Web.Components.Features.UserManagement.Models;

namespace Web.Components.Features.UserManagement.ManageRoles;

internal sealed class UserManagementHandler(
IManagementApiClientFactory managementApiClientFactory,
IUserManagementCacheService cache,
IConfiguration? configuration = null)
: IRequestHandler<GetUsersWithRolesQuery, Result<IReadOnlyList<UserWithRolesDto>>>,
IRequestHandler<AssignRoleCommand, Result>,
IRequestHandler<RemoveRoleCommand, Result>,
IRequestHandler<GetAvailableRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
	// Auth0's Management API rate limits are as low as 2 req/s sustained on Free/non-production
	// tenants, so this stays small by default; override via "Auth0:Management:RolesFetchConcurrency".
	private const int DefaultRolesFetchConcurrency = 5;

	public async Task<Result<IReadOnlyList<UserWithRolesDto>>> Handle(
	GetUsersWithRolesQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var users = await cache.GetOrFetchUsersAsync(async () =>
			{
				var client = await managementApiClientFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
				var usersPager = await client.Users.ListAsync(new ListUsersRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
				var auth0Users = new List<UserResponseSchema>();
				await foreach (var user in usersPager.ConfigureAwait(false))
				{
					auth0Users.Add(user);
				}

				var result = new UserWithRolesDto[auth0Users.Count];
				var parallelOptions = new ParallelOptions
				{
					MaxDegreeOfParallelism = GetRolesFetchConcurrency(),
					CancellationToken = cancellationToken
				};
				await Parallel.ForEachAsync(Enumerable.Range(0, auth0Users.Count), parallelOptions, async (index, ct) =>
				{
					var user = auth0Users[index];
					var rolesPager = await client.Users.Roles.ListAsync(
					user.UserId ?? string.Empty, new ListUserRolesRequestParameters(), cancellationToken: ct).ConfigureAwait(false);
					var roles = new List<string>();
					await foreach (var role in rolesPager.ConfigureAwait(false))
					{
						roles.Add(role.Name ?? string.Empty);
					}
					result[index] = new UserWithRolesDto(
					user.UserId ?? string.Empty,
					user.Email ?? string.Empty,
					user.Name ?? user.Email ?? string.Empty,
					roles);
				}).ConfigureAwait(false);
				return result;
			}, cancellationToken).ConfigureAwait(false);
			return Result.Ok(users);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<IReadOnlyList<UserWithRolesDto>>(ex.Message);
		}
		catch (HttpRequestException ex)
		{
			return Result.Fail<IReadOnlyList<UserWithRolesDto>>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<IReadOnlyList<UserWithRolesDto>>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}

	public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var client = await managementApiClientFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
			await client.Users.Roles.AssignAsync(
			request.UserId,
			new AssignUserRolesRequestContent { Roles = [request.RoleId] },
			cancellationToken: cancellationToken).ConfigureAwait(false);
			await cache.InvalidateUsersAsync(CancellationToken.None).ConfigureAwait(false);
			return Result.Ok();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail(ex.Message);
		}
		catch (HttpRequestException ex)
		{
			return Result.Fail(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}

	public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var client = await managementApiClientFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
			await client.Users.Roles.DeleteAsync(
			request.UserId,
			new DeleteUserRolesRequestContent { Roles = [request.RoleId] },
			cancellationToken: cancellationToken).ConfigureAwait(false);
			await cache.InvalidateUsersAsync(CancellationToken.None).ConfigureAwait(false);
			return Result.Ok();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail(ex.Message);
		}
		catch (HttpRequestException ex)
		{
			return Result.Fail(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}

	public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var roles = await cache.GetOrFetchRolesAsync(async () =>
			{
				var client = await managementApiClientFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
				var rolesPager = await client.Roles.ListAsync(new ListRolesRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
				var result = new List<RoleDto>();
				await foreach (var role in rolesPager.ConfigureAwait(false))
				{
					result.Add(new RoleDto(role.Id ?? string.Empty, role.Name ?? string.Empty));
				}
				return result;
			}, cancellationToken).ConfigureAwait(false);
			return Result.Ok(roles);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<IReadOnlyList<RoleDto>>(ex.Message);
		}
		catch (HttpRequestException ex)
		{
			return Result.Fail<IReadOnlyList<RoleDto>>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<IReadOnlyList<RoleDto>>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}

	private int GetRolesFetchConcurrency()
	{
		var configured = configuration?["Auth0:Management:RolesFetchConcurrency"];
		return int.TryParse(configured, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
			? value
			: DefaultRolesFetchConcurrency;
	}
}
