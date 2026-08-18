// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Program.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Auth0.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using Web.Components;
using Web.Components.Features.UserManagement.Caching.Extensions;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
var disableRedis = builder.Configuration.GetValue("DisableRedis", false);

// --- Configuration Registration ---
IConfiguration configuration = builder.Configuration;

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
if (!disableRedis)
{
	builder.AddRedisOutputCache("cache");
}
else
{
	builder.Services.AddDistributedMemoryCache();
}

// Authentication & Authorization
builder.Services.AddAuthenticationAndAuthorization(configuration);

builder.Services.AddMemoryCache();
builder.Services.AddUserManagementCaching();
builder.Services.AddMyMediator(typeof(Program).Assembly);

// Output Cache
builder.Services.AddOutputCache();

builder.Services.AddHttpClient();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

// Statically files middleware first
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

if (!app.Configuration.GetValue("DisableRedis", false))
{
	app.UseOutputCache();
}

app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapGet("/Account/Login", async (HttpContext httpContext, string returnUrl = "/") =>
{
	var authenticationProperties = new LoginAuthenticationPropertiesBuilder()
		.WithRedirectUri(returnUrl)
		.Build();

	await httpContext.ChallengeAsync(Auth0Constants.AuthenticationScheme, authenticationProperties).ConfigureAwait(false);
});

app.MapGet("/Account/Logout", async httpContext =>
{
	var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
		.WithRedirectUri("/")
		.Build();

	await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties).ConfigureAwait(false);
	await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
});

app.Run();
