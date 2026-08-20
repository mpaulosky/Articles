// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleFeatureHandler.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Domain.Abstractions;

using FluentValidation;

using MongoDB.Bson;

using Web.Components.Features.Articles.Commands;
using Web.Components.Features.Articles.Entities;
using Web.Components.Features.Articles.Models;
using Web.Components.Features.Articles.Queries;
using Web.Data;

namespace Web.Components.Features.Articles.Handlers;

/// <summary>
///     Handles the article CQRS feature contract for create, read, update, and delete operations.
/// </summary>
internal sealed class ArticleFeatureHandler(
	ArticleRepository repository,
	IValidator<CreateArticleCommand>? createValidator = null,
	IValidator<UpdateArticleCommand>? updateValidator = null)
	: IRequestHandler<CreateArticleCommand, Result<ArticleDto>>,
		IRequestHandler<GetArticlesQuery, Result<IReadOnlyList<ArticleDto>>>,
		IRequestHandler<GetArticleByIdQuery, Result<ArticleDto>>,
		IRequestHandler<UpdateArticleCommand, Result<ArticleDto>>,
		IRequestHandler<DeleteArticleCommand, Result>,
		IRequestHandler<PublishArticleCommand, Result<ArticleDto>>,
		IRequestHandler<UnpublishArticleCommand, Result<ArticleDto>>
{
	/// <inheritdoc />
	public async Task<Result<ArticleDto>> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
	{
		var validationResult = createValidator is null
			? null
			: await createValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
		if (validationResult is { IsValid: false })
		{
			var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Article data is invalid.";
			return Result.Fail<ArticleDto>(message, ResultErrorCode.Validation);
		}

		try
		{
			var article = Article.Create(request.Title, request.Content, request.Author, request.Slug);
			if (request.Category is not null)
			{
				article.AssignCategory(request.Category);
			}

			var created = await repository.AddAsync(article, cancellationToken).ConfigureAwait(false);
			return Result.Ok(ArticleDto.FromEntity(created));
		}
		catch (ArgumentException ex)
		{
			return Result.Fail<ArticleDto>(ex.Message, ResultErrorCode.Validation);
		}
	}

	/// <inheritdoc />
	public async Task<Result<IReadOnlyList<ArticleDto>>> Handle(GetArticlesQuery request,
		CancellationToken cancellationToken)
	{
		var articles = await repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
		var dtoList = articles.Select(ArticleDto.FromEntity).ToList();
		return Result.Ok<IReadOnlyList<ArticleDto>>(dtoList.AsReadOnly());
	}

	/// <inheritdoc />
	public async Task<Result<ArticleDto>> Handle(GetArticleByIdQuery request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<ArticleDto>("The article id is not valid.", ResultErrorCode.Validation);
		}

		var article = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (article is null)
		{
			return Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound);
		}

		return Result.Ok(ArticleDto.FromEntity(article));
	}

	/// <inheritdoc />
	public async Task<Result<ArticleDto>> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
	{
		var validationResult = updateValidator is null
			? null
			: await updateValidator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
		if (validationResult is { IsValid: false })
		{
			var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Article data is invalid.";
			return Result.Fail<ArticleDto>(message, ResultErrorCode.Validation);
		}

		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<ArticleDto>("The article id is not valid.", ResultErrorCode.Validation);
		}

		var article = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (article is null)
		{
			return Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound);
		}

		try
		{
			article.Update(request.Title, request.Content, request.Category, request.ClearCategory, request.Slug);
			var updated = await repository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
			return Result.Ok(ArticleDto.FromEntity(updated));
		}
		catch (ArgumentException ex)
		{
			return Result.Fail<ArticleDto>(ex.Message, ResultErrorCode.Validation);
		}
	}

	/// <inheritdoc />
	public async Task<Result> Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail("The article id is not valid.", ResultErrorCode.Validation);
		}

		var deleted = await repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
		if (!deleted)
		{
			return Result.Fail("Article not found.", ResultErrorCode.NotFound);
		}

		return Result.Ok();
	}

	/// <inheritdoc />
	public async Task<Result<ArticleDto>> Handle(PublishArticleCommand request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<ArticleDto>("The article id is not valid.", ResultErrorCode.Validation);
		}

		var article = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (article is null)
		{
			return Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound);
		}

		article.Publish();
		var updated = await repository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
		return Result.Ok(ArticleDto.FromEntity(updated));
	}

	/// <inheritdoc />
	public async Task<Result<ArticleDto>> Handle(UnpublishArticleCommand request, CancellationToken cancellationToken)
	{
		if (!ObjectId.TryParse(request.Id, out var id))
		{
			return Result.Fail<ArticleDto>("The article id is not valid.", ResultErrorCode.Validation);
		}

		var article = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
		if (article is null)
		{
			return Result.Fail<ArticleDto>("Article not found.", ResultErrorCode.NotFound);
		}

		article.Unpublish();
		var updated = await repository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
		return Result.Ok(ArticleDto.FromEntity(updated));
	}
}
