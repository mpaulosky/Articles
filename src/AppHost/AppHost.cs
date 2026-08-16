// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHost.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  AppHost
// =============================================

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

builder.AddProject<Projects.Web>("webfrontend")
	.WithExternalHttpEndpoints()
	.WithHttpHealthCheck("/health")
	.WithReference(cache)
	.WaitFor(cache);

builder.Build().Run();
