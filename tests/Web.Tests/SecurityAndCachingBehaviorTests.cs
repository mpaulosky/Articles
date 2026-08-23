using System.Security.Claims;
using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MongoDB.Bson;

using NSubstitute;

using Web.Components.Features.UserManagement.Caching.Converters;
using Web.Components.Features.UserManagement.Caching.Extensions;
using Web.Components.Features.UserManagement.Caching.Interfaces;
using Web.Components.Features.UserManagement.Models;
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
	[InlineData("example.auth0.com", "test-client-id", "real-client-secret", true)]
	[InlineData("example.auth0.com", "real-client-id", "test-client-secret", true)]
	[InlineData("example.auth0.com", "real-client-id", "real-client-secret", false)]
	[InlineData(null, "real-client-id", "real-client-secret", true)]
	[InlineData("", "real-client-id", "real-client-secret", true)]
	public void UsesPlaceholderWebAppLogin_RecognizesPlaceholderValues(string? domain, string? clientId,
		string? clientSecret, bool expected)
	{
		// Arrange

		// Act
		var result = Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(domain, clientId, clientSecret);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("test.auth0.com", "real-id", true)]
	[InlineData("example.auth0.com", "test-client-id", true)]
	[InlineData("example.auth0.com", "real-id", false)]
	[InlineData(null, "real-id", true)]
	[InlineData("example.auth0.com", "", true)]
	public void UsesPlaceholderWebAppLogin_TwoParamOverload(string? domain, string? clientId, bool expected)
	{
		// Act
		var result = Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(domain, clientId);

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
	public void IsAuthenticationEnabled_OnlyReturnsTrueForNonPlaceholderRealConfiguration(string? domain,
		string? clientId, string? clientSecret, bool expected)
	{
		// Arrange

		// Act
		var result = Auth0ConfigurationHelper.IsAuthenticationEnabled(domain, clientId, clientSecret);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("example.auth0.com", "real-client-id", false)]
	[InlineData("test.auth0.com", "real-client-id", false)]
	public void IsAuthenticationEnabled_TwoParamOverload_ReturnsFalseBecauseClientSecretIsNull(string? domain,
		string? clientId, bool expected)
	{
		// Act
		var result = Auth0ConfigurationHelper.IsAuthenticationEnabled(domain, clientId);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData(true, "test.auth0.com", "real-client-id", true)]
	[InlineData(false, "test.auth0.com", "real-client-id", false)]
	[InlineData(true, "example.auth0.com", "real-client-id", false)]
	public void ShouldUseLocalTestLogin_TwoParamOverload(bool isTestingEnv, string? domain, string? clientId,
		bool expected)
	{
		// Act
		var result = Auth0ConfigurationHelper.ShouldUseLocalTestLogin(isTestingEnv, domain, clientId);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData(true, "test.auth0.com", "real-client-id", "real-secret", true)]
	[InlineData(false, "test.auth0.com", "real-client-id", "real-secret", false)]
	[InlineData(true, "example.auth0.com", "real-client-id", "real-secret", false)]
	public void ShouldUseLocalTestLogin_ThreeParamOverload(bool isTestingEnv, string? domain, string? clientId,
		string? clientSecret, bool expected)
	{
		// Act
		var result = Auth0ConfigurationHelper.ShouldUseLocalTestLogin(isTestingEnv, domain, clientId, clientSecret);

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
	public async Task TransformAsync_NormalizesSupportedClaimTypesAndRoleFormats(string claimType, string claimValue,
		params string[] expectedRoles)
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
		var normalizedRoles = result.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value)
			.ToList();
		normalizedRoles.Should().Contain(expectedRoles);
	}
}

