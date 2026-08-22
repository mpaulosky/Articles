using System.Security.Claims;
using System.Text.Encodings.Web;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Web;
using Web.Security;
using Web.Services;

using LocalAuthenticationServiceCollectionExtensions = Web.Security.AuthenticationServiceCollectionExtensions;

namespace Web.Tests;

public class AuthWiringExtensionsTests
{
	[Fact]
	public async Task AddAuth0Authentication_WhenEnvironmentIsTesting_UsesCookieAuthenticationDefaults()
	{
		// Arrange
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Auth0:Domain"] = "example.auth0.com",
			["Auth0:ClientId"] = "client-id",
			["Auth0:ClientSecret"] = "client-secret"
		});

		// Act
		builder.AddAuth0Authentication();
		await using var provider = builder.Services.BuildServiceProvider();
		var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
		var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();

		// Assert
		defaultScheme.Should().NotBeNull();
		defaultScheme!.Name.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
	}

	[Fact]
	public void AddAuth0Authentication_WhenConfigurationIsMissing_ThrowsInvalidOperationException()
	{
		// Arrange
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>());

		// Act
		var act = () => builder.AddAuth0Authentication();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Missing required Auth0 configuration values: Auth0:Domain, Auth0:ClientId, Auth0:ClientSecret*");
	}

	[Fact]
	public void UseAuth0Authentication_WhenApplicationIsBuilt_DoesNotThrow()
	{
		// Arrange
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Auth0:Domain"] = "example.auth0.com",
			["Auth0:ClientId"] = "client-id",
			["Auth0:ClientSecret"] = "client-secret"
		});
		builder.AddAuth0Authentication();
		var app = builder.Build();

		// Act
		var act = () => app.UseAuth0Authentication();

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public async Task
		AddAuthenticationAndAuthorization_WhenAuth0ValuesArePlaceholder_UsesCookieAuthAndScopedAuthenticationStateProvider()
	{
		// Arrange
		var services = new ServiceCollection();
		var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Auth0:Domain"] = "example.auth0.com",
			["Auth0:ClientId"] = "YOUR_CLIENT_ID",
			["Auth0:ClientSecret"] = "YOUR_CLIENT_SECRET"
		}).Build();

		// Act
		services.AddAuthenticationAndAuthorization(config);
		await using var provider = services.BuildServiceProvider();
		var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
		var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
		var stateProvider = provider.GetRequiredService<AuthenticationStateProvider>();

		// Assert
		defaultScheme.Should().NotBeNull();
		defaultScheme!.Name.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
		stateProvider.Should().BeOfType<Auth0AuthenticationStateProvider>();
	}

	[Fact]
	public async Task AddLocalAuthentication_WhenShouldUseLocalTestLogin_RegistersLocalTestSchemeAndProvider()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLocalAuthentication(shouldUseLocalTestLogin: true);
		await using var provider = services.BuildServiceProvider();
		var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
		var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
		var stateProvider = provider.GetRequiredService<AuthenticationStateProvider>();

		// Assert
		defaultScheme.Should().NotBeNull();
		defaultScheme!.Name.Should().Be(LocalAuthenticationServiceCollectionExtensions.LocalTestAuthenticationScheme);
		stateProvider.Should().BeOfType<LocalTestAuthenticationStateProvider>();
	}

	[Fact]
	public async Task AddLocalAuthentication_WhenShouldUseAnonymousLogin_RegistersAnonymousSchemeAndProvider()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		services.AddLocalAuthentication(shouldUseLocalTestLogin: false);
		await using var provider = services.BuildServiceProvider();
		var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
		var defaultScheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
		var stateProvider = provider.GetRequiredService<AuthenticationStateProvider>();

		// Assert
		defaultScheme.Should().NotBeNull();
		defaultScheme!.Name.Should().Be(LocalAuthenticationServiceCollectionExtensions.LocalAnonymousAuthenticationScheme);
		stateProvider.Should().BeOfType<LocalAnonymousAuthenticationStateProvider>();
	}

	[Fact]
	public async Task LocalAnonymousAuthenticationStateProvider_GetAuthenticationStateAsync_ReturnsAnonymousPrincipal()
	{
		// Arrange
		var provider = new LocalAnonymousAuthenticationStateProvider();

		// Act
		var state = await provider.GetAuthenticationStateAsync();

		// Assert
		state.User.Identity.Should().NotBeNull();
		state.User.Identity!.IsAuthenticated.Should().BeFalse();
		state.User.Claims.Should().BeEmpty();
	}

	[Fact]
	public void LocalTestAuthenticationStateProvider_CreatePrincipal_ReturnsExpectedClaims()
	{
		// Act
		var principal = LocalTestAuthenticationStateProvider.CreatePrincipal();

		// Assert
		principal.Identity.Should().NotBeNull();
		principal.Identity!.AuthenticationType.Should()
			.Be(LocalAuthenticationServiceCollectionExtensions.LocalTestAuthenticationScheme);
		principal.Identity.IsAuthenticated.Should().BeTrue();
		principal.FindFirstValue(ClaimTypes.Name).Should().Be("Test User");
		principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("test-user-id");
		principal.FindFirstValue(ClaimTypes.Email).Should().Be("test.user@example.com");
		principal.IsInRole("Admin").Should().BeTrue();
		principal.IsInRole("Author").Should().BeTrue();
	}

	[Fact]
	public async Task
		LocalAuthenticationHandler_HandleAuthenticateAsync_WhenLocalTestSchemeIsUsed_ReturnsAuthenticatedPrincipal()
	{
		// Arrange
		var monitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
		var handler = new LocalAuthenticationHandler(
			monitor,
			new LoggerFactory(),
			UrlEncoder.Default);
		var scheme = new AuthenticationScheme(
			LocalAuthenticationServiceCollectionExtensions.LocalTestAuthenticationScheme,
			LocalAuthenticationServiceCollectionExtensions.LocalTestAuthenticationScheme,
			typeof(LocalAuthenticationHandler));
		await handler.InitializeAsync(scheme, new DefaultHttpContext());

		// Act
		var result = await handler.AuthenticateAsync();

		// Assert
		result.Succeeded.Should().BeTrue();
		result.Principal.Should().NotBeNull();
		result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
		result.Principal.FindFirstValue(ClaimTypes.Name).Should().Be("Test User");
	}

	[Fact]
	public async Task
		LocalAuthenticationHandler_HandleAuthenticateAsync_WhenLocalAnonymousSchemeIsUsed_ReturnsAnonymousPrincipal()
	{
		// Arrange
		var monitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
		var handler = new LocalAuthenticationHandler(
			monitor,
			new LoggerFactory(),
			UrlEncoder.Default);
		var scheme = new AuthenticationScheme(
			LocalAuthenticationServiceCollectionExtensions.LocalAnonymousAuthenticationScheme,
			LocalAuthenticationServiceCollectionExtensions.LocalAnonymousAuthenticationScheme,
			typeof(LocalAuthenticationHandler));
		await handler.InitializeAsync(scheme, new DefaultHttpContext());

		// Act
		var result = await handler.AuthenticateAsync();

		// Assert
		result.Succeeded.Should().BeTrue();
		result.Principal.Should().NotBeNull();
		result.Principal!.Identity!.IsAuthenticated.Should().BeFalse();
		result.Principal.Claims.Should().BeEmpty();
	}

	private static WebApplicationBuilder CreateTestWebApplicationBuilder(string environmentName = "Testing")
	{
		var contentRootPath = Path.Combine(Path.GetTempPath(), "Articles-WebTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(contentRootPath);
		var originalCurrentDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(contentRootPath);

		try
		{
			return WebApplication.CreateBuilder(new WebApplicationOptions
			{
				EnvironmentName = environmentName, ContentRootPath = contentRootPath
			});
		}
		finally
		{
			Directory.SetCurrentDirectory(originalCurrentDirectory);
		}
	}

	private sealed class TestOptionsMonitor<T>(T options) : IOptionsMonitor<T>
	{
		public T CurrentValue => options;

		public T Get(string? name) => options;

		public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

		private sealed class NullDisposable : IDisposable
		{
			public static readonly NullDisposable Instance = new();

			public void Dispose()
			{
			}
		}
	}
}
