//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RolesFetchFanOut.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace Web.Components.Features.UserManagement.ManageRoles;

internal static class RolesFetchFanOut
{
	/// <summary>
	///     Runs <paramref name="worker" /> once per item with a bounded degree of parallelism, preserving
	///     input order in the returned results.
	/// </summary>
	/// <param name="items">The items to process.</param>
	/// <param name="worker">Invoked once per item; receives the item and a linked cancellation token.</param>
	/// <param name="concurrency">The maximum number of items processed at the same time.</param>
	/// <param name="cancellationToken">Cancels the whole operation.</param>
	/// <returns>The worker's results, in the same order as <paramref name="items" />.</returns>
	public static async Task<IReadOnlyList<TOut>> RunAsync<TIn, TOut>(
	IReadOnlyList<TIn> items,
	Func<TIn, CancellationToken, Task<TOut>> worker,
	int concurrency,
	CancellationToken cancellationToken)
	{
		var results = new TOut[items.Count];
		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = concurrency,
			CancellationToken = cancellationToken
		};

		await Parallel.ForEachAsync(Enumerable.Range(0, items.Count), parallelOptions, async (index, ct) =>
		{
			results[index] = await worker(items[index], ct).ConfigureAwait(false);
		}).ConfigureAwait(false);

		return results;
	}
}
