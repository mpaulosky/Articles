// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryFeatureHandler.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Domain.Abstractions;

using FluentValidation;

using MongoDB.Bson;

using Web.Components.Features.Categories.Commands;
using Web.Components.Features.Categories.Entities;
using Web.Components.Features.Categories.Models;
using Web.Components.Features.Categories.Queries;
using Web.Data;

namespace Web.Components.Features.Categories.Handlers;

/// <summary>
///     Handles the category CQRS feature contract for create, read, update, and delete operations.
/// </summary>
internal sealed class CategoryFeatureHandler(
	CategoryRepository repository,
	IValidator<CreateCategoryCommand>? createValidator = null,
	IValidator<UpdateCategoryCommand>? updateValidator = null)
	: IRequestHandler<CreateCategoryCommand, Result<CategoryDto>>,
		IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>,
		IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto>>,
		IRequestHandler<UpdateCategoryCommand, Result<CategoryDto>>,
		IRequestHandler<ArchiveCategoryCommand, Result<CategoryDto>>,
		IRequestHandler<UnarchiveCategoryCommand, Result<CategoryDto>>
{
	/// <inheritdoc />
	public async Task<Result<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
	{
		var validationResult = createValidator is null
			? null
			: await createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
		if (validationResult is { IsValid: false })
		{
			var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Category data is invalid.";
			return Result.Fail<CategoryDto>(message, ResultErrorCode.Validation);
		}

		try
		{
			var category = Category.Create(request.Name, request.Description);
			var created = await repository.AddAsync(category, cancellationToken).ConfigureAwait(false);
			return Result.Ok(CategoryDto.FromEntity(created));
		}
		catch (ArgumentException ex)
		{
			return Result.Fail<CategoryDto>(ex.Message, ResultErrorCode.Validation);
		}
	}

	/// <inheritdoc />
	public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(GetCategoriesQuery request,
		CancellationToken cancellationToken)
	{
		var categories = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
		var dtoList = categories.Select(CategoryDto.FromEntity).ToList();
		return Result.Ok<IReadOnlyList<CategoryDto>>(dtoList.AsReadOnly());
	}

	/// <inheritdoc />
	public async Task<Result<CategoryDto>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<CategoryDto>("The category id is not valid.", ResultErrorCode.Validation);
		}

		var category = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (category is null)
		{
			return Result.Fail<CategoryDto>("Category not found.", ResultErrorCode.NotFound);
		}

		return Result.Ok(CategoryDto.FromEntity(category));
	}

	/// <inheritdoc />
	public async Task<Result<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
	{
		var validationResult = updateValidator is null
			? null
			: await updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
		if (validationResult is { IsValid: false })
		{
			var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Category data is invalid.";
			return Result.Fail<CategoryDto>(message, ResultErrorCode.Validation);
		}

		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<CategoryDto>("The category id is not valid.", ResultErrorCode.Validation);
		}

		var category = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (category is null)
		{
			return Result.Fail<CategoryDto>("Category not found.", ResultErrorCode.NotFound);
		}

		try
		{
			category.Update(request.Name, request.Description);
			var updated = await repository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
			return Result.Ok(CategoryDto.FromEntity(updated));
		}
		catch (ArgumentException ex)
		{
			return Result.Fail<CategoryDto>(ex.Message, ResultErrorCode.Validation);
		}
	}

	/// <inheritdoc />
	public async Task<Result<CategoryDto>> Handle(ArchiveCategoryCommand request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<CategoryDto>("The category id is not valid.", ResultErrorCode.Validation);
		}

		var category = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (category is null)
		{
			return Result.Fail<CategoryDto>("Category not found.", ResultErrorCode.NotFound);
		}

		category.Archive();
		var updated = await repository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
		return Result.Ok(CategoryDto.FromEntity(updated));
	}

	/// <inheritdoc />
	public async Task<Result<CategoryDto>> Handle(UnarchiveCategoryCommand request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<CategoryDto>("The category id is not valid.", ResultErrorCode.Validation);
		}

		var category = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (category is null)
		{
			return Result.Fail<CategoryDto>("Category not found.", ResultErrorCode.NotFound);
		}

		category.Unarchive();
		var updated = await repository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
		return Result.Ok(CategoryDto.FromEntity(updated));
	}
}
