// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ObjectIdExtensions.cs
// Company :       mpaulosky
// Author :        Teqslamer
// Solution Name : Articles
// Project Name :  Domain
// =============================================

namespace Domain.ValueObjects;

/// <summary>
///     Domain-layer helpers for <see cref="ObjectId" /> creation and parsing.
///     All deterministic IDs used in seed data or tests must originate from this class.
/// </summary>
public static class ObjectIdExtensions
{
	/// <summary>
	///     Tries to parse a 24-character hex string into an <see cref="ObjectId" />.
	///     Returns <see langword="false" /> when <paramref name="hex" /> is null, empty, or not a valid ObjectId.
	/// </summary>
	public static bool TryParseObjectId(string? hex, out ObjectId id)
	{
		if (string.IsNullOrWhiteSpace(hex))
		{
			id = ObjectId.Empty;
			return false;
		}

		return ObjectId.TryParse(hex, out id);
	}

	/// <summary>
	///     Parses a 24-character hex string into an <see cref="ObjectId" />.
	///     Throws <see cref="FormatException" /> when the string is not a valid ObjectId.
	/// </summary>
	public static ObjectId ParseObjectId(string hex) => ObjectId.Parse(hex);

	/// <summary>
	///     Returns a deterministic <see cref="ObjectId" /> for the given <paramref name="slot" />.
	///     Suitable for seed data and tests where a stable, repeatable ID is required.
	///     The slot is a 1-based integer; slot 1 produces <c>000000000000000000000001</c>.
	/// </summary>
	/// <param name="slot">Positive integer identifying the seed slot (1–9 999 999).</param>
	public static ObjectId DeterministicId(int slot)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(slot, 1);
		return new ObjectId(slot.ToString("X24", System.Globalization.CultureInfo.InvariantCulture));
	}
}