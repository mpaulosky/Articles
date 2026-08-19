using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;

using Bunit;

using Web.Services;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Web.UI.Tests.Layout;

public class MainLayoutThemeTests : BunitContext
{
	private sealed class TestAuthStateProvider : AuthenticationStateProvider
	{
		private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
			new[] { new Claim(ClaimTypes.Name, "test-user") },
			"TestAuthType"));

		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			return Task.FromResult(new AuthenticationState(_user));
		}
	}

	private sealed class TestAnonymousAuthStateProvider : AuthenticationStateProvider
	{
		private readonly ClaimsPrincipal _user = new(new ClaimsIdentity());

		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			return Task.FromResult(new AuthenticationState(_user));
		}
	}

	private sealed class TestAuthorizationService : IAuthorizationService
	{
		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource,
			IEnumerable<IAuthorizationRequirement> requirements)
		{
			return Task.FromResult(user.Identity?.IsAuthenticated == true
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
		}

		public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
		{
			return Task.FromResult(user.Identity?.IsAuthenticated == true
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
		}
	}

	private void RegisterTestConfiguration()
	{
		AddAuthorization();
		Services.AddHttpClient();
		Services.AddSingleton<AuthenticationStateProvider, TestAuthStateProvider>();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Auth0:Domain"] = "", ["Auth0:ClientId"] = "", ["Auth0:ClientSecret"] = ""
			})
			.Build());
	}

	private IRenderedComponent<CascadingAuthenticationState> RenderMainLayout()
	{
		return Render<CascadingAuthenticationState>(parameters =>
			parameters.AddChildContent<Web.Components.Layout.MainLayout>());
	}

	[Fact]
	public void MainLayoutRendersNavigationAndContentShell()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();

		// Assert
		cut.Find("header").Should().NotBeNull();
		cut.Find("nav").TextContent.Should().Contain("Overview");
		cut.Find("nav").TextContent.Should().Contain("test-user");
		cut.Find("nav").TextContent.Should().Contain("Logout");
		cut.Find("main").Should().NotBeNull();
		cut.Find("article").Should().NotBeNull();
		cut.Find("header").ClassList.Should().Contain("app-header");
		cut.Markup.Should().Contain("Articles");
	}

	[Fact]
	public void NavMenuShowsLoginWhenUsingLocalOrPlaceholderAuthConfiguration()
	{
		// Arrange
		AddAuthorization();
		Services.AddHttpClient();
		Services.AddSingleton<AuthenticationStateProvider, TestAnonymousAuthStateProvider>();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Auth0:Domain"] = "test.auth0.com",
				["Auth0:ClientId"] = "test-client-id",
				["Auth0:ClientSecret"] = "test-client-secret",
			})
			.Build());
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);

		// Act
		var cut = RenderMainLayout();

		// Assert
		cut.FindAll("a").Should().Contain(item => item.TextContent.Contains("Login", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ThemeToggleSwitchesBetweenLightAndDarkState()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();
		var toggle = cut.Find("button[aria-label='Switch to dark theme']");

		// Act
		cut.WaitForAssertion(() => toggle.TextContent.Trim().Should().Be("🌙"));
		toggle.Click();

		// Assert
		cut.WaitForAssertion(() =>
			cut.Find("button[aria-label='Switch to light theme']").TextContent.Trim().Should().Be("☀️"));
		var applyThemeInvocation = JSInterop.Invocations
			.Where(invocation => invocation.Identifier == "applyTheme")
			.LastOrDefault();

		applyThemeInvocation.Should().NotBeNull();
		applyThemeInvocation!.Arguments.Count.Should().Be(1);
		applyThemeInvocation.Arguments[0].Should().Be("dark");
	}

	[Fact]
	public void DefaultThemeUsesJavaScriptThemeStateWhenRendering()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("dark");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();

		// Assert
		cut.WaitForAssertion(() =>
			cut.Find("button[aria-label='Switch to light theme']").TextContent.Trim().Should().Be("☀️"));
	}

	[Fact]
	public void MainLayoutRendersPageShellWithHiddenNavAndDarkThemeToggle()
	{
		// Arrange
		RegisterTestConfiguration();
		JSInterop.Setup<string>("getTheme").SetResult("light");
		JSInterop.SetupVoid("applyTheme", _ => true);
		var cut = RenderMainLayout();
		var shell = cut.FindAll("div").FirstOrDefault(div =>
			div.ClassList.Contains("app-page"));

		// Assert
		shell.Should().NotBeNull();
		cut.Find("button[aria-label='Switch to dark theme']").Should().NotBeNull();
		cut.Find("nav").ClassList.Should().Contain("hidden");
		cut.Find("nav").ClassList.Should().Contain("app-nav");
	}

	[Fact]
	public async Task GitHubMetadataProviderUsesDefaultBranchWhenResolvingLastCommit()
	{
		// Arrange
		Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", "https://github.com/mpaulosky/Articles.git");

		using var handler = new StubHttpMessageHandler(request =>
		{
			if (request.RequestUri is null)
			{
				return new HttpResponseMessage(HttpStatusCode.BadRequest);
			}

			if (request.RequestUri.AbsoluteUri.Contains("/releases/latest", StringComparison.OrdinalIgnoreCase))
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"tag_name\":\"v1.2.3\"}", Encoding.UTF8, "application/json")
				};
			}

			if (request.RequestUri.AbsoluteUri.Contains("/commits/", StringComparison.OrdinalIgnoreCase))
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"sha\":\"abcdef1234567890\"}", Encoding.UTF8, "application/json")
				};
			}

			if (request.RequestUri.AbsoluteUri.Contains("/repos/mpaulosky/Articles", StringComparison.OrdinalIgnoreCase))
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"default_branch\":\"main\"}", Encoding.UTF8, "application/json")
				};
			}

			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		using var httpClient = new HttpClient(handler);

		// Act
		var metadata =
			await GitHubMetadataProvider.GetMetadataAsync(httpClient, Xunit.TestContext.Current.CancellationToken);

		// Assert
		metadata.Should().NotBeNull();
		metadata!.ReleaseTag.Should().Be("v1.2.3");
		metadata.LastCommit.Should().Be("abcdef1");
	}

	[Fact]
	public async Task FooterUsesGitHubReleaseMetadataWhenBuildInfoIsPlaceholder()
	{
		// Arrange
		using var handler = new StubHttpMessageHandler(request =>
		{
			if (request.RequestUri?.AbsoluteUri.Contains("/releases/latest", StringComparison.OrdinalIgnoreCase) == true)
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"tag_name\":\"v1.2.3\"}", Encoding.UTF8, "application/json")
				};
			}

			if (request.RequestUri?.AbsoluteUri.Contains("/commits/", StringComparison.OrdinalIgnoreCase) == true)
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"sha\":\"abcdef1234567890\"}", Encoding.UTF8, "application/json")
				};
			}

			if (request.RequestUri?.AbsoluteUri.Contains("/repos/mpaulosky/Articles", StringComparison.OrdinalIgnoreCase) ==
			    true)
			{
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("{\"default_branch\":\"main\"}", Encoding.UTF8, "application/json")
				};
			}

			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		Services.AddSingleton<IHttpClientFactory>(new TestHttpClientFactory(new HttpClient(handler)));

		// Act
		var cut = Render<Web.Components.Layout.Footer>();

		// Assert
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("v1.2.3"));
	}

	private sealed class StubHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

		public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
		{
			_handler = handler;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(_handler(request));
		}
	}

	private sealed class TestHttpClientFactory : IHttpClientFactory
	{
		private readonly HttpClient _httpClient;

		public TestHttpClientFactory(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public HttpClient CreateClient(string name)
		{
			return _httpClient;
		}
	}
}
