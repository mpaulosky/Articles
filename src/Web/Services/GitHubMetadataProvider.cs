using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Web.Services;

[SuppressMessage("Design", "CA1515", Justification = "The metadata provider is intentionally consumed by the Blazor footer and its corresponding Web tests.")]
public sealed record GitHubMetadata(string ReleaseTag, string LastCommit);

[SuppressMessage("Design", "CA1515", Justification = "The metadata provider is intentionally consumed by the Blazor footer and its corresponding Web tests.")]
public static class GitHubMetadataProvider
{
	public static async Task<GitHubMetadata?> GetMetadataAsync(HttpClient httpClient, CancellationToken cancellationToken = default)
	{
		if (httpClient is null)
		{
			throw new ArgumentNullException(nameof(httpClient));
		}

		var remoteUrl = await GetOriginUrlAsync();
		if (!TryParseGitHubRepository(remoteUrl, out var owner, out var repo))
		{
			return null;
		}

		var repoDetails = await GetRepositoryDetailsAsync(httpClient, owner, repo, cancellationToken);
		var releaseTag = await GetLatestReleaseTagAsync(httpClient, owner, repo, cancellationToken);
		var defaultBranch = repoDetails?.DefaultBranch ?? "main";
		var lastCommit = await GetLastCommitAsync(httpClient, owner, repo, defaultBranch, cancellationToken);

		return new GitHubMetadata(
			releaseTag ?? "no release",
			lastCommit ?? "unknown");
	}

	[SuppressMessage("Design", "CA1054", Justification = "The Git origin string comes directly from git remotes and is normalized before being parsed.")]
	public static bool TryParseGitHubRepository(string? remoteUrl, out string owner, out string repo)
	{
		owner = string.Empty;
		repo = string.Empty;

		if (string.IsNullOrWhiteSpace(remoteUrl))
		{
			return false;
		}

		var normalized = remoteUrl.Trim();
		if (normalized.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
		{
			normalized = $"https://github.com/{normalized["git@github.com:".Length..]}";
		}
		else if (normalized.StartsWith("ssh://git@github.com/", StringComparison.OrdinalIgnoreCase))
		{
			normalized = normalized.Replace("ssh://git@github.com/", "https://github.com/");
		}

		if (!normalized.Contains("github.com", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		normalized = normalized.TrimEnd('/');
		var githubIndex = normalized.IndexOf("github.com", StringComparison.OrdinalIgnoreCase);
		var path = normalized[(githubIndex + "github.com".Length)..].TrimStart('/');
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (segments.Length < 2)
		{
			return false;
		}

		owner = segments[0];
		repo = segments[1];
		if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
		{
			repo = repo[..^4];
		}

		return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
	}

	private static async Task<string?> GetOriginUrlAsync()
	{
		var configuredRepositoryUrl = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_URL")
			?? Environment.GetEnvironmentVariable("REPOSITORY_URL");
		if (!string.IsNullOrWhiteSpace(configuredRepositoryUrl))
		{
			return configuredRepositoryUrl.Trim();
		}

		var configuredRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
		if (!string.IsNullOrWhiteSpace(configuredRepository))
		{
			return $"https://github.com/{configuredRepository.Trim()}.git";
		}

		foreach (var candidate in GetCandidateDirectories())
		{
			var gitRoot = FindGitRoot(candidate);
			if (gitRoot is null)
			{
				continue;
			}

			try
			{
				var startInfo = new ProcessStartInfo("git")
				{
					WorkingDirectory = gitRoot,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				};
				startInfo.ArgumentList.Add("remote");
				startInfo.ArgumentList.Add("get-url");
				startInfo.ArgumentList.Add("origin");

				using var process = Process.Start(startInfo);
				if (process is null)
				{
					continue;
				}

				var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
				await process.WaitForExitAsync().ConfigureAwait(false);

				if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
				{
					return output.Trim();
				}
			}
			catch
			{
				// Ignore and continue searching other candidate directories.
			}
		}

		return null;
	}

	private static IEnumerable<string> GetCandidateDirectories()
	{
		var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			Environment.CurrentDirectory,
			AppContext.BaseDirectory,
		};

		var current = new DirectoryInfo(Environment.CurrentDirectory);
		while (current is not null)
		{
			directories.Add(current.FullName);
			current = current.Parent;
		}

		var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
		while (baseDir is not null)
		{
			directories.Add(baseDir.FullName);
			baseDir = baseDir.Parent;
		}

		return directories;
	}

	private static string? FindGitRoot(string directory)
	{
		var current = new DirectoryInfo(directory);
		while (current is not null)
		{
			if (Directory.Exists(Path.Combine(current.FullName, ".git")))
			{
				return current.FullName;
			}

			current = current.Parent;
		}

		return null;
	}

	private static async Task<RepositoryDetails?> GetRepositoryDetailsAsync(HttpClient httpClient, string owner, string repo, CancellationToken cancellationToken)
	{
		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Articles-Web", "1.0"));

			using var response = await httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			return await response.Content.ReadFromJsonAsync<RepositoryDetails>(cancellationToken: cancellationToken);
		}
		catch
		{
			return null;
		}
	}

	private static async Task<string?> GetLatestReleaseTagAsync(HttpClient httpClient, string owner, string repo, CancellationToken cancellationToken)
	{
		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/latest");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Articles-Web", "1.0"));

			using var response = await httpClient.SendAsync(request, cancellationToken);
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				return null;
			}

			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken: cancellationToken);
			return release?.TagName;
		}
		catch
		{
			return null;
		}
	}

	private static async Task<string?> GetLastCommitAsync(HttpClient httpClient, string owner, string repo, string defaultBranch, CancellationToken cancellationToken)
	{
		try
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/commits/{Uri.EscapeDataString(defaultBranch)}");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Articles-Web", "1.0"));

			using var response = await httpClient.SendAsync(request, cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}

			var commit = await response.Content.ReadFromJsonAsync<GitHubCommit>(cancellationToken: cancellationToken);
			return commit?.Sha?[..7];
		}
		catch
		{
			return null;
		}
	}

	private sealed class RepositoryDetails
	{
		public string DefaultBranch { get; set; } = string.Empty;
	}

	private sealed class GitHubRelease
	{
		public string TagName { get; set; } = string.Empty;
	}

	private sealed class GitHubCommit
	{
		public string? Sha { get; set; }
	}
}
