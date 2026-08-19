using System.Security.Claims;

using FluentAssertions;

using Web.Components.Features.Articles.Authorization;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.Tests.Features.Articles;

public class ArticleAuthorizationServiceTests
{
	[Fact]
	public void CanViewArticle_AllowsAuthenticatedUsersToViewPublishedArticles()
	{
		// Arrange
		var user = CreateUser("user-1", "User");
		var article = CreateArticle("article-1", "Published article", "author-2", published: true);

		// Act
		var canView = ArticleAuthorizationService.CanViewArticle(user, article);

		// Assert
		canView.Should().BeTrue();
	}

	[Fact]
	public void CanEditArticle_AllowsAdminToEditAnyArticle()
	{
		// Arrange
		var admin = CreateUser("admin-1", "Admin");
		var article = CreateArticle("article-2", "Admin article", "author-3", published: true);

		// Act
		var canEdit = ArticleAuthorizationService.CanEditArticle(admin, article);

		// Assert
		canEdit.Should().BeTrue();
	}

	[Fact]
	public void CanEditArticle_AuthorCanOnlyEditTheirOwnArticles()
	{
		// Arrange
		var author = CreateUser("author-1", "Author");
		var ownArticle = CreateArticle("article-3", "My article", "author-1", published: true);
		var otherArticle = CreateArticle("article-4", "Someone else's article", "author-2", published: true);

		// Act
		var canEditOwn = ArticleAuthorizationService.CanEditArticle(author, ownArticle);
		var canEditOther = ArticleAuthorizationService.CanEditArticle(author, otherArticle);

		// Assert
		canEditOwn.Should().BeTrue();
		canEditOther.Should().BeFalse();
	}

	[Fact]
	public void CanViewArticle_AuthorCanOnlyViewTheirOwnArticlesWhenRoleIsAuthor()
	{
		// Arrange
		var author = CreateUser("author-1", "Author");
		var ownArticle = CreateArticle("article-5", "My published article", "author-1", published: true);
		var otherArticle = CreateArticle("article-6", "Someone else published article", "author-2", published: true);

		// Act
		var canViewOwn = ArticleAuthorizationService.CanViewArticle(author, ownArticle);
		var canViewOther = ArticleAuthorizationService.CanViewArticle(author, otherArticle);

		// Assert
		canViewOwn.Should().BeTrue();
		canViewOther.Should().BeFalse();
	}

	private static ClaimsPrincipal CreateUser(string userId, string role)
	{
		var identity = new ClaimsIdentity(
			[
				new Claim(ClaimTypes.NameIdentifier, userId),
				new Claim(ClaimTypes.Name, role + " User"),
				new Claim(ClaimTypes.Role, role)
			],
			"TestAuth");

		return new ClaimsPrincipal(identity);
	}

	private static ArticleDto CreateArticle(string id, string title, string authorUserId, bool published)
	{
		return new ArticleDto(
			id,
			title,
			"body",
			new AuthorDto(authorUserId, "Author Name", "author@example.com"),
			new CategoryDto
			{
				Id = MongoDB.Bson.ObjectId.GenerateNewId(),
				CategoryName = "General",
				Slug = "general",
				CreatedOn = DateTime.UtcNow,
				IsArchived = false
			},
			DateTime.UtcNow,
			null,
			published);
	}
}