public class RoleClaimsHelperTests
{
	[Fact]
	public void GetRoleClaimTypes_ReturnsDefaults_WhenConfigSectionMissing()
	{
		// Arrange
		var config = new ConfigurationBuilder().Build();

		// Act
		var types = RoleClaimsHelper.GetRoleClaimTypes(config);

		// Assert
		types.Should().Contain("https://myblog/roles");
		types.Should().Contain("roles");
		types.Should().Contain("role");
	}

	[Fact]
	public void GetRoleClaimTypes_ReturnsConfiguredTypes_WhenPresent()
	{
		// Arrange
		var inMemory = new Dictionary<string, string?>
		{
			{ "Auth0:RoleClaimTypes:0", "custom-role-1" }, { "Auth0:RoleClaimTypes:1", "custom-role-2" }
		};
		var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

		// Act
		var types = RoleClaimsHelper.GetRoleClaimTypes(config);

		// Assert
		types.Should().BeEquivalentTo(["custom-role-1", "custom-role-2"]);
	}

	[Fact]
	public void AddRoleClaims_ThrowsArgumentNullException_WhenIdentityIsNull()
	{
		// Act
		var act = () => RoleClaimsHelper.AddRoleClaims(null!, ["role"]);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void AddRoleClaims_ExtractsAndAddsRoleClaims()
	{
		// Arrange
		var identity = new ClaimsIdentity([
			new Claim("roles", "[\"Admin\", \"Editor\"]"),
			new Claim("https://custom/roles", "Author, Reader"),
			new Claim(ClaimTypes.Role, "SuperUser")
		]);

		// Act
		RoleClaimsHelper.AddRoleClaims(identity, ["roles", "https://custom/roles"]);

		// Assert
		var roleClaims = identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
		roleClaims.Should().Contain(["Admin", "Editor", "Author", "Reader", "SuperUser"]);
	}

	[Fact]
	public void GetRoles_ReturnsOrderedDistinctRoles()
	{
		// Arrange
		var identity = new ClaimsIdentity([
			new Claim("roles", "[\"Zebra\", \"Alpha\"]"),
			new Claim("role", "Beta, Alpha"),
			new Claim("other", "IgnoreMe")
		]);
		var principal = new ClaimsPrincipal(identity);

		// Act
		var roles = RoleClaimsHelper.GetRoles(principal);

		// Assert
		roles.Should().ContainInOrder("Alpha", "Beta", "Zebra");
	}

	[Fact]
	public void GetRoles_HandlesEmptyAndMalformedClaims()
	{
		// Arrange
		var identity = new ClaimsIdentity([
			new Claim("roles", ""),
			new Claim("roles", "[bad json"),
			new Claim("roles", "  ")
		]);
		var principal = new ClaimsPrincipal(identity);

		// Act
		var roles = RoleClaimsHelper.GetRoles(principal);

		// Assert
		roles.Should().BeEmpty();
	}
}

public class ObjectIdJsonConverterTests
{
	private readonly JsonSerializerOptions _options;

	public ObjectIdJsonConverterTests()
	{
		_options = new JsonSerializerOptions();
		_options.Converters.Add(new ObjectIdJsonConverter());
	}

	[Fact]
	public void RoundTripSerialization_SerializesAndDeserializesSuccessfully()
	{
		// Arrange
		var original = ObjectId.GenerateNewId();

		// Act
		var json = JsonSerializer.Serialize(original, _options);
		var deserialized = JsonSerializer.Deserialize<ObjectId>(json, _options);

		// Assert
		json.Should().Be($"\"{original}\"");
		deserialized.Should().Be(original);
	}

	[Theory]
	[InlineData("\"not-a-valid-object-id\"")]
	[InlineData("\"\"")]
	[InlineData("null")]
	public void Deserialize_InvalidOrNullString_ReturnsObjectIdEmpty(string json)
	{
		// Act
		var deserialized = JsonSerializer.Deserialize<ObjectId>(json, _options);

		// Assert
		deserialized.Should().Be(ObjectId.Empty);
	}
}

public class CachingServiceExtensionsTests
{
	[Fact]
	public void AddUserManagementCaching_RegistersIUserManagementCacheService()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddMemoryCache();
		services.AddDistributedMemoryCache();

