//-----------------------------------------------------------------------
// <copyright file="BboxToVector2dBoundsConverter.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Vector2d;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Mapbox.BaseModule.Utilities.JsonConverters
{
	/// <summary>
	/// Bbox to geo coordinate bounds converter.
	/// </summary>
	public class BboxToVector2dBoundsConverter : CustomCreationConverter<LatitudeLongitudeBounds>
	{
		/// <summary>
		/// Gets a value indicating whether this <see cref="T:Mapbox.BboxToVector2dBoundsConverter"/> can write.
		/// </summary>
		/// <value><c>true</c> if can write; otherwise, <c>false</c>.</value>
		public override bool CanWrite {
			get { return true; }
		}

		/// <summary>
		/// Create the specified objectType.
		/// </summary>
		/// <param name="objectType">Object type.</param>
		/// <returns>A <see cref="LatitudeLongitudeBounds"/>.</returns>
		public override LatitudeLongitudeBounds Create(Type objectType)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Create the specified objectType and jArray.
		/// </summary>
		/// <param name="objectType">Object type.</param>
		/// <param name="val">J array.</param>
		/// <returns>A <see cref="LatitudeLongitudeBounds"/>.</returns>
		public LatitudeLongitudeBounds Create(Type objectType, JArray val)
		{
			// GeoJSON bbox order is [min_lon, min_lat, max_lon, max_lat];
			// LatitudeLongitude's constructor takes (latitude, longitude), so the pairs are swapped.
			return new LatitudeLongitudeBounds(
				new LatitudeLongitude((double)val[1], (double)val[0]),
				new LatitudeLongitude((double)val[3], (double)val[2]));
		}

		/// <summary>
		/// Reads the json.
		/// </summary>
		/// <returns>The serialized object.</returns>
		/// <param name="reader">A reader.</param>
		/// <param name="objectType">Object type.</param>
		/// <param name="existingValue">Existing value.</param>
		/// <param name="serializer">A <see cref="JsonSerializer"/>.</param>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JArray bbox = JArray.Load(reader);

			return Create(objectType, bbox);
		}

		/// <summary>
		/// Writes the JSON as an array.
		/// </summary>
		/// <param name="writer">A <see cref="JsonWriter"/>.</param>
		/// <param name="value">The value to serialize.</param>
		/// <param name="serializer">A <see cref="JsonSerializer"/>.</param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var val = (LatitudeLongitudeBounds)value;

			// TODO: This is not working correctly, and setting "bbox: [0,0,0,0]" to Vector2d properties for some reason. 
			System.Diagnostics.Debug.WriteLine(val);
			serializer.Serialize(writer, val.ToArray());
		}
	}
}
