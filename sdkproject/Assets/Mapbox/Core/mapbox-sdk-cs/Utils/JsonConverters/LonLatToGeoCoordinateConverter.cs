//-----------------------------------------------------------------------
// <copyright file="LonLatToVector2dConverter.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils.JsonConverters
{
	using System;
	using Mapbox.Json;
	using Mapbox.Json.Converters;
	using Mapbox.Json.Linq;

	/// <summary>
	/// Bbox to geo coordinate bounds converter.
	/// </summary>
	public class LonLatToVector2dConverter : CustomCreationConverter<Vector2d>
	{
		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Mapbox.LonLatToVector2dConverter"/> can write.
		/// </summary>
		/// <value><c>true</c> if can write; otherwise, <c>false</c>.</value>
		public override bool CanWrite {
			get { return true; }
		}

		/// <summary>
		/// Create the specified objectType.
		/// </summary>
		/// <param name="objectType">Object type.</param>
		/// <returns>A <see cref="Vector2d"/>.</returns>
		public override Vector2d Create(Type objectType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create the specified objectType and jArray.
		/// </summary>
		/// <param name="objectType">Object type.</param>
		/// <param name="val">Jarray representing a two length array of coordinates.</param>
		/// <returns>A <see cref="Vector2d"/>.</returns>
		public Vector2d Create(Type objectType, JArray val)
		{
			// Assumes long,lat order (like in geojson)
			return new Vector2d(y: (double)val[0], x: (double)val[1]);
		}

		/// <summary>
		/// Writes the json.
		/// </summary>
		/// <param name="writer">A <see cref="JsonWriter"/>.</param>
		/// <param name="value">The value to serialize.</param>
		/// <param name="serializer">A <see cref="JsonSerializer"/>.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var val = (Vector2d)value;

			Array valAsArray = val.ToArray();

			// By default, Vector2d outputs an array with [lat, lon] order, but we want the reverse.
			Array.Reverse(valAsArray);

			serializer.Serialize(writer, valAsArray);
		}

		/// <summary>
		/// Reads the json.
		/// </summary>
		/// <returns>The serialized object.</returns>
		/// <param name="reader">A reader.</param>
		/// <param name="objectType">Object type.</param>
		/// <param name="existingValue">Existing value.</param>
		/// <param name="serializer">A <see cref="JsonSerializer"/>.</param>
		/// <returns>An object.</returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JArray coordinates = JArray.Load(reader);

			return Create(objectType, coordinates);
		}
	}
}
