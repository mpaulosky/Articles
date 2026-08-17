using FluentAssertions;

namespace Web.Tests;

public class ThemeScriptBehaviorTests
{
	[Fact]
	public void AppShellUsesThemeStorageAndAvoidsBootstrapDependency()
	{
		// Arrange
		var appMarkup = ReadAppShellMarkup();

		// Assert
		appMarkup.Should().Contain("tailwind-color-theme");
		appMarkup.Should().Contain("localStorage");
		appMarkup.Should().Contain("document.documentElement");
		appMarkup.Should().Contain("root.classList.toggle(\"dark\", resolvedTheme === \"dark\")");
		appMarkup.Should().NotContain("bootstrap");
		appMarkup.Should().NotContain("Bootstrap");
	}

	[Fact]
	public void AppShellAppliesDeterministicThemePreferenceToRootElement()
	{
		// Arrange
		var appMarkup = ReadAppShellMarkup();

		// Assert
		appMarkup.Should().Contain("const getPreferredTheme = () =>");
		appMarkup.Should().Contain("return window.matchMedia(\"(prefers-color-scheme: dark)\").matches ? \"dark\" : \"light\";");
		appMarkup.Should().Contain("root.dataset.theme = resolvedTheme;");
		appMarkup.Should().Contain("root.style.colorScheme = resolvedTheme;");
		appMarkup.Should().Contain("root.classList.toggle(\"dark\", resolvedTheme === \"dark\");");
		appMarkup.Should().Contain("window.dispatchEvent(new CustomEvent(\"theme-change\", { detail: { theme } }));");
	}

	private static string ReadAppShellMarkup()
	{
		var root = FindRepositoryRoot();
		var appShellPath = Path.Combine(root, "src", "Web", "Components", "App.razor");
		return File.ReadAllText(appShellPath);
	}

	private static string FindRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Articles.slnx")))
		{
			directory = directory.Parent;
		}

		directory.Should().NotBeNull();
		return directory!.FullName;
	}
}
