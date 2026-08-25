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

using static Domain.Constants.ApplicationConstants;

using Microsoft.EntityFrameworkCore;

using Web;
using Web.Components;
using Web.Components.Features.Articles.Services;
using Web.Components.Features.UserManagement.Auth0;
using Web.Components.Features.UserManagement.Caching.Extensions;
using Web.Data;
using Web.Security;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
DevelopmentStaticWebAssets.EnableForDevelopment(builder);
var redisConnectionString = builder.Configuration.GetConnectionString(RedisCache);
var useRedisCache = !string.IsNullOrWhiteSpace(redisConnectionString);

// --- Configuration Registration ---
ConfigurationManager configuration = builder.Configuration;

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
if (useRedisCache)
{
	builder.AddRedisOutputCache(OutputCache);
	builder.AddRedisDistributedCache(RedisCache);
}
else
{
	builder.Services.AddDistributedMemoryCache();
}

// Authentication & Authorization
builder.Services.AddAuthenticationAndAuthorization(configuration);

builder.Services.AddMemoryCache();
builder.Services.AddUserManagementCaching();
builder.Services.AddAuth0ManagementApiClient();
builder.Services.AddMyMediator(typeof(Program).Assembly);

// Cross-cutting request/response logging via the mediator pipeline.
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

var mongoConnectionString = configuration.GetConnectionString("articlesdb")
                            ?? configuration["MONGODB_CONNECTION_STRING"]
                            ?? "mongodb://localhost:27017";
var mongoDatabaseName = configuration["MONGODB_DATABASE_NAME"] ?? "articlesdb";

builder.Services.AddDbContextFactory<ArticlesMongoDbContext>(options =>
{
	options.UseMongoDB(mongoConnectionString, mongoDatabaseName);
});
builder.Services.AddSingleton<IImageOptimizer, ImageOptimizer>();
builder.Services.AddScoped<IFileStorage, FileStorage>();
builder.Services.AddScoped<ArticleRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddHostedService<ArticleImageBackfillHostedService>();

// Output Cache
builder.Services.AddOutputCache();

builder.Services.AddHttpClient();

builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents()
	// SignalR's default 32 KB message limit is far below what a base64-encoded image upload
	// needs (FileStorage.AddFile allows up to 10 MB, and base64 inflates that by ~33%); without
	// this the circuit silently disconnects and reconnects mid-upload, leaving the editor stuck
	// showing "Uploading image..." forever.
	.AddHubOptions(options => options.MaximumReceiveMessageSize = 15 * 1024 * 1024);

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

if (useRedisCache)
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

	var isAuth0Enabled = Auth0ConfigurationHelper.IsAuthenticationEnabled(
		app.Configuration["Auth0:Domain"],
		app.Configuration["Auth0:ClientId"],
		app.Configuration["Auth0:ClientSecret"]);

	var scheme = isAuth0Enabled
		? Auth0Constants.AuthenticationScheme
		: CookieAuthenticationDefaults.AuthenticationScheme;

	await httpContext.ChallengeAsync(scheme, authenticationProperties).ConfigureAwait(false);
});

app.MapGet("/Account/Logout", async httpContext =>
{
	var authenticationProperties = new LogoutAuthenticationPropertiesBuilder()
		.WithRedirectUri("/")
		.Build();

	var isAuth0Enabled = Auth0ConfigurationHelper.IsAuthenticationEnabled(
		app.Configuration["Auth0:Domain"],
		app.Configuration["Auth0:ClientId"],
		app.Configuration["Auth0:ClientSecret"]);

	if (isAuth0Enabled)
	{
		await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties).ConfigureAwait(false);
	}

	await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticationProperties).ConfigureAwait(false);
});

app.Run();
