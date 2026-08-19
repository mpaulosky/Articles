// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleValidators.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

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
			.MinimumLength(3)
			.MaximumLength(200);

		RuleFor(command => command.Content)
			.NotEmpty()
			.MinimumLength(10);

		RuleFor(command => command.Author)
			.NotNull();
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
			.MinimumLength(10);
	}
}
