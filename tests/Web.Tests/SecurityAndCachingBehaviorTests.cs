using System.Security.Claims;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Components.Features.UserManagement.GetUserRoles;
using Web.Components.Features.UserManagement.GetUserWithRoles;
using Web.Infrastructure.Caching;
using Web.Security;

namespace Web.Tests;

public class Auth0ConfigurationHelperTests
{
	[Theory]
	[InlineData("YOUR_DOMAIN", "real-client-id", "real-client-secret", true)]
	[InlineData("example.auth0.com", "YOUR_CLIENT_ID", "real-client-secret", true)]
	[InlineData("example.auth0.com", "real-client-id", "YOUR_CLIENT_SECRET", true)]
	[InlineData("test.auth0.com", "real-client-id", "real-client-secret", true)]
	[InlineData("example.auth0.com", "real-client-id", "real-client-secret", false)]
	public void UsesPlaceholderWebAppLogin_RecognizesPlaceholderValues(string? domain, string? clientId, string? clientSecret, bool expected)
	{
		// Arrange

		// Act
		var result = Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(domain, clientId, clientSecret);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("example.auth0.com", "real-client-id", "real-client-secret", true)]
	[InlineData(null, "real-client-id", "real-client-secret", false)]
	[InlineData("example.auth0.com", null, "real-client-secret", false)]
	[InlineData("example.auth0.com", "real-client-id", null, false)]
	[InlineData("test.auth0.com", "real-client-id", "real-client-secret", false)]
	[InlineData("YOUR_DOMAIN", "real-client-id", "real-client-secret", false)]
	public void IsAuthenticationEnabled_OnlyReturnsTrueForNonPlaceholderRealConfiguration(string? domain, string? clientId, string? clientSecret, bool expected)
	{
		// Arrange

		// Act
		var result = Auth0ConfigurationHelper.IsAuthenticationEnabled(domain, clientId, clientSecret);

		// Assert
		result.Should().Be(expected);
	}
}

public class RoleClaimNormalizerTests
{
	[Theory]
	[InlineData("https://articles/roles", "[\"Admin\",\"Editor\"]", "Admin", "Editor")]
	[InlineData("roles", "Admin,Editor", "Admin", "Editor")]
	[InlineData("role", "Support", "Support")]
	public async Task TransformAsync_NormalizesSupportedClaimTypesAndRoleFormats(string claimType, string claimValue, params string[] expectedRoles)
	{
		// Arrange
		var identity = new ClaimsIdentity(
		[
			new Claim(claimType, claimValue),
			new Claim(ClaimTypes.Role, "Admin")
		],
		"TestAuth");
		var principal = new ClaimsPrincipal(identity);
		var normalizer = new RoleClaimNormalizer();

		// Act
		var result = await normalizer.TransformAsync(principal);

		// Assert
		var normalizedRoles = result.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value).ToList();
		normalizedRoles.Should().Contain(expectedRoles);
	}
}

public class UserManagementCacheServiceTests
{
	[Fact]
	public async Task GetOrFetchUsersAsync_WhenL1CacheHit_ReturnsCachedValueWithoutFetch()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = CreateDistributedCache();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var cachedUsers = new List<UserWithRolesDto>
		{
			new("user-1", "alice@example.com", "Alice", ["Admin"])
		};
		localCache.Set(UserManagementCacheKeys.AllUsers, cachedUsers, TimeSpan.FromMinutes(1));
		var fetchCallCount = 0;

		// Act
		var result = await service.GetOrFetchUsersAsync(() =>
		{
			fetchCallCount++;
			return Task.FromResult<IReadOnlyList<UserWithRolesDto>>(new List<UserWithRolesDto>());
		}, TestContext.Current.CancellationToken);

