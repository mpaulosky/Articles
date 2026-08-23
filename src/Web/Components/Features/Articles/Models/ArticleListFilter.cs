// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleListFilter.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

namespace Web.Components.Features.Articles.Models;

/// <summary>
///     Holds the Articles list's search/filter criteria and applies them to a list of articles.
/// </summary>
public sealed class ArticleListFilter
{
	public bool ShowMyArticlesOnly { get; set; }
	public bool IncludeArchived { get; set; }
	public string SearchText { get; set; } = string.Empty;
	public string TitleFilter { get; set; } = string.Empty;
	public string AuthorFilter { get; set; } = string.Empty;
	public string CategoryFilter { get; set; } = string.Empty;
	public string StatusFilter { get; set; } = "All";
	public string? CurrentUserId { get; set; }

	/// <summary>
	///     Applies the current criteria to <paramref name="articles" />.
	/// </summary>
	public IReadOnlyList<ArticleDto> Apply(IEnumerable<ArticleDto> articles)
	{
		return articles
			.Where(article => !ShowMyArticlesOnly || IsOwnedByCurrentUser(article))
			.Where(article => IncludeArchived || !article.IsArchived)
			.Where(MatchesGlobalSearch)
			.Where(MatchesTitleFilter)
			.Where(MatchesAuthorFilter)
			.Where(MatchesCategoryFilter)
			.Where(MatchesStatusFilter)
			.ToList();
	}

	private bool IsOwnedByCurrentUser(ArticleDto article)
	{
		return !string.IsNullOrWhiteSpace(CurrentUserId) && article.Author.UserId == CurrentUserId;
	}

	private bool MatchesGlobalSearch(ArticleDto article)
	{
		return string.IsNullOrWhiteSpace(SearchText)
			|| article.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
			|| article.Author.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
	}

	private bool MatchesTitleFilter(ArticleDto article)
	{
		return string.IsNullOrWhiteSpace(TitleFilter)
			|| article.Title.Contains(TitleFilter, StringComparison.OrdinalIgnoreCase);
	}

	private bool MatchesAuthorFilter(ArticleDto article)
	{
		return string.IsNullOrWhiteSpace(AuthorFilter)
			|| article.Author.Name.Contains(AuthorFilter, StringComparison.OrdinalIgnoreCase);
	}

	private bool MatchesCategoryFilter(ArticleDto article)
	{
		return string.IsNullOrWhiteSpace(CategoryFilter)
			|| article.Category.Id.ToString() == CategoryFilter;
	}

	private bool MatchesStatusFilter(ArticleDto article)
	{
		return StatusFilter switch
		{
			"Published" => article.IsPublished,
			"Draft" => !article.IsPublished,
			_ => true
		};
	}
}
