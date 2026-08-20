// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CategoryRepository.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web
// =============================================

using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;

using Web.Components.Features.Categories.Entities;

namespace Web.Data;

/// <summary>
///     Provides repository operations for category persistence.
/// </summary>
public sealed class CategoryRepository
{
	private readonly ArticlesMongoDbContext _context;

	/// <summary>
	///     Initializes a new instance of the <see cref="CategoryRepository" /> class.
	/// </summary>
	/// <param name="context">The MongoDB data context.</param>
	public CategoryRepository(ArticlesMongoDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	/// <summary>
	///     Adds a new category to the repository.
	/// </summary>
	/// <param name="category">The category to create.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The created category.</returns>
	public async Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(category);

		await _context.Categories.AddAsync(category, cancellationToken).ConfigureAwait(false);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		return category;
	}

	/// <summary>
	///     Gets all categories ordered by name.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The categories in the repository.</returns>
	public async Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		return await _context.Categories
			.AsNoTracking()
			.OrderBy(category => category.Name)
			.ToListAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	///     Gets a specific category by identifier.
	/// </summary>
	/// <param name="id">The category identifier.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The category if it exists; otherwise, null.</returns>
	public async Task<Category?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
	{
		return await _context.Categories
			.AsNoTracking()
			.FirstOrDefaultAsync(category => category.Id == id, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	///     Updates an existing category.
	/// </summary>
	/// <param name="category">The category to update.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The updated category.</returns>
	public async Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(category);

		var trackedEntity = _context.Categories.Local.FirstOrDefault(c => c.Id == category.Id);
		if (trackedEntity != null && !ReferenceEquals(trackedEntity, category))
		{
			_context.Entry(trackedEntity).State = EntityState.Detached;
		}

		_context.Categories.Update(category);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		return category;
	}

	/// <summary>
	///     Deletes an existing category by identifier.
	/// </summary>
	/// <param name="id">The category identifier.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>True when the category was deleted; otherwise, false.</returns>
	public async Task<bool> DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
	{
		var category = await _context.Categories
			.FirstOrDefaultAsync(existing => existing.Id == id, cancellationToken)
			.ConfigureAwait(false);

		if (category is null)
		{
			return false;
		}

		_context.Categories.Remove(category);
		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		return true;
	}
}
