using System.Diagnostics;
using System.Net;
using System.Text;

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
	public async Task GetMetadataAsync_UsesLocalGitFallbackWhenGitHubApiIsRateLimited()
	{
		// Arrange
		var tempRoot = Path.Combine(Path.GetTempPath(), $"git-metadata-provider-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempRoot);
		try
		{
			RunGit(tempRoot, "init");
			RunGit(tempRoot, "remote", "add", "origin", "https://github.com/mpaulosky/Articles.git");
			RunGit(tempRoot, "config", "user.name", "Test User");
			RunGit(tempRoot, "config", "user.email", "test@example.com");
			File.WriteAllText(Path.Combine(tempRoot, "README.md"), "test");
			RunGit(tempRoot, "add", "README.md");
			RunGit(tempRoot, "commit", "-m", "initial commit");
			RunGit(tempRoot, "tag", "v1.2.3");

			var originalCurrentDirectory = Environment.CurrentDirectory;
			Environment.CurrentDirectory = tempRoot;
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", "https://github.com/mpaulosky/Articles.git");

			using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
				new HttpResponseMessage(HttpStatusCode.TooManyRequests)
				{
					Content = new StringContent("{\"message\":\"API rate limit exceeded\"}", Encoding.UTF8, "application/json")
				}));

			try
			{
				// Act
				var metadata = await GitHubMetadataProvider.GetMetadataAsync(httpClient, CancellationToken.None);

				// Assert
				metadata.Should().NotBeNull();
				metadata!.ReleaseTag.Should().Be("v1.2.3");
				metadata.LastCommit.Should().NotBeNullOrWhiteSpace();
			}
			finally
			{
				Environment.CurrentDirectory = originalCurrentDirectory;
				Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", null);
			}
		}
		finally
		{
			if (Directory.Exists(tempRoot))
			{
				Directory.Delete(tempRoot, recursive: true);
			}
		}
	}

	[Fact]
	public async Task GetMetadataAsync_WhenNoLocalTagExists_ReturnsNoReleaseValue()
	{
		// Arrange
		var tempRoot = Path.Combine(Path.GetTempPath(), $"git-metadata-provider-no-tag-{Guid.NewGuid():N}");
		Directory.CreateDirectory(tempRoot);
		try
		{
			RunGit(tempRoot, "init");
			RunGit(tempRoot, "remote", "add", "origin", "https://github.com/mpaulosky/Articles.git");
			RunGit(tempRoot, "config", "user.name", "Test User");
			RunGit(tempRoot, "config", "user.email", "test@example.com");
			File.WriteAllText(Path.Combine(tempRoot, "README.md"), "test");
			RunGit(tempRoot, "add", "README.md");
			RunGit(tempRoot, "commit", "-m", "initial commit");

			var originalCurrentDirectory = Environment.CurrentDirectory;
			Environment.CurrentDirectory = tempRoot;
			Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", "https://github.com/mpaulosky/Articles.git");

			using var httpClient = new HttpClient(new StubHttpMessageHandler(_ =>
				new HttpResponseMessage(HttpStatusCode.TooManyRequests)
				{
					Content = new StringContent("{\"message\":\"API rate limit exceeded\"}", Encoding.UTF8, "application/json")
				}));

			try
			{
				// Act
				var metadata = await GitHubMetadataProvider.GetMetadataAsync(httpClient, CancellationToken.None);

				// Assert
				metadata.Should().NotBeNull();
				metadata!.ReleaseTag.Should().Be("no release");
				metadata.LastCommit.Should().NotBeNullOrWhiteSpace();
			}
			finally
			{
				Environment.CurrentDirectory = originalCurrentDirectory;
				Environment.SetEnvironmentVariable("GITHUB_REPOSITORY_URL", null);
			}
		}
		finally
		{
			if (Directory.Exists(tempRoot))
			{
				Directory.Delete(tempRoot, recursive: true);
			}
		}
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

	private static void RunGit(string workingDirectory, params string[] arguments)
	{
		using var process = new Process();
		process.StartInfo = new ProcessStartInfo("git")
		{
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		foreach (var argument in arguments)
		{
			process.StartInfo.ArgumentList.Add(argument);
		}

		if (!process.Start())
		{
			throw new InvalidOperationException($"Unable to start git for '{string.Join(" ", arguments)}'.");
		}

		var stdout = process.StandardOutput.ReadToEnd();
		var stderr = process.StandardError.ReadToEnd();
		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"git {string.Join(" ", arguments)} failed: {stderr}\n{stdout}");
		}
	}

	private sealed class StubHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

		public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
		{
			_handler = handler;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(_handler(request));
		}
	}
}
