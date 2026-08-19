// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LayerTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Architecture.Tests
// =============================================

namespace Architecture.Tests;

public class LayerTests
{
	[Fact]
	public void DomainShouldNotReferenceSystemData()
	{
		// Arrange
		var dependency = "System.Data";

		// Act
		var result = Types
			.InAssembly(typeof(ProjectName.Domain.AssemblyMarker).Assembly)
			.ShouldNot()
			.HaveDependencyOn(dependency)
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "the Domain layer must remain free of infrastructure concerns");
	}

	[Fact]
	public void DomainShouldNotReferenceUi()
	{
		// Arrange
		var dependency = "Web";

		// Act
		var result = Types
			.InAssembly(typeof(ProjectName.Domain.AssemblyMarker).Assembly)
			.ShouldNot()
			.HaveDependencyOn(dependency)
			.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			because: "Domain must not depend on the presentation layer");
	}
}