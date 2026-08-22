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

public sealed record GetArticlesQuery : IQuery<Result<IReadOnlyList<ArticleDto>>>;

public sealed record GetArticleByIdQuery(string Id) : IQuery<Result<ArticleDto>>;

public sealed record GetArticleBySlugQuery(string Slug) : IQuery<Result<ArticleDto>>;
