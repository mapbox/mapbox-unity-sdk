//-----------------------------------------------------------------------
// <copyright file="JsonConverters.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils.JsonConverters
{
	using Mapbox.Json;

	/// <summary>
	/// Custom json converters.
	/// </summary>
	public static class JsonConverters
	{
		/// <summary>
		/// Array of converters.
		/// </summary>
		private static JsonConverter[] converters =
		{
			new LonLatToVector2dConverter(),
			new BboxToVector2dBoundsConverter(),
			new PolylineToVector2dListConverter()
		};

		/// <summary>
		/// Gets the converters.
		/// </summary>
		/// <value>The converters.</value>
		public static JsonConverter[] Converters {
			get {
				return converters;
			}
		}
	}
}
