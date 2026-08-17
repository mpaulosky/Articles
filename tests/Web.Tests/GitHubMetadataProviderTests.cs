using FluentAssertions;
using Web.Services;

namespace Web.Tests;

public class GitHubMetadataProviderTests
{
	[Theory]
	[InlineData("https://github.com/mpaulosky/Articles.git", "mpaulosky", "Articles")]
	[InlineData("git@github.com:mpaulosky/Articles.git", "mpaulosky", "Articles")]
	[InlineData("ssh://git@github.com/mpaulosky/Articles.git", "mpaulosky", "Articles")]
	public void TryParseGitHubRepository_ParsesGitHubRemotes(string remoteUrl, string expectedOwner, string expectedRepo)
	{
		// Arrange
		// Act
		var parsed = GitHubMetadataProvider.TryParseGitHubRepository(remoteUrl, out var owner, out var repo);

		// Assert
		parsed.Should().BeTrue();
		owner.Should().Be(expectedOwner);
		repo.Should().Be(expectedRepo);
	}

	[Fact]
	public void TryParseGitHubRepository_RejectsNonGitHubRemotes()
	{
		// Arrange
		const string remoteUrl = "https://gitlab.com/mpaulosky/Articles.git";

		// Act
		var parsed = GitHubMetadataProvider.TryParseGitHubRepository(remoteUrl, out var owner, out var repo);

		// Assert
		parsed.Should().BeFalse();
		owner.Should().BeEmpty();
		repo.Should().BeEmpty();
	}
}