		// Act
		services.AddUserManagementCaching();
		var provider = services.BuildServiceProvider();
		var cacheService = provider.GetService<IUserManagementCacheService>();

		// Assert
		cacheService.Should().NotBeNull();
	}
}

public class UserManagementCacheServiceTests
{
	private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

	[Fact]
	public async Task GetOrFetchUsersAsync_WhenL1CacheHit_ReturnsCachedValueWithoutFetch()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = Substitute.For<IDistributedCache>();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var cachedUsers = new List<UserWithRolesDto> { new("user-1", "alice@example.com", "Alice", ["Admin"]) };
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
		await distributedCache.DidNotReceiveWithAnyArgs().GetAsync(default!, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task GetOrFetchUsersAsync_WhenL2CacheHit_FillsLocalCacheAndReturnsValue()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = Substitute.For<IDistributedCache>();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var cachedUsers = new List<UserWithRolesDto> { new("user-2", "bob@example.com", "Bob", ["Editor"]) };
		distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, Arg.Any<CancellationToken>())
			.Returns(JsonSerializer.SerializeToUtf8Bytes(cachedUsers));
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
	public async Task GetOrFetchUsersAsync_WhenL2CacheContainsCorruptJson_RemovesEntryAndFetchesFreshValue()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = Substitute.For<IDistributedCache>();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var fetchedUsers = new List<UserWithRolesDto> { new("user-3", "carol@example.com", "Carol", ["Reader"]) };
		distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, Arg.Any<CancellationToken>())
			.Returns(new byte[] { 0x7B, 0x7D, 0x00 });
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
		await distributedCache.Received(1)
			.RemoveAsync(UserManagementCacheKeys.AllUsers, TestContext.Current.CancellationToken);
		await distributedCache.Received(1).SetAsync(
			UserManagementCacheKeys.AllUsers,
			Arg.Is<byte[]>(payload =>
				JsonSerializer.Deserialize<List<UserWithRolesDto>>(payload, WebJsonOptions)!.Count == 1),
			Arg.Any<DistributedCacheEntryOptions>(),
			TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task InvalidateUsersAsync_RemovesEntriesFromLocalAndDistributedCache()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = Substitute.For<IDistributedCache>();
		var service = new UserManagementCacheService(localCache, distributedCache);
		var users = new List<UserWithRolesDto> { new("user-4", "dave@example.com", "Dave", ["Admin"]) };
		localCache.Set(UserManagementCacheKeys.AllUsers, users, TimeSpan.FromMinutes(1));

		// Act
		await service.InvalidateUsersAsync(TestContext.Current.CancellationToken);

		// Assert
		localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out _).Should().BeFalse();
		await distributedCache.Received(1)
			.RemoveAsync(UserManagementCacheKeys.AllUsers, TestContext.Current.CancellationToken);
	}

	[Fact]
	public async Task GetOrFetchRolesAsync_WhenL1AndL2CacheAreUsed_AndInvalidateRolesRemovesBothTiers()
	{
		// Arrange
		using var localCache = new MemoryCache(new MemoryCacheOptions());
		var distributedCache = Substitute.For<IDistributedCache>();
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
		distributedCache.GetAsync(UserManagementCacheKeys.AllRoles, Arg.Any<CancellationToken>())
			.Returns(JsonSerializer.SerializeToUtf8Bytes(cachedRoles));

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

		// Act
		await service.InvalidateRolesAsync(TestContext.Current.CancellationToken);

		// Assert
		localCache.TryGetValue(UserManagementCacheKeys.AllRoles, out _).Should().BeFalse();
		await distributedCache.Received(1)
			.RemoveAsync(UserManagementCacheKeys.AllRoles, TestContext.Current.CancellationToken);
	}
}
