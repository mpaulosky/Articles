// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ArticleFormModel.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

namespace Web.Components.Features.Articles.Models;

/// <summary>
///     Represents the editable fields shared by the create and edit article forms.
/// </summary>
public sealed class ArticleFormModel
{
	public string Title { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public string CategoryId { get; set; } = string.Empty;

	public void Reset()
	{
		Title = string.Empty;
		Content = string.Empty;
		CategoryId = string.Empty;
	}
}
