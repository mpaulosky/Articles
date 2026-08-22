// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryCommands.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Domain.Abstractions;

using Web.Components.Features.Categories.Models;

namespace Web.Components.Features.Categories.Commands;

internal sealed record CreateCategoryCommand(string Name, string Description) : ICommand<Result<CategoryDto>>;

internal sealed record UpdateCategoryCommand(string Id, string Name, string Description)
	: ICommand<Result<CategoryDto>>;

internal sealed record ArchiveCategoryCommand(string Id) : ICommand<Result<CategoryDto>>;

internal sealed record UnarchiveCategoryCommand(string Id) : ICommand<Result<CategoryDto>>;