		// Assert
		fetchCallCount.Should().Be(0);
		result.Should().BeEquivalentTo(cachedUsers);
	}

	[Fact]
	public async Task GetOrFetchUsersAsync_WhenL2CacheHit_FillsLocalCacheAndReturnsCachedValue()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = CreateDistributedCache();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var cachedUsers = new List<UserWithRolesDto>
		{
			new("user-2", "bob@example.com", "Bob", ["Editor"])
		};
		await distributedCache.SetAsync(
			UserManagementCacheKeys.AllUsers,
			JsonSerializer.SerializeToUtf8Bytes(cachedUsers),
			new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
			TestContext.Current.CancellationToken);
		var fetchCallCount = 0;

		// Act
		var result = await service.GetOrFetchUsersAsync(() =>
		{
			fetchCallCount++;
			return Task.FromResult<IReadOnlyList<UserWithRolesDto>>(new List<UserWithRolesDto>());
		}, TestContext.Current.CancellationToken);

		// Assert
		fetchCallCount.Should().Be(0);
		result.Should().BeEquivalentTo(cachedUsers);
		localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out List<UserWithRolesDto>? l1Hit).Should().BeTrue();
		l1Hit.Should().BeEquivalentTo(cachedUsers);
	}

	[Fact]
	public async Task GetOrFetchUsersAsync_WhenCacheMiss_FetchesAndStoresInBothTiers()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = CreateDistributedCache();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var fetchedUsers = new List<UserWithRolesDto>
		{
			new("user-3", "carol@example.com", "Carol", ["Reader"])
		};
		var fetchCallCount = 0;

		// Act
		var result = await service.GetOrFetchUsersAsync(() =>
		{
			fetchCallCount++;
			return Task.FromResult<IReadOnlyList<UserWithRolesDto>>(fetchedUsers);
		}, TestContext.Current.CancellationToken);

		// Assert
		fetchCallCount.Should().Be(1);
		result.Should().BeEquivalentTo(fetchedUsers);
		localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out List<UserWithRolesDto>? l1Hit).Should().BeTrue();
		l1Hit.Should().BeEquivalentTo(fetchedUsers);
		var redisBytes = await distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, TestContext.Current.CancellationToken);
		redisBytes.Should().NotBeNull();
		JsonSerializer.Deserialize<List<UserWithRolesDto>>(redisBytes!, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Should().BeEquivalentTo(fetchedUsers);
	}

	[Fact]
	public async Task InvalidateUsersAsync_RemovesEntriesFromLocalAndDistributedCache()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = CreateDistributedCache();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var users = new List<UserWithRolesDto> { new("user-4", "dave@example.com", "Dave", ["Admin"]) };
		localCache.Set(UserManagementCacheKeys.AllUsers, users, TimeSpan.FromMinutes(1));
		await distributedCache.SetAsync(
			UserManagementCacheKeys.AllUsers,
			JsonSerializer.SerializeToUtf8Bytes(users),
			new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
			TestContext.Current.CancellationToken);

		// Act
		await service.InvalidateUsersAsync(TestContext.Current.CancellationToken);

		// Assert
		localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out _).Should().BeFalse();
		(await distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, TestContext.Current.CancellationToken)).Should().BeNull();
	}

	[Fact]
	public async Task GetOrFetchRolesAsync_WhenL1AndL2CacheHitsAndInvalidationBehaveAsExpected()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = CreateDistributedCache();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var cachedRoles = new List<RoleDto> { new("role-1", "Admin") };
		var fetchCallCount = 0;
		localCache.Set(UserManagementCacheKeys.AllRoles, cachedRoles, TimeSpan.FromMinutes(1));

		// Act
		var l1Result = await service.GetOrFetchRolesAsync(() =>
		{
			fetchCallCount++;
			return Task.FromResult<IReadOnlyList<RoleDto>>(new List<RoleDto>());
		}, TestContext.Current.CancellationToken);

		// Assert
		fetchCallCount.Should().Be(0);
		l1Result.Should().BeEquivalentTo(cachedRoles);

		// Arrange
		localCache.Remove(UserManagementCacheKeys.AllRoles);
		await distributedCache.SetAsync(
			UserManagementCacheKeys.AllRoles,
			JsonSerializer.SerializeToUtf8Bytes(cachedRoles),
			new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
			TestContext.Current.CancellationToken);

		// Act
		var l2Result = await service.GetOrFetchRolesAsync(() =>
		{
			fetchCallCount++;
			return Task.FromResult<IReadOnlyList<RoleDto>>(new List<RoleDto>());
		}, TestContext.Current.CancellationToken);

		// Assert
		fetchCallCount.Should().Be(0);
		l2Result.Should().BeEquivalentTo(cachedRoles);
		localCache.TryGetValue(UserManagementCacheKeys.AllRoles, out List<RoleDto>? localHit).Should().BeTrue();
		localHit.Should().BeEquivalentTo(cachedRoles);

		// Arrange
		fetchCallCount = 0;
		localCache.Remove(UserManagementCacheKeys.AllRoles);
		await distributedCache.RemoveAsync(UserManagementCacheKeys.AllRoles, TestContext.Current.CancellationToken);
		var fetchedRoles = new List<RoleDto> { new("role-2", "Editor") };

		// Act
		var missResult = await service.GetOrFetchRolesAsync(() =>
		{
			fetchCallCount++;
			return Task.FromResult<IReadOnlyList<RoleDto>>(fetchedRoles);
		}, TestContext.Current.CancellationToken);

		// Assert
		fetchCallCount.Should().Be(1);
		missResult.Should().BeEquivalentTo(fetchedRoles);

		// Act
		await service.InvalidateRolesAsync(TestContext.Current.CancellationToken);

		// Assert
		localCache.TryGetValue(UserManagementCacheKeys.AllRoles, out _).Should().BeFalse();
		(await distributedCache.GetAsync(UserManagementCacheKeys.AllRoles, TestContext.Current.CancellationToken)).Should().BeNull();
	}

	private static MemoryDistributedCache CreateDistributedCache()
	{
		return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
	}
}
