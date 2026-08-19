// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryValidators.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using FluentValidation;

using Web.Components.Features.Categories.Commands;

namespace Web.Components.Features.Categories.Validators;

/// <summary>
///     Validates category creation commands.
/// </summary>
internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
	/// <summary>
	///     Initializes validation rules for category creation.
	/// </summary>
	public CreateCategoryCommandValidator()
	{
		RuleFor(command => command.Name)
			.NotEmpty()
			.MinimumLength(2)
			.MaximumLength(100);

		RuleFor(command => command.Description)
			.NotEmpty()
			.MinimumLength(5)
			.MaximumLength(500);
	}
}

/// <summary>
///     Validates category update commands.
/// </summary>
internal sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
	/// <summary>
	///     Initializes validation rules for category updates.
	/// </summary>
	public UpdateCategoryCommandValidator()
	{
		RuleFor(command => command.Id)
			.NotEmpty();

		RuleFor(command => command.Name)
			.NotEmpty()
			.MinimumLength(2)
			.MaximumLength(100);

		RuleFor(command => command.Description)
			.NotEmpty()
			.MinimumLength(5)
			.MaximumLength(500);
	}
}
