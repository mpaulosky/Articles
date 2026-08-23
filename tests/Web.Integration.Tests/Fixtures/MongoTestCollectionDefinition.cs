// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoTestCollectionDefinition.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Integration.Tests
// =============================================

namespace Web.Integration.Tests.Fixtures;

/// <summary>
///     xUnit collection shared by every test class that needs the MongoDB TestContainer, so the
///     container is started once for the assembly instead of once per test class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MongoTestCollectionDefinition : ICollectionFixture<MongoContainerFixture>
{
	public const string Name = "Mongo integration tests";
}
