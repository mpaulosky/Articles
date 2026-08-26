// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     E2ETestCollectionDefinition.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.E2E.Tests
// =============================================

namespace Web.E2E.Tests;

/// <summary>
/// xUnit collection shared by every E2E test class, so per-role auth fixtures are started once for
/// the collection instead of once per test class. The app host itself is an assembly fixture
/// (see AssemblyInfo.cs), since collection fixtures can depend on assembly fixtures but not on
/// each other.
/// </summary>
[CollectionDefinition(Name)]
public sealed class E2ETestCollectionDefinition :
	ICollectionFixture<AdminAuthFixture>, ICollectionFixture<AuthorAuthFixture>, ICollectionFixture<UserAuthFixture>
{
	public const string Name = "E2E";
}
