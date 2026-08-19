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

internal sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;

internal sealed record GetCategoryByIdQuery(string Id) : IRequest<Result<CategoryDto>>;
