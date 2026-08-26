//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Auth0ManagementApiClientFactory.cs
//Company :       mpaulosky
//Author :        Teqslamer
//Solution Name : Articles
//Project Name :  Web
//=======================================================

using Auth0.ManagementApi;

namespace Web.Components.Features.UserManagement.Auth0;

/// <inheritdoc cref="IManagementApiClientFactory" />
internal sealed class Auth0ManagementApiClientFactory(
IConfiguration configuration,
IHttpClientFactory httpClientFactory) : IManagementApiClientFactory, IDisposable
{
	private readonly Lock gate = new();

	private ClientCredentialsTokenProvider? tokenProvider;

	public Task<IManagementApiClient> CreateAsync(CancellationToken cancellationToken)
	{
		var domain = GetRequiredManagementSetting(
			"Auth0:Management:Domain",
			"Auth0:Management:Domain");

		var client = new ManagementClient(new ManagementClientOptions
		{
			Domain = domain,
			TokenProvider = GetOrCreateTokenProvider(domain),
			HttpClient = httpClientFactory.CreateClient()
		});

		return Task.FromResult<IManagementApiClient>(client);
	}

	public void Dispose() => tokenProvider?.Dispose();

	// Cached and reused across CreateAsync calls: ClientCredentialsTokenProvider holds the
	// exchanged access token (Auth0 tokens are valid 24h) and refreshes it only near expiry,
	// so a fresh instance per call would defeat the whole point of not re-exchanging a token
	// on every Management API request.
	private ClientCredentialsTokenProvider GetOrCreateTokenProvider(string domain)
	{
		if (tokenProvider is not null)
		{
			return tokenProvider;
		}

		lock (gate)
		{
#pragma warning disable CA1508 // False positive: analyzer only reasons about the single-threaded path and
			// doesn't know another thread could have already set tokenProvider between the unlocked check
			// above and taking this lock — the ??= is the double-checked-locking guard, not redundant.
			// CA2000: ownership of the HttpClient transfers into ClientCredentialsTokenProvider, which this
			// class disposes in Dispose() below.
#pragma warning disable CA2000
			tokenProvider ??= new ClientCredentialsTokenProvider(
				domain,
				GetRequiredManagementSetting("Auth0:Management:ClientId", "Auth0:Management:ClientId"),
				GetRequiredManagementSetting("Auth0:Management:ClientSecret", "Auth0:Management:ClientSecret"),
				GetOptionalManagementSetting("Auth0:Management:Audience", "Auth0:Management:Audience"),
				httpClientFactory.CreateClient());
#pragma warning restore CA2000
#pragma warning restore CA1508
		}

		return tokenProvider;
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
}
