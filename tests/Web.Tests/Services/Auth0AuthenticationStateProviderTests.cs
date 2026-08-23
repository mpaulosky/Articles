// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Auth0AuthenticationStateProviderTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Web.Services;

namespace Web.Tests.Services;

public class Auth0AuthenticationStateProviderTests
{
	[Fact]
	public async Task GetAuthenticationStateAsync_WhenHttpContextIsNull_ReturnsAnonymousUser()
	{
		// Arrange
		var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
		httpContextAccessor.HttpContext.Returns((HttpContext?)null);
		var provider = CreateProvider(httpContextAccessor);

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		state.User.Identity.Should().NotBeNull();
		state.User.Identity!.IsAuthenticated.Should().BeFalse();
		state.User.Claims.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenUserIsNotAuthenticated_ReturnsAnonymousUser()
	{
		// Arrange
		var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		state.User.Identity!.IsAuthenticated.Should().BeFalse();
		state.User.Claims.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenAuthenticatedWithNoRoleClaims_PreservesOriginalClaimsWithoutAddingRoles()
	{
		// Arrange
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "auth0|12345"), new Claim(ClaimTypes.Name, "Jane Doe")
		};
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
		};
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		state.User.Identity!.IsAuthenticated.Should().BeTrue();
		state.User.FindFirst(ClaimTypes.Name)!.Value.Should().Be("Jane Doe");
		state.User.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenCustomRolesClaimIsCommaSeparated_AddsEachTrimmedRole()
	{
		// Arrange
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
			new Claim("https://articlesite.com/roles", "Admin, Editor")
		};
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
		};
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		var roles = state.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
		roles.Should().BeEquivalentTo(["Admin", "Editor"]);
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenAuth0RolesClaimsPresent_AddsEachAsRoleClaim()
	{
		// Arrange
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
			new Claim("roles", "Admin"),
			new Claim("roles", "Author")
		};
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
		};
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		var roles = state.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
		roles.Should().BeEquivalentTo(["Admin", "Author"]);
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenAuth0RoleDuplicatesCustomRole_DoesNotAddDuplicateRoleClaim()
	{
		// Arrange
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
			new Claim("https://articlesite.com/roles", "Admin"),
			new Claim("roles", "Admin")
		};
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
		};
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		state.User.Claims.Count(c => c.Type == ClaimTypes.Role && c.Value == "Admin").Should().Be(1);
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenCustomRoleDuplicatesExistingRoleClaim_DoesNotAddDuplicateRoleClaim()
	{
		// Arrange
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
			new Claim(ClaimTypes.Role, "Admin"),
			new Claim("https://articlesite.com/roles", "Admin")
		};
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
		};
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		state.User.Claims.Count(c => c.Type == ClaimTypes.Role && c.Value == "Admin").Should().Be(1);
	}

	[Fact]
	public async Task GetAuthenticationStateAsync_WhenBothRoleSourcesPresent_UnionsTheRoles()
	{
		// Arrange
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
			new Claim("https://articlesite.com/roles", "Admin"),
			new Claim("roles", "Editor")
		};
		var httpContext = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
		};
		var provider = CreateProvider(CreateHttpContextAccessor(httpContext));

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		var roles = state.User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value);
		roles.Should().BeEquivalentTo(["Admin", "Editor"]);
	}

	private static IHttpContextAccessor CreateHttpContextAccessor(HttpContext httpContext)
	{
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns(httpContext);
		return accessor;
	}

	private static Auth0AuthenticationStateProvider CreateProvider(IHttpContextAccessor httpContextAccessor)
	{
		return new Auth0AuthenticationStateProvider(
			httpContextAccessor, Substitute.For<ILogger<Auth0AuthenticationStateProvider>>());
	}
}
