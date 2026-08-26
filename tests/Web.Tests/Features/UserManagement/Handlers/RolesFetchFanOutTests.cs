// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     RolesFetchFanOutTests.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Web.Tests
// =============================================

using FluentAssertions;

using Web.Components.Features.UserManagement.ManageRoles;

namespace Web.Tests.Features.UserManagement.Handlers;

public class RolesFetchFanOutTests
{
	[Fact]
	public async Task RunAsync_ReturnsResultsInInputOrder()
	{
		// Arrange
		var items = Enumerable.Range(0, 20).ToList();

		// Act
		var results = await RolesFetchFanOut.RunAsync(
		items,
		(item, _) => Task.FromResult(item * 10),
		concurrency: 3,
		CancellationToken.None);

		// Assert
		results.Should().Equal(items.Select(i => i * 10));
	}

	[Fact]
	public async Task RunAsync_RespectsConfiguredConcurrency()
	{
		// Arrange
		const int itemCount = 20;
		const int concurrency = 3;
		var tracker = new ConcurrencyTracker();

		// Act
		await RolesFetchFanOut.RunAsync(
		Enumerable.Range(0, itemCount).ToList(),
		async (_, ct) =>
		{
			tracker.Enter();
			try
			{
				await Task.Delay(TimeSpan.FromMilliseconds(30), ct);
				return 0;
			}
			finally
			{
				tracker.Exit();
			}
		},
		concurrency,
		CancellationToken.None);

		// Assert
		tracker.MaxObserved.Should().BeLessThanOrEqualTo(concurrency);
		tracker.MaxObserved.Should().BeGreaterThan(1, "the fan-out should run concurrently instead of staying fully sequential");
	}

	[Fact]
	public async Task RunAsync_WhenAWorkerThrows_PropagatesTheExceptionDirectly()
	{
		// Arrange
		var items = new[] { 1, 2, 3 };

		// Act
		var act = () => RolesFetchFanOut.RunAsync(
		items,
		(item, _) => item == 2
			? throw new InvalidOperationException("boom")
			: Task.FromResult(item),
		concurrency: 1,
		CancellationToken.None);

		// Assert
		// Parallel.ForEachAsync throws the worker's exception directly rather than wrapping it
		// in an AggregateException, so callers can catch specific exception types as usual.
		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
	}

	/// <summary>
	///     Tracks the peak number of concurrently in-flight calls, so tests can assert the
	///     fan-out actually stays within its configured bound.
	/// </summary>
	private sealed class ConcurrencyTracker
	{
		private int current;

		private int maxObserved;

		public int MaxObserved => maxObserved;

		public void Enter()
		{
			var value = Interlocked.Increment(ref current);
			InterlockedMax(ref maxObserved, value);
		}

		public void Exit() => Interlocked.Decrement(ref current);

		private static void InterlockedMax(ref int target, int candidate)
		{
			int initial;
			do
			{
				initial = target;
				if (candidate <= initial)
				{
					return;
				}
			} while (Interlocked.CompareExchange(ref target, candidate, initial) != initial);
		}
	}
}
