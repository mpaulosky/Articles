// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryQueries.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Domain.Abstractions;

using Web.Components.Features.Categories.Models;

namespace Web.Components.Features.Categories.Queries;

public sealed record GetCategoriesQuery : IQuery<Result<IReadOnlyList<CategoryDto>>>;

public sealed record GetCategoryByIdQuery(string Id) : IQuery<Result<CategoryDto>>;
