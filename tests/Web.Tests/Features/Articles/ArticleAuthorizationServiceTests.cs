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

	[Fact]
	public void CanViewArticle_ReturnsFalse_WhenUserIsNull()
	{
		// Arrange
		var article = CreateArticle("article-7", "Title", "author-1", published: true);

		// Act
		var canView = ArticleAuthorizationService.CanViewArticle(null, article);

		// Assert
		canView.Should().BeFalse();
	}

	[Fact]
	public void CanViewArticle_ReturnsFalse_WhenUserIsNotAuthenticated()
	{
		// Arrange
		var unauthUser = new ClaimsPrincipal(new ClaimsIdentity());
		var article = CreateArticle("article-8", "Title", "author-1", published: true);

		// Act
		var canView = ArticleAuthorizationService.CanViewArticle(unauthUser, article);

		// Assert
		canView.Should().BeFalse();
	}

	[Fact]
	public void CanViewArticle_ThrowsArgumentNullException_WhenArticleIsNull()
	{
		// Arrange
		var user = CreateUser("user-1", "User");

		// Act
		var act = () => ArticleAuthorizationService.CanViewArticle(user, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void CanViewArticle_AdminCanViewUnpublishedArticle()
	{
		// Arrange
		var admin = CreateUser("admin-1", "Admin");
		var unpublished = CreateArticle("article-9", "Draft", "author-99", published: false);

		// Act
		var canView = ArticleAuthorizationService.CanViewArticle(admin, unpublished);

		// Assert
		canView.Should().BeTrue();
	}

	[Fact]
	public void CanViewArticle_ReturnsFalse_ForUnpublishedArticleWhenUserIsRegularUser()
	{
		// Arrange
		var user = CreateUser("user-1", "Reader");
		var unpublished = CreateArticle("article-10", "Draft", "author-99", published: false);

		// Act
		var canView = ArticleAuthorizationService.CanViewArticle(user, unpublished);

		// Assert
		canView.Should().BeFalse();
	}

	[Fact]
	public void CanEditArticle_ReturnsFalse_WhenUserIsNull()
	{
		// Arrange
		var article = CreateArticle("article-11", "Title", "author-1", published: true);

		// Act
		var canEdit = ArticleAuthorizationService.CanEditArticle(null, article);

		// Assert
		canEdit.Should().BeFalse();
	}

	[Fact]
	public void CanEditArticle_ReturnsFalse_WhenUserIsNotAuthenticated()
	{
		// Arrange
		var unauthUser = new ClaimsPrincipal(new ClaimsIdentity());
		var article = CreateArticle("article-12", "Title", "author-1", published: true);

		// Act
		var canEdit = ArticleAuthorizationService.CanEditArticle(unauthUser, article);

		// Assert
		canEdit.Should().BeFalse();
	}

	[Fact]
	public void CanEditArticle_ThrowsArgumentNullException_WhenArticleIsNull()
	{
		// Arrange
		var user = CreateUser("user-1", "Admin");

		// Act
		var act = () => ArticleAuthorizationService.CanEditArticle(user, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void CanEditArticle_ReturnsFalse_WhenUserHasOtherRole()
	{
		// Arrange
		var user = CreateUser("user-1", "Reader");
		var article = CreateArticle("article-13", "Title", "author-1", published: true);

		// Act
		var canEdit = ArticleAuthorizationService.CanEditArticle(user, article);

		// Assert
		canEdit.Should().BeFalse();
	}

	[Fact]
	public void CanArchiveArticle_AllowsAdminToArchiveAnyArticle()
	{
		// Arrange
		var admin = CreateUser("admin-1", "Admin");
		var article = CreateArticle("article-14", "Admin article", "author-3", published: true);

		// Act
		var canArchive = ArticleAuthorizationService.CanArchiveArticle(admin, article);

		// Assert
		canArchive.Should().BeTrue();
	}

	[Fact]
	public void CanArchiveArticle_DeniesAuthorEvenOnTheirOwnArticle()
	{
		// Arrange
		var author = CreateUser("author-1", "Author");
		var ownArticle = CreateArticle("article-15", "My article", "author-1", published: true);

		// Act
		var canArchive = ArticleAuthorizationService.CanArchiveArticle(author, ownArticle);

		// Assert
		canArchive.Should().BeFalse();
	}

	[Fact]
	public void CanArchiveArticle_ReturnsFalse_WhenUserIsNull()
	{
		// Arrange
		var article = CreateArticle("article-16", "Title", "author-1", published: true);

		// Act
		var canArchive = ArticleAuthorizationService.CanArchiveArticle(null, article);

		// Assert
		canArchive.Should().BeFalse();
	}

	[Fact]
	public void CanArchiveArticle_ReturnsFalse_WhenUserIsNotAuthenticated()
	{
		// Arrange
		var unauthUser = new ClaimsPrincipal(new ClaimsIdentity());
		var article = CreateArticle("article-17", "Title", "author-1", published: true);

		// Act
		var canArchive = ArticleAuthorizationService.CanArchiveArticle(unauthUser, article);

		// Assert
		canArchive.Should().BeFalse();
	}

	[Fact]
	public void CanArchiveArticle_ReturnsFalse_WhenUserHasOtherRole()
	{
		// Arrange
		var user = CreateUser("user-1", "Reader");
		var article = CreateArticle("article-18", "Title", "author-1", published: true);

		// Act
		var canArchive = ArticleAuthorizationService.CanArchiveArticle(user, article);

		// Assert
		canArchive.Should().BeFalse();
	}

	[Fact]
	public void CanArchiveArticle_ThrowsArgumentNullException_WhenArticleIsNull()
	{
		// Arrange
		var user = CreateUser("admin-1", "Admin");

		// Act
		var act = () => ArticleAuthorizationService.CanArchiveArticle(user, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void GetCurrentUserId_ReturnsNull_WhenUserIsNull()
	{
		// Act
		var userId = ArticleAuthorizationService.GetCurrentUserId(null);

		// Assert
		userId.Should().BeNull();
	}

	[Theory]
	[InlineData("sub", "sub-123")]
	[InlineData("user_id", "user-id-456")]
	[InlineData("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "soap-id-789")]
	public void GetCurrentUserId_ResolvesAlternativeClaims(string claimType, string expectedUserId)
	{
		// Arrange
		var identity = new ClaimsIdentity([new Claim(claimType, expectedUserId)], "TestAuth");
		var principal = new ClaimsPrincipal(identity);

		// Act
		var userId = ArticleAuthorizationService.GetCurrentUserId(principal);

		// Assert
		userId.Should().Be(expectedUserId);
	}

	[Theory]
	[InlineData(null, "Admin", false)]
	[InlineData("user", "", false)]
	[InlineData("user", "  ", false)]
	public void IsInRole_ReturnsFalse_WhenUserIsNull_OrRoleIsBlank(string? userType, string role, bool expected)
	{
		// Arrange
		var principal = userType != null ? CreateUser("u1", "Admin") : null;

		// Act
		var result = ArticleAuthorizationService.IsInRole(principal, role);

		// Assert
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData("roles", "[\"Admin\", \"Editor\"]", "Admin", true)]
	[InlineData("roles", "[\"Admin\", \"Editor\"]", "Editor", true)]
	[InlineData("roles", "[\"Admin\", \"Editor\"]", "Author", false)]
	[InlineData("role", "Admin, Editor, Author", "Author", true)]
	[InlineData("https://articles/roles", "Admin", "Admin", true)]
	[InlineData("https://myblog/roles", "Admin", "admin", true)]
	[InlineData("roles", "[invalid json", "Admin", false)]
	[InlineData("other_claim", "Admin", "Admin", false)]
	public void IsInRole_EvaluatesDifferentClaimTypesAndFormats(string claimType, string claimValue, string checkRole, bool expected)
	{
		// Arrange
		var identity = new ClaimsIdentity([new Claim(claimType, claimValue)], "TestAuth");
		var principal = new ClaimsPrincipal(identity);

		// Act
		var result = ArticleAuthorizationService.IsInRole(principal, checkRole);

		// Assert
		result.Should().Be(expected);
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
			"test-slug",
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
			published,
			published ? DateTime.UtcNow : null);
	}
}
