// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ObjectIdJsonConverter.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  Web
// =============================================

using System.Text.Json;
using System.Text.Json.Serialization;

using MongoDB.Bson;

namespace Web.Components.Features.UserManagement.Caching.Converters;

/// <summary>
/// Serializes <see cref="ObjectId"/> as its 24-character lowercase hex string so that
/// values survive a Redis round-trip without collapsing to <see cref="ObjectId.Empty"/>.
/// </summary>
internal sealed class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
	public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var value = reader.GetString();
		return value is not null && ObjectId.TryParse(value, out var id) ? id : ObjectId.Empty;
	}

	public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options) =>
		writer.WriteStringValue(value.ToString());
}
