//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Auth0ManagementApiClientFactory.cs
//Company :       mpaulosky
//Author :        Teqslamer
//Solution Name : Articles
//Project Name :  Web
//=======================================================

using System.Text.Json.Serialization;

using Auth0.ManagementApi;

namespace Web.Components.Features.UserManagement.Auth0;

/// <inheritdoc cref="IManagementApiClientFactory" />
internal sealed class Auth0ManagementApiClientFactory(
IConfiguration configuration,
IHttpClientFactory httpClientFactory) : IManagementApiClientFactory
{
	public async Task<IManagementApiClient> CreateAsync(CancellationToken cancellationToken)
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
