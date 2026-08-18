//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Text.Json.Serialization;

using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;

using Domain.Abstractions;

using Web.Components.Features.UserManagement.AddUserRoles;
using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.GetUserRoles;
using Web.Components.Features.UserManagement.GetUserWithRoles;

namespace Web.Components.Features.UserManagement.ManageRoles;

internal sealed class UserManagementHandler(
IConfiguration configuration,
IHttpClientFactory httpClientFactory,
IUserManagementCacheService cache)
: IRequestHandler<GetUsersWithRolesQuery, Result<IReadOnlyList<UserWithRolesDto>>>,
IRequestHandler<AssignRoleCommand, Result>,
IRequestHandler<RemoveRoleCommand, Result>,
IRequestHandler<GetAvailableRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
	public async Task<Result<IReadOnlyList<UserWithRolesDto>>> Handle(
	GetUsersWithRolesQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var users = await cache.GetOrFetchUsersAsync(async () =>
			{
				var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
				var usersPager = await client.Users.ListAsync(new ListUsersRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
				var result = new List<UserWithRolesDto>();
				await foreach (var user in usersPager.ConfigureAwait(false))
				{
					var rolesPager = await client.Users.Roles.ListAsync(
					user.UserId ?? string.Empty, new ListUserRolesRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
					var roles = new List<string>();
					await foreach (var role in rolesPager.ConfigureAwait(false))
					{
						roles.Add(role.Name ?? string.Empty);
					}
					result.Add(new UserWithRolesDto(
					user.UserId ?? string.Empty,
					user.Email ?? string.Empty,
					user.Name ?? user.Email ?? string.Empty,
					roles));
				}
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
			var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
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
			var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
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
				var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
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

	private async Task<ManagementApiClient> GetManagementClientAsync(CancellationToken cancellationToken)
	{
		var domain = GetRequiredManagementSetting(
			"Auth0:Management:Domain",
			"Auth0:Management:Domain");
		var clientId = GetRequiredManagementSetting(
			"Auth0:Management:ClientId",
			"Auth0:Management:ClientId");
		var clientSecret = GetRequiredManagementSetting(
			"Auth0:Management:ClientSecret",
			"Auth0:Management:ClientSecret");
		var audience = GetOptionalManagementSetting(
			"Auth0:Management:Audience",
			"Auth0:Management:Audience")
				?? $"https://{domain}/api/v2/";

		using var httpClient = httpClientFactory.CreateClient();
		var tokenResponse = await httpClient.PostAsJsonAsync(
		$"https://{domain}/oauth/token",
		new
		{
			client_id = clientId,
			client_secret = clientSecret,
			audience,
			grant_type = "client_credentials"
		}, cancellationToken).ConfigureAwait(false);
		tokenResponse.EnsureSuccessStatusCode();
		var tokenData = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken).ConfigureAwait(false);
		if (string.IsNullOrWhiteSpace(tokenData?.AccessToken))
		{
			throw new InvalidOperationException("Auth0 Management API token response did not contain a valid access_token.");
		}

		return new ManagementApiClient(
		token: tokenData.AccessToken,
		clientOptions: new ClientOptions { BaseUrl = $"https://{domain}/api/v2" });
	}

	private string GetRequiredManagementSetting(string primaryKey, string legacyKey, params string[] additionalKeys)
	{
		var keys = new string[additionalKeys.Length + 2];
		keys[0] = primaryKey;
		keys[1] = legacyKey;
		additionalKeys.CopyTo(keys, 2);

		return GetOptionalManagementSetting(keys)
				?? throw new InvalidOperationException(
					string.Join(" ", Array.ConvertAll(keys, key => $"{key} not configured.")));
	}

	private string? GetOptionalManagementSetting(params string[] keys)
	{
		foreach (var key in keys)
		{
			var value = configuration[key];
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}

		return null;
	}

	private sealed class TokenResponse
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; init; } = string.Empty;
	}
}
