// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AssemblyInfo.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

[assembly: System.CLSCompliant(false)]

// Boots the app host once per assembly run rather than once per collection; collection fixtures
// (e.g. AdminAuthFixture) can then depend on it via constructor injection.
[assembly: AssemblyFixture(typeof(Web.E2E.Tests.PlaywrightAppFixture))]
