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
using Web.Components.Features.UserManagement.Caching.Extensions;
using Web.Data;
using Web.Security;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
DevelopmentStaticWebAssets.EnableForDevelopment(builder);
var redisConnectionString = builder.Configuration.GetConnectionString(RedisCache);
var useRedisCache = !string.IsNullOrWhiteSpace(redisConnectionString);

// --- Configuration Registration ---
IConfiguration configuration = builder.Configuration;

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
builder.Services.AddScoped<ArticleRepository>();
builder.Services.AddScoped<CategoryRepository>();

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

	await httpContext.SignOutAsync(Auth0Constants.AuthenticationScheme, authenticationProperties).ConfigureAwait(false);
	await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
});

app.Run();
