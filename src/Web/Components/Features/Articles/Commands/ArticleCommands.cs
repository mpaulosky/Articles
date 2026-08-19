// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleCommands.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Domain.Abstractions;

using Web.Components.Features.Articles.Models;
using Web.Components.Features.AuthInfo.Entities;
using Web.Components.Features.Categories.Models;

namespace Web.Components.Features.Articles.Commands;

internal sealed record CreateArticleCommand(
	string Title,
	string Content,
	AuthorDto Author,
	CategoryDto? Category = null) : IRequest<Result<ArticleDto>>;

internal sealed record UpdateArticleCommand(
	string Id,
	string Title,
	string Content,
	CategoryDto? Category = null,
	bool ClearCategory = false) : IRequest<Result<ArticleDto>>;

internal sealed record DeleteArticleCommand(string Id) : IRequest<Result>;
