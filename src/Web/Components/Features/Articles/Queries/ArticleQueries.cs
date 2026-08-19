// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleQueries.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Domain.Abstractions;

using Web.Components.Features.Articles.Models;

namespace Web.Components.Features.Articles.Queries;

internal sealed record GetArticlesQuery : IRequest<Result<IReadOnlyList<ArticleDto>>>;

internal sealed record GetArticleByIdQuery(string Id) : IRequest<Result<ArticleDto>>;
