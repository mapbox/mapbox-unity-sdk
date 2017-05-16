//-----------------------------------------------------------------------
// <copyright file="PolylineToVector2dListConverter.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils.JsonConverters
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Json;
	using Mapbox.Json.Converters;
	using Mapbox.Json.Linq;

	/// <summary>
	/// Bbox to geo coordinate bounds converter.
	/// </summary>
	public class PolylineToVector2dListConverter : CustomCreationConverter<List<Vector2d>>
	{
		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Mapbox.PolylineToVector2dListConverter"/> can write.
		/// </summary>
		/// <value><c>true</c> if can write; otherwise, <c>false</c>.</value>
		public override bool CanWrite {
			get { return true; }
		}

		/// <summary>
		/// Create the specified objectType.
		/// </summary>
		/// <param name="objectType">Object type.</param>
		/// <returns>A List of <see cref="Vector2d"/>.</returns>
		public override List<Vector2d> Create(Type objectType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create the specified objectType and jArray.
		/// </summary>
		/// <param name="objectType">Object type.</param>
		/// <param name="polyLine">String representation of a polyLine.</param>
		/// <returns>A List of <see cref="Vector2d"/>.</returns>
		public List<Vector2d> Create(Type objectType, string polyLine)
		{
			return PolylineUtils.Decode(polyLine);
		}

		/// <summary>
		/// Writes the JSON as an encoded polyline.
		/// </summary>
		/// <param name="writer">A <see cref="JsonWriter"/>.</param>
		/// <param name="value">The original value.</param>
		/// <param name="serializer">A <see cref="JsonSerializer"/>.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var val = (List<Vector2d>)value;

			serializer.Serialize(writer, PolylineUtils.Encode(val));
		}

		/// <summary>
		/// Reads the json. Must be a linestring.
		/// </summary>
		/// <returns>The serialized object.</returns>
		/// <param name="reader">A Reader.</param>
		/// <param name="objectType">Object type.</param>
		/// <param name="existingValue">Existing value.</param>
		/// <param name="serializer">A <see cref="JsonSerializer"/>.</param>
		/// <returns>An object.</returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JToken polyLine = JToken.Load(reader);

			return Create(objectType, (string)polyLine);
		}
	}
}
