// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     BoundaryTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Architecture.Tests
// =============================================

namespace Architecture.Tests;

public class BoundaryTests
{
	private static readonly Assembly DomainAssembly = typeof(ProjectName.Domain.AssemblyMarker).Assembly;

	[Theory]
	[InlineData("Web", "the presentation layer must depend on Domain, never the reverse")]
	[InlineData("AppHost", "host orchestration belongs to composition root concerns")]
	[InlineData("ServiceDefaults", "service wiring and defaults are infrastructure concerns")]
	public void DomainShouldNotDependOnOuterSolutionLayers(string forbiddenDependency, string reason)
	{
		// Arrange
		var dependency = forbiddenDependency;

		// Act
		var result = Types
			.InAssembly(DomainAssembly)
			.ShouldNot()
			.HaveDependencyOn(dependency)
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: $"Domain must stay pure and cannot depend on '{dependency}' because {reason}");
	}

	[Theory]
	[InlineData("Microsoft.AspNetCore", "HTTP pipeline concerns must stay outside Domain")]
	[InlineData("Microsoft.Extensions.Hosting", "host lifecycle concerns belong to infrastructure")]
	[InlineData("System.Net.Http", "external I/O should be abstracted behind outer layers")]
	[InlineData("System.Data", "persistence implementation details belong to infrastructure")]
	public void DomainShouldNotDependOnInfrastructureFrameworks(string forbiddenDependency, string reason)
	{
		// Arrange
		var dependency = forbiddenDependency;

		// Act
		var result = Types
			.InAssembly(DomainAssembly)
			.ShouldNot()
			.HaveDependencyOn(dependency)
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: $"Domain must remain framework-agnostic and cannot depend on '{dependency}' because {reason}");
	}
}