// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     Program.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Web;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);
var disableRedis = builder.Configuration.GetValue("DisableRedis", false);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
if (!disableRedis)
{
	builder.AddRedisOutputCache("cache");
}

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

if (!app.Configuration.GetValue("DisableRedis", false))
{
	app.UseOutputCache();
}

app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
