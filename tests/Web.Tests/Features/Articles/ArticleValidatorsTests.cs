using FluentAssertions;

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Validators;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.Tests.Features.Articles;

public class ArticleValidatorsTests
{
	private readonly CreateArticleCommandValidator _createValidator = new();
	private readonly UpdateArticleCommandValidator _updateValidator = new();

	[Fact]
	public void CreateArticleCommandValidator_RejectsMissingRequiredFields()
	{
		// Arrange
		var command = new CreateArticleCommand("", "test-slug", "", null!, null);

		// Act
		var result = _createValidator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateArticleCommand.Title));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateArticleCommand.Content));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateArticleCommand.Author));
	}

	[Fact]
	public void CreateArticleCommandValidator_AllowsValidArticle()
	{
		// Arrange
		var command = new CreateArticleCommand(
			"Valid title",
			"valid-title",
			"This content is long enough to pass validation.",
			new AuthorDto("user-1", "Ada Lovelace", "ada@example.com"),
			new CategoryDto
			{
				Id = MongoDB.Bson.ObjectId.GenerateNewId(),
				CategoryName = "Technology",
				Slug = "technology",
				CreatedOn = DateTime.UtcNow,
				IsArchived = false
			});

		// Act
		var result = _createValidator.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
		result.Errors.Should().BeEmpty();
	}

	[Fact]
	public void UpdateArticleCommandValidator_RejectsEmptyIdAndShortFields()
	{
		// Arrange
		var command = new UpdateArticleCommand(string.Empty, "A", "a", "short");

		// Act
		var result = _updateValidator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateArticleCommand.Id));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateArticleCommand.Title));
		result.Errors.Should().Contain(error => error.PropertyName == nameof(UpdateArticleCommand.Content));
	}
}
