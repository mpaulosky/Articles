// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleValidators.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using MongoDB.Bson;
using FluentValidation;

using Web.Components.Features.Articles.Commands;

namespace Web.Components.Features.Articles.Validators;

/// <summary>
///     Validates the article creation command.
/// </summary>
internal sealed class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
	/// <summary>
	///     Initializes validation rules for article creation.
	/// </summary>
	public CreateArticleCommandValidator()
	{

		RuleFor(command => command.Title)
			.NotEmpty()
			.WithMessage("Title is required")
			.MaximumLength(100)
			.WithMessage("Title cannot exceed 100 characters");

		RuleFor(command => command.Content)
			.NotEmpty()
			.WithMessage("Content is required")
			.MaximumLength(50000)
			.WithMessage("Content cannot exceed 50000 characters");

		RuleFor(command => command.Slug)
			.NotEmpty()
			.WithMessage("Slug is required")
			.MaximumLength(200)
			.WithMessage("Slug cannot exceed 200 characters")
			.Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$")
			.WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

		RuleFor(command => command.Author)
			.NotNull()
			.WithMessage("Author is required");

		RuleFor(command => command.Category)
			.NotNull()
			.WithMessage("Category is required");

	}
}

/// <summary>
///     Validates the article update command.
/// </summary>
internal sealed class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
	/// <summary>
	///     Initializes validation rules for article updates.
	/// </summary>
	public UpdateArticleCommandValidator()
	{
		RuleFor(command => command.Id)
			.NotEmpty();

		RuleFor(command => command.Title)
			.NotEmpty()
			.MinimumLength(3)
			.MaximumLength(200);

		RuleFor(command => command.Content)
			.NotEmpty()
			.MinimumLength(10)
			.MaximumLength(50000);
	}
}
