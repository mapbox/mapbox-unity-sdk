//-----------------------------------------------------------------------
// <copyright file="Conversions.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
	/// <summary>
	/// A set of Geo and Terrain Conversion utils.
	/// </summary>
	public static class Conversions
	{
		private const int TileSize = 256;
		/// <summary>according to https://wiki.openstreetmap.org/wiki/Zoom_levels</summary>
		private const int EarthRadius = 6378137; //no seams with globe example
		private const double InitialResolution = 2 * Math.PI * EarthRadius / TileSize;
		private const double OriginShift = 2 * Math.PI * EarthRadius / 2;

		private static readonly int[] PowerTable2 =
		{
			1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576, 2097152, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456, 536870912, 1073741824
		};

		
		/// <summary>
		/// Converts <see cref="T:Mapbox.Utils.Vector2d"/> struct, WGS84
		/// lat/lon to Spherical Mercator EPSG:900913 xy meters.
		/// </summary>
		/// <param name="v"> The <see cref="T:Mapbox.Utils.Vector2d"/>. </param>
		/// <returns> A <see cref="T:UnityEngine.Vector2d"/> of coordinates in meters. </returns>
		public static Vector2d LatitudeLongitudeToWebMercator(LatitudeLongitude v)
		{
			return LatitudeLongitudeToWebMercator(v.Latitude, v.Longitude);
		}

		/// <summary>
		/// Converts WGS84 lat/lon to Spherical Mercator EPSG:900913 xy meters.
		/// SOURCE: http://stackoverflow.com/questions/12896139/geographic-coordinates-converter.
		/// </summary>
		/// <param name="lat"> The latitude. </param>
		/// <param name="lon"> The longitude. </param>
		/// <returns> A <see cref="T:UnityEngine.Vector2d"/> of xy meters. </returns>
		public static Vector2d LatitudeLongitudeToWebMercator(double lat, double lon)
		{
			var posx = lon * OriginShift / 180;
			var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
			posy = posy * OriginShift / 180;
			return new Vector2d(posx, posy);
		}

		public static UnwrappedTileId LatitudeLongitudeToTileId(LatitudeLongitude latlng, int zoom)
		{
			return LatitudeLongitudeToTileId(latlng.Latitude, latlng.Longitude, zoom);
		}

		/// <summary>
		/// Gets the xy tile ID at the requested zoom that contains the WGS84 lat/lon point.
		/// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames.
		/// </summary>
		/// <param name="latitude"> The latitude. </param>
		/// <param name="longitude"> The longitude. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> A <see cref="T:Mapbox.Map.UnwrappedTileId"/> xy tile ID. </returns>
		public static UnwrappedTileId LatitudeLongitudeToTileId(double latitude, double longitude, int zoom)
		{
			var x = (int)Math.Floor((longitude + 180.0) / 360.0 * Math.Pow(2.0, zoom));
			var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latitude * Math.PI / 180.0)
			                                        + 1.0 / Math.Cos(latitude * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));

			return new UnwrappedTileId(zoom, x, y);
		}

		public static Vector2 LatitudeLongitudeToInTile01(LatitudeLongitude latlng, CanonicalTileId tileId)
		{
			var rect = TileIdToBounds(tileId.X, tileId.Y, tileId.Z);
			var dx = (latlng.Longitude - rect.West) / (rect.East - rect.West);
			var dy = (latlng.Latitude - rect.South) / (rect.North - rect.South);
			return new Vector2((float) dx, (float) dy);
		}
		
		public static LatitudeLongitude Tile01ToLatitudeLongitude(Vector2 zeroOne, CanonicalTileId tileId)
		{
			var rect = TileIdToBounds(tileId.X, tileId.Y, tileId.Z);
			return new LatitudeLongitude(
				latitude: Mathd.Lerp(rect.South, rect.North, zeroOne.y),
				longitude:Mathd.Lerp(rect.East, rect.West, zeroOne.x)
			);
		}
		
		public static LatitudeLongitude Tile01ToLatitudeLongitude(Vector3 zeroOne, CanonicalTileId tileId)
		{
			var rect = TileIdToBounds(tileId.X, tileId.Y, tileId.Z);
			return new LatitudeLongitude(
				latitude: Mathd.Lerp(rect.South, rect.North, 1 + zeroOne.z),
				longitude:Mathd.Lerp(rect.West, rect.East, zeroOne.x)
			);
		}

		/// <summary>
		/// Converts WGS84 lat/lon to x/y meters in reference to a center point
		/// </summary>
		/// <param name="lat"> The latitude. </param>
		/// <param name="lon"> The longitude. </param>
		/// <param name="refPoint"> A <see cref="T:UnityEngine.Vector2d"/> center point to offset resultant xy, this is usually map's center mercator</param>
		/// <param name="scale"> Scale in meters. (default scale = 1) </param>
		/// <returns> A <see cref="T:UnityEngine.Vector2d"/> xy tile ID. </returns>
		/// <example>
		/// Converts a Lat/Lon of (37.7749, 122.4194) into Unity coordinates for a map centered at (10,10) and a scale of 2.5 meters for every 1 Unity unit
		/// <code>
		/// var worldPosition = Conversions.GeoToWorldPosition(37.7749, 122.4194, new Vector2d(10, 10), (float)2.5);
		/// // worldPosition = ( 11369163.38585, 34069138.17805 )
		/// </code>
		/// </example>
		public static Vector3 LatitudeLongitudeToWorldPosition(double lat, double lon, Vector2d refPoint, float scale = 1)
		{
			var posx = lon * OriginShift / 180;
			var posy = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180);
			posy = posy * OriginShift / 180;
			return new Vector3((float)(posx - refPoint.x) * scale, 0, (float)(posy - refPoint.y) * scale);
		}

		public static Vector3 LatitudeLongitudeToWorldPosition(LatitudeLongitude latLong, Vector2d refPoint, float scale = 1)
		{
			return LatitudeLongitudeToWorldPosition(latLong.Latitude, latLong.Longitude, refPoint, scale);
		}


        /// <summary>
        /// Convert a simple string to a latitude longitude.
        /// Expects format: latitude, longitude
        /// </summary>
        /// <returns>The lat/lon as Vector2d.</returns>
        /// <param name="s">string.</param>
        public static LatitudeLongitude StringToLatLon(string s)
		{
			var latLonSplit = s.Split(',');
			if (latLonSplit.Length != 2)
			{
				throw new ArgumentException("Wrong number of arguments");
			}

			double latitude = 0;
			double longitude = 0;

			if (!double.TryParse(latLonSplit[0], NumberStyles.Any, NumberFormatInfo.InvariantInfo, out latitude))
			{
				throw new Exception(string.Format("Could not convert latitude to double: {0}", latLonSplit[0]));
			}

			if (!double.TryParse(latLonSplit[1], NumberStyles.Any, NumberFormatInfo.InvariantInfo, out longitude))
			{
				throw new Exception(string.Format("Could not convert longitude to double: {0}", latLonSplit[0]));
			}

			return new LatitudeLongitude(latitude, longitude);
		}

		/// <summary>
		/// Converts Spherical Mercator EPSG:900913 in xy meters to WGS84 lat/lon.
		/// Inverse of LatLonToMeters.
		/// </summary>
		/// <param name="m"> A <see cref="T:UnityEngine.Vector2d"/> of coordinates in meters.  </param>
		/// <returns> The <see cref="T:Mapbox.Utils.Vector2d"/> in lat/lon. </returns>

		/// <example>
		/// Converts EPSG:900913 xy meter coordinates to lat lon
		/// <code>
		/// var worldPosition =  new Vector2d (4547675.35434,13627665.27122);
		/// var latlon = Conversions.MetersToLatLon(worldPosition);
		/// // latlon = ( 37.77490, 122.41940 )
		/// </code>
		/// </example>
		public static LatitudeLongitude WebMercatorToLatLon(Vector2d m)
		{
			var vx = (m.x / OriginShift) * 180;
			var vy = (m.y / OriginShift) * 180;
			vy = 180 / Math.PI * (2 * Math.Atan(Math.Exp(vy * Math.PI / 180)) - Math.PI / 2);
			return new LatitudeLongitude(vy, vx);
		}

		
		
		public static RectD TileBoundsInWebMercator(UnwrappedTileId unwrappedTileId)
		{
			var min = PixelsToMeters(
				unwrappedTileId.X * TileSize,
				unwrappedTileId.Y * TileSize,
				unwrappedTileId.Z);
			var max = PixelsToMeters(
				(unwrappedTileId.X + 1) * TileSize,
				(unwrappedTileId.Y + 1) * TileSize,
				unwrappedTileId.Z);
			return new RectD(min, max - min);
		}
		
		public static RectD TileBoundsInWebMercator(CanonicalTileId canonicalTileId)
		{
			var min = PixelsToMeters(
				canonicalTileId.X * TileSize,
				canonicalTileId.Y * TileSize,
				canonicalTileId.Z);
			var max = PixelsToMeters(
				(canonicalTileId.X + 1) * TileSize,
				(canonicalTileId.Y + 1) * TileSize,
				canonicalTileId.Z);
			return new RectD(min, max - min);
		}

		public static Vector3 TileTopLeftInUnitySpace(CanonicalTileId unwrappedTileId, Vector2d worldCenter, float scale)
		{
			var res = InitialResolution / PowerTable2[unwrappedTileId.Z];
			var xMeters = (unwrappedTileId.X * TileSize) * res - OriginShift;
			xMeters = WrapWorldX(xMeters, worldCenter.x);
			var minX = (float)((xMeters - worldCenter.x) / scale);
			var minY = (float)((-((unwrappedTileId.Y * TileSize * res) - OriginShift)) - worldCenter.y) / scale;
			return new Vector3(minX, 0, minY);
		}

		public static float TileSizeInUnitySpace(int tileZ, float scale)
		{
			return (40075017f / PowerTable2[tileZ])/ scale;
		}
		
		public static RectD TileBoundsInUnitySpace(CanonicalTileId unwrappedTileId, Vector2d worldCenter, float scale)
		{
			var min = PixelsToMeters(
				unwrappedTileId.X * TileSize,
				unwrappedTileId.Y * TileSize,
				unwrappedTileId.Z);
			var max = PixelsToMeters(
				(unwrappedTileId.X + 1) * TileSize,
				(unwrappedTileId.Y + 1) * TileSize,
				unwrappedTileId.Z);
			var centerX = (min.x + max.x) * 0.5;
			var shift = Math.Round((centerX - worldCenter.x) / (OriginShift * 2.0)) * (OriginShift * 2.0);
			if (shift != 0.0)
			{
				min.x -= shift;
				max.x -= shift;
			}
			return new RectD((min - worldCenter)/scale, (max - min)/scale);
		}
		
		[Obsolete("This overload is obsolete, please use TileEdgeSizeInMercator instead")]
		public static float CalculateTileEdgeSizeInMercator(CanonicalTileId unwrappedTileId)
		{
			var res = InitialResolution / PowerTable2[unwrappedTileId.Z];
			var first = unwrappedTileId.X * TileSize * res - OriginShift;
			var second = (unwrappedTileId.X + 1) * TileSize * res - OriginShift;
			return (float)(second - first);
		}

		public static float TileEdgeSizeInMercator(CanonicalTileId unwrappedTileId)
		{
			return (40075017f / PowerTable2[unwrappedTileId.Z]);
		}

		private static double WrapWorldX(double x, double worldCenterX)
		{
			var worldWidth = OriginShift * 2.0;
			var shift = Math.Round((x - worldCenterX) / worldWidth) * worldWidth;
			return x - shift;
		}

		public static RectD TileBoundsInUnitySpace(UnwrappedTileId unwrappedTileId, Vector2d worldCenter, float scale)
		{
			var min = PixelsToMeters(
				unwrappedTileId.X * TileSize,
				unwrappedTileId.Y * TileSize,
				unwrappedTileId.Z);
			var max = PixelsToMeters(
				(unwrappedTileId.X + 1) * TileSize,
				(unwrappedTileId.Y + 1) * TileSize,
				unwrappedTileId.Z);
			var centerX = (min.x + max.x) * 0.5;
			var shift = Math.Round((centerX - worldCenter.x) / (OriginShift * 2.0)) * (OriginShift * 2.0);
			if (shift != 0.0)
			{
				min.x -= shift;
				max.x -= shift;
			}
			return new RectD((min - worldCenter)/scale, (max - min)/scale);
		}
		
		/// <summary>
		/// Gets the WGS84 longitude of the northwest corner from a tile's X position and zoom level.
		/// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames.
		/// </summary>
		/// <param name="x"> Tile X position. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> NW Longitude. </returns>
		private static double TileXToNWLongitude(int x, int zoom)
		{
			var n = PowerTable2[zoom];
			var lon_deg = (float)x / n * 360.0 - 180.0;
			return lon_deg;
		}

		/// <summary>
		/// Gets the WGS84 latitude of the northwest corner from a tile's Y position and zoom level.
		/// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames.
		/// </summary>
		/// <param name="y"> Tile Y position. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> NW Latitude. </returns>
		private static double TileYToNWLatitude(int y, int zoom)
		{
			var n = Math.Pow(2.0, zoom);
			var lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
			var lat_deg = lat_rad * 180.0 / Math.PI;
			return lat_deg;
		}

		/// <summary>
		/// Gets the <see cref="T:Mapbox.Utils.Vector2dBounds"/> of a tile.
		/// </summary>
		/// <param name="x"> Tile X position. </param>
		/// <param name="y"> Tile Y position. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> The <see cref="T:Mapbox.Utils.Vector2dBounds"/> of the tile. </returns>
		public static LatitudeLongitudeBounds TileIdToBounds(CanonicalTileId tileId)
		{
			return TileIdToBounds(tileId.X, tileId.Y, tileId.Z);
		}
		
		/// <summary>
		/// Gets the <see cref="T:Mapbox.Utils.Vector2dBounds"/> of a tile.
		/// </summary>
		/// <param name="x"> Tile X position. </param>
		/// <param name="y"> Tile Y position. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> The <see cref="T:Mapbox.Utils.Vector2dBounds"/> of the tile. </returns>
		private static LatitudeLongitudeBounds TileIdToBounds(int x, int y, int zoom)
		{
			//new line takes y+1 for latitude calculation. so it's taking tile one below, and latitude of its top left corner
			//top left corner of y+1 is bottom left (or sw) of current
			var southWestLatLng = new LatitudeLongitude(TileYToNWLatitude(y + 1, zoom), TileXToNWLongitude(x, zoom));
			//again topLeft of tile to the right, is topRight (or ne) of the current tile
			var northEastLatLng = new LatitudeLongitude(TileYToNWLatitude(y, zoom), TileXToNWLongitude(x + 1, zoom));
			return new LatitudeLongitudeBounds(southWestLatLng, northEastLatLng);
		}

		/// <summary>
		/// Gets the WGS84 lat/lon of the center of a tile.
		/// </summary>
		/// <param name="x"> Tile X position. </param>
		/// <param name="y"> Tile Y position. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns>A <see cref="T:UnityEngine.Vector2d"/> of lat/lon coordinates.</returns>
		public static Vector2d TileIdToCenterLatitudeLongitude(int x, int y, int zoom)
		{
			var bb = TileIdToBounds(x, y, zoom);
			var center = bb.Center;
			return new Vector2d(center.Latitude, center.Longitude);
		}

		private static double Resolution(int zoom)
		{
			return InitialResolution / PowerTable2[zoom];
		}

		public static Vector2d PixelsToMeters(float p1, float p2, int zoom)
		{
			var res = Resolution(zoom);
			var met = new Vector2d((p1 * res - OriginShift), -(p2 * res - OriginShift));
			return met;
		}
		
		/// <summary> Converts a coordinate to a tile identifier. </summary>
		/// <param name="coord"> Geographic coordinate. </param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns>The to tile identifier.</returns>
		/// <example>
		/// Convert a geocoordinate to a TileId:
		/// <code>
		/// var unwrappedTileId = TileCover.CoordinateToTileId(new Vector2d(40.015, -105.2705), 18);
		/// Console.Write("UnwrappedTileId: " + unwrappedTileId.ToString());
		/// </code>
		/// </example>
		public static UnwrappedTileId CoordinateToTileId(LatitudeLongitude coord, int zoom)
		{
			var lat = coord.Latitude;
			var lng = coord.Longitude;

			// See: http://wiki.openstreetmap.org/wiki/Slippy_map_tilenames
			var x = (int)Math.Floor((lng + 180.0) / 360.0 * Math.Pow(2.0, zoom));
			var y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(lat * Math.PI / 180.0)
			                                        + 1.0 / Math.Cos(lat * Math.PI / 180.0)) / Math.PI) / 2.0 * Math.Pow(2.0, zoom));

			return new UnwrappedTileId(zoom, x, y);
		}

		/// <summary>
		///  Converts a Web Mercator coordinate to a tile identifier. https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Derivation_of_tile_names
		/// </summary>
		/// <param name="webMerc">Web Mercator coordinate</param>
		/// <param name="zoom">Zoom level</param>
		/// <returns>The to tile identifier.</returns>
		public static UnwrappedTileId WebMercatorToTileId(Vector2d webMerc, int zoom)
		{
			// See:  https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Derivation_of_tile_names
			double tileCount = Math.Pow(2, zoom);

			//this SDK defines Vector2d.x as latitude and Vector2d.y as longitude
			//same for WebMerc, so we have to flip x/y to make this formula work
			double dblX = webMerc.x / Constants.Map.WebMercMax;
			double dblY = webMerc.y / Constants.Map.WebMercMax;

			int x = (int)Math.Floor((1 + dblX) / 2 * tileCount);
			int y = (int)Math.Floor((1 - dblY) / 2 * tileCount);
			return new UnwrappedTileId(zoom, x, y);
		}

		public static double GeoDistance(
			double lon1,
			double lat1,
			double lon2,
			double lat2)
		{
			var toRad = 3.141592653589793 / 180;
			double dlon = (lon2 - lon1) * toRad;
			double dlat = (lat2 - lat1) * toRad;

			double a = (Math.Sin(dlat / 2) * Math.Sin(dlat / 2)) + Math.Cos(lat1 * toRad) * Math.Cos(lat2 * toRad) * (Math.Sin(dlon / 2) * Math.Sin(dlon / 2));
			double angle = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return angle * 6378.16;
		}

		public static float LatitudeElevationCompensation(double latitude)
		{
			return Mathf.Cos(Mathf.Deg2Rad * (float) latitude);
		}
	}
}
