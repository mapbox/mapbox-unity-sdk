//-----------------------------------------------------------------------
// <copyright file="PolylineUtils.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Utils
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>
	/// A set of Polyline utils.
	/// </summary>
	public static class PolylineUtils
	{
		/// <summary>Decodes an encoded path string into a sequence of Positions.</summary>
		/// <remarks>
		/// Adapted from <see href="https://github.com/mapbox/mapbox-java/blob/9bda93a2f84e26ad67434de1a5c73c335ecac12c/libjava/lib/src/main/java/com/mapbox/services/commons/utils/PolylineUtils.java"/>
		/// </remarks>
		/// <param name="encodedPath">A string representing a path.</param>
		/// <param name="precision">Level of precision. OSRMv4 uses 6, OSRMv5 and Google use 5.</param>
		/// <returns>List of <see cref="Vector2d"/> making up the line.</returns>
		public static List<Vector2d> Decode(string encodedPath, int precision = 5)
		{
			int len = encodedPath.Length;

			double factor = Math.Pow(10, precision);

			// For speed we preallocate to an upper bound on the final length, then
			// truncate the array before returning.
			var path = new List<Vector2d>();
			int index = 0;
			int lat = 0;
			int lng = 0;

			while (index < len)
			{
				int result = 1;
				int shift = 0;
				int b;
				do
				{
					b = encodedPath[index++] - 63 - 1;
					result += b << shift;
					shift += 5;
				}
				while (b >= 0x1f);
				lat += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

				result = 1;
				shift = 0;
				do
				{
					b = encodedPath[index++] - 63 - 1;
					result += b << shift;
					shift += 5;
				}
				while (b >= 0x1f);
				lng += (result & 1) != 0 ? ~(result >> 1) : (result >> 1);

				path.Add(new Vector2d(y: lng / factor, x: lat / factor));
			}

			return path;
		}

		/// <summary>
		/// Encodes a sequence of Positions into an encoded path string.
		/// </summary>
		/// <remarks>
		/// Adapted from <see href="https://github.com/mapbox/mapbox-java/blob/9bda93a2f84e26ad67434de1a5c73c335ecac12c/libjava/lib/src/main/java/com/mapbox/services/commons/utils/PolylineUtils.java"/>
		/// </remarks>
		/// <param name="path">List of <see cref="Vector2d"/> making up the line.</param>
		/// <param name="precision">Level of precision. OSRMv4 uses 6, OSRMv5 and Google use 5..</param>
		/// <returns>A string representing a polyLine.</returns>
		public static string Encode(List<Vector2d> path, int precision = 5)
		{
			long lastLat = 0;
			long lastLng = 0;

			var result = new StringBuilder();

			double factor = Math.Pow(10, precision);

			foreach (Vector2d point in path)
			{
				var lat = (long)Math.Round(point.x * factor);
				var lng = (long)Math.Round(point.y * factor);

				Encode(lat - lastLat, result);
				Encode(lng - lastLng, result);

				lastLat = lat;
				lastLng = lng;
			}

			return result.ToString();
		}

		/// <summary>
		/// Encode the latitude or longitude.
		/// </summary>
		/// <param name="variable">The value to encode.</param>
		/// <param name="result">String representation of latitude or longitude.</param>
		private static void Encode(long variable, StringBuilder result)
		{
			variable = variable < 0 ? ~(variable << 1) : variable << 1;
			while (variable >= 0x20)
			{
				result.Append((char)((int)((0x20 | (variable & 0x1f)) + 63)));
				variable >>= 5;
			}

			result.Append((char)((int)(variable + 63)));
		}
	}
}
