// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AssemblyCoverageTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Architecture.Tests
// =============================================

namespace Architecture.Tests;

public class AssemblyCoverageTests
{
	private static readonly string[] ProductionAssemblyNames =
	[
		"Domain",
		"Web",
		"ServiceDefaults",
		"AppHost"
	];

	[Fact]
	public void ArchitectureTestsProjectShouldReferenceAllProductionProjects()
	{
		// Arrange
		var csprojPath = Path.Combine(GetRepositoryRoot(), "tests", "Architecture.Tests", "Architecture.Tests.csproj");
		var projectDocument = XDocument.Load(csprojPath);

		// Act
		var projectReferences = projectDocument
			.Descendants("ProjectReference")
			.Select(element => element.Attribute("Include")?.Value)
			.Where(include => !string.IsNullOrWhiteSpace(include))
			.Select(include => include!.Replace('\\', '/'))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// Assert
		projectReferences.Should().BeEquivalentTo(
		[
			"../../src/Domain/Domain.csproj",
			"../../src/Web/Web.csproj",
			"../../src/ServiceDefaults/ServiceDefaults.csproj",
			"../../src/AppHost/AppHost.csproj"
		]);
	}

	[Fact]
	public void BuildOutputPathsShouldUseForwardSlashesToAvoidWatchPathIssues()
	{
		// Arrange
		var propertyFilePath = Path.Combine(GetRepositoryRoot(), "Directory.Build.props");

		// Act
		var propertyFileContents = File.ReadAllText(propertyFilePath);

		// Assert
		propertyFileContents.Should().Contain("<BaseIntermediateOutputPath>$(MSBuildProjectDirectory)/obj/</BaseIntermediateOutputPath>");
		propertyFileContents.Should().Contain("<BaseOutputPath>$(MSBuildProjectDirectory)/bin/</BaseOutputPath>");
		propertyFileContents.Should().NotContain("\\Debug");
	}

	[Fact]
	public void ShouldLoadAllProductionAssembliesForArchitectureValidation()
	{
		// Arrange
		var loadedAssemblies = ProductionAssemblyNames
			.Select(LoadAssembly)
			.ToArray();

		// Act
		var assemblyNames = loadedAssemblies.Select(assembly => assembly.GetName().Name).ToArray();

		// Assert
		loadedAssemblies.Should().HaveCount(ProductionAssemblyNames.Length);
		assemblyNames.Should().BeEquivalentTo(ProductionAssemblyNames);
	}

	[Fact]
	public void ProductionProjectsShouldFollowExpectedHighLevelTopology()
	{
		// Arrange
		var repositoryRoot = GetRepositoryRoot();
		var appHostProjectReferences =
			ReadProjectReferences(Path.Combine(repositoryRoot, "src", "AppHost", "AppHost.csproj"));
		var webProjectReferences = ReadProjectReferences(Path.Combine(repositoryRoot, "src", "Web", "Web.csproj"));
		var domainProjectReferences = ReadProjectReferences(Path.Combine(repositoryRoot, "src", "Domain", "Domain.csproj"));
		var serviceDefaultsProjectReferences =
			ReadProjectReferences(Path.Combine(repositoryRoot, "src", "ServiceDefaults", "ServiceDefaults.csproj"));
		var domainAssembly = typeof(ProjectName.Domain.AssemblyMarker).Assembly;
		var webAssembly = typeof(Web.AssemblyMarker).Assembly;
		var serviceDefaultsAssembly = typeof(Extensions).Assembly;

		// Act
		var appHostDependencies = appHostProjectReferences;
		var webDependencies = webProjectReferences;
		var domainDependencies = domainProjectReferences;
		var serviceDefaultsDependencies = serviceDefaultsProjectReferences;

		// Assert
		appHostDependencies.Should().BeEquivalentTo(["../Web/Web.csproj"]);
		webDependencies.Should().BeEquivalentTo(["../Domain/Domain.csproj", "../ServiceDefaults/ServiceDefaults.csproj"]);
		domainDependencies.Should().BeEmpty("the domain model should stay isolated from app-layer projects");
		serviceDefaultsDependencies.Should()
			.BeEmpty("ServiceDefaults should remain reusable across hostable services");

		AssertReferences(webAssembly, mustContain: ["Domain", "ServiceDefaults"], mustNotContain: ["AppHost"]);
		AssertReferences(serviceDefaultsAssembly, mustNotContain: ["Web", "AppHost"]);
	}

	private static void AssertReferences(
		Assembly assembly,
		string[]? mustContain = null,
		string[]? mustNotContain = null)
	{
		// Arrange
		var referenceNames = assembly.GetReferencedAssemblies()
			.Select(name => name.Name)
			.Where(name => !string.IsNullOrWhiteSpace(name))
			.Select(name => name!)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		// Act
		if (mustContain is not null)
		{
			foreach (var dependency in mustContain)
			{
				referenceNames.Should().Contain(dependency,
					because: $"{assembly.GetName().Name} should reference {dependency}");
			}
		}

		if (mustNotContain is not null)
		{
			foreach (var dependency in mustNotContain)
			{
				referenceNames.Should().NotContain(dependency,
					because: $"{assembly.GetName().Name} must not reference {dependency}");
			}
		}
	}

	private static Assembly LoadAssembly(string assemblyName)
	{
		var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(assembly =>
				string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

		if (loadedAssembly is not null)
		{
			return loadedAssembly;
		}

		var assemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
		if (File.Exists(assemblyPath))
		{
			return Assembly.LoadFrom(assemblyPath);
		}

		return Assembly.Load(new AssemblyName(assemblyName));
	}

	private static HashSet<string> ReadProjectReferences(string projectPath)
	{
		var projectDocument = XDocument.Load(projectPath);

		return projectDocument
			.Descendants("ProjectReference")
			.Select(element => element.Attribute("Include")?.Value)
			.Where(include => !string.IsNullOrWhiteSpace(include))
			.Select(include => include!.Replace('\\', '/'))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
	}

	private static string GetRepositoryRoot()
	{
		var current = new DirectoryInfo(AppContext.BaseDirectory);

		while (current is not null)
		{
			var hasSrc = Directory.Exists(Path.Combine(current.FullName, "src"));
			var hasTests = Directory.Exists(Path.Combine(current.FullName, "tests"));
			if (hasSrc && hasTests)
			{
				return current.FullName;
			}

			current = current.Parent;
		}

		throw new DirectoryNotFoundException("Could not locate the repository root from test output path.");
	}
}