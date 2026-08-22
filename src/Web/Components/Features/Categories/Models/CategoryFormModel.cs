// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryFormModel.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

namespace Web.Components.Features.Categories.Models;

/// <summary>
///     Represents the editable fields shared by the create and edit category forms.
/// </summary>
public sealed class CategoryFormModel
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;

	public void Reset()
	{
		Name = string.Empty;
		Description = string.Empty;
	}
}
