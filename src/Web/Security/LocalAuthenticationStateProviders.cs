using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Web.Security;

public static class AuthenticationServiceCollectionExtensions
{
	public const string LocalTestAuthenticationScheme = "LocalTestAuth";
	public const string LocalAnonymousAuthenticationScheme = "LocalAnonymousAuth";

	public static IServiceCollection AddLocalAuthentication(this IServiceCollection services, bool shouldUseLocalTestLogin)
	{
		var schemeName = shouldUseLocalTestLogin ? LocalTestAuthenticationScheme : LocalAnonymousAuthenticationScheme;

		services.AddAuthentication(options =>
		{
			options.DefaultScheme = schemeName;
			options.DefaultAuthenticateScheme = schemeName;
			options.DefaultChallengeScheme = schemeName;
		})
		.AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(schemeName, _ => { });

		services.AddScoped<AuthenticationStateProvider>(_ =>
			shouldUseLocalTestLogin
				? new LocalTestAuthenticationStateProvider()
				: new LocalAnonymousAuthenticationStateProvider());

		return services;
	}
}

internal sealed class LocalAnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
		return Task.FromResult(new AuthenticationState(anonymous));
	}
}

internal sealed class LocalTestAuthenticationStateProvider : AuthenticationStateProvider
{
	public static ClaimsPrincipal CreatePrincipal()
	{
		var identity = new ClaimsIdentity(
		[
			new Claim(ClaimTypes.Name, "Test User"),
			new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
			new Claim(ClaimTypes.Email, "test.user@example.com"),
			new Claim("nickname", "test-user"),
			new Claim("picture", "https://example.com/avatar.png"),
			new Claim("https://myblog/roles", "Admin,Author"),
			new Claim(ClaimTypes.Role, "Admin"),
			new Claim(ClaimTypes.Role, "Author")
		],
		AuthenticationServiceCollectionExtensions.LocalTestAuthenticationScheme);

		return new ClaimsPrincipal(identity);
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		var principal = CreatePrincipal();
		return Task.FromResult(new AuthenticationState(principal));
	}
}

internal sealed class LocalAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (Scheme.Name == AuthenticationServiceCollectionExtensions.LocalTestAuthenticationScheme)
		{
			var principal = LocalTestAuthenticationStateProvider.CreatePrincipal();
			var ticket = new AuthenticationTicket(principal, Scheme.Name);
			return Task.FromResult(AuthenticateResult.Success(ticket));
		}

		var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
		var anonymousTicket = new AuthenticationTicket(anonymous, Scheme.Name);
		return Task.FromResult(AuthenticateResult.Success(anonymousTicket));
	}
}
