//-----------------------------------------------------------------------
// <copyright file="TileCover.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
	using System;
	using System.Collections.Generic;
	using Mapbox.Utils;
	using UnityEngine;

	/// <summary>
	///     Helper funtions to get a tile cover, i.e. a set of tiles needed for
	///     covering a bounding box.
	/// </summary>
	public static class TileCover
	{
		/// <summary> Get a tile cover for the specified bounds and zoom. </summary>
		/// <param name="bounds"> Geographic bounding box.</param>
		/// <param name="zoom"> Zoom level. </param>
		/// <returns> The tile cover set. </returns>
		/// <example>
		/// Build a map of Colorado using TileCover:
		/// <code>
		/// var sw = new Vector2d(36.997749, -109.0524961);
		/// var ne = new Vector2d(41.0002612, -102.0609668);
		/// var coloradoBounds = new Vector2dBounds(sw, ne);
		/// var tileCover = TileCover.Get(coloradoBounds, 8);
		/// Console.Write("Tiles Needed: " + tileCover.Count);
		/// foreach (var id in tileCover)
		/// {
		/// 	var tile = new RasterTile();
		/// 	var parameters = new Tile.Parameters();
		/// 	parameters.Id = id;
		///		parameters.Fs = MapboxAccess.Instance;
		///		parameters.MapId = "mapbox://styles/mapbox/outdoors-v10";
		///		tile.Initialize(parameters, (Action)(() =&gt;
		///		{
		///			// Place tiles and load textures.
		///		}));
		///	}
		/// </code>
		/// </example>
		public static HashSet<CanonicalTileId> Get(Vector2dBounds bounds, int zoom)
		{
			var tiles = new HashSet<CanonicalTileId>();

			if (bounds.IsEmpty() ||
				bounds.South > Constants.LatitudeMax ||
				bounds.North < -Constants.LatitudeMax)
			{
				return tiles;
			}

			var hull = Vector2dBounds.FromCoordinates(
				new Vector2d(Math.Max(bounds.South, -Constants.LatitudeMax), bounds.West),
				new Vector2d(Math.Min(bounds.North, Constants.LatitudeMax), bounds.East));

			var sw = CoordinateToTileId(hull.SouthWest, zoom);
			var ne = CoordinateToTileId(hull.NorthEast, zoom);

			// Scanlines.
			for (var x = sw.X; x <= ne.X; ++x)
			{
				for (var y = ne.Y; y <= sw.Y; ++y)
				{
					tiles.Add(new UnwrappedTileId(zoom, x, y).Canonical);
				}
			}

			return tiles;
		}


		public static HashSet<UnwrappedTileId> GetWithWebMerc(Vector2dBounds bounds, int zoom)
		{
			HashSet<UnwrappedTileId> tiles = new HashSet<UnwrappedTileId>();
			HashSet<CanonicalTileId> canonicalTiles = new HashSet<CanonicalTileId>();

			if (bounds.IsEmpty()) { return tiles; }

			//stay within WebMerc bounds
			Vector2d swWebMerc = new Vector2d(Math.Max(bounds.SouthWest.x, -Constants.WebMercMax), Math.Max(bounds.SouthWest.y, -Constants.WebMercMax));
			Vector2d neWebMerc = new Vector2d(Math.Min(bounds.NorthEast.x, Constants.WebMercMax), Math.Min(bounds.NorthEast.y, Constants.WebMercMax));

			//UnityEngine.Debug.LogFormat("swWebMerc:{0}/{1} neWebMerc:{2}/{3}", swWebMerc.x, swWebMerc.y, neWebMerc.x, neWebMerc.y);

			UnwrappedTileId swTile = WebMercatorToTileId(swWebMerc, zoom);
			UnwrappedTileId neTile = WebMercatorToTileId(neWebMerc, zoom);

			//UnityEngine.Debug.LogFormat("swTile:{0} neTile:{1}", swTile, neTile);

			for (int x = swTile.X; x <= neTile.X; x++)
			{
				for (int y = neTile.Y; y <= swTile.Y; y++)
				{
					UnwrappedTileId uwtid = new UnwrappedTileId(zoom, x, y);
					//hack: currently too many tiles are created at lower zoom levels
					//investigate formulas, this worked before
					if (!canonicalTiles.Contains(uwtid.Canonical))
					{
						//Debug.LogFormat("TileCover.GetWithWebMerc: {0}/{1}/{2}", zoom, x, y);
						tiles.Add(uwtid);
						canonicalTiles.Add(uwtid.Canonical);
					}
				}
			}

			return tiles;
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
		public static UnwrappedTileId CoordinateToTileId(Vector2d coord, int zoom)
		{
			var lat = coord.x;
			var lng = coord.y;

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
			double dblX = webMerc.x / Constants.WebMercMax;
			double dblY = webMerc.y / Constants.WebMercMax;

			//dblX = 1 + dblX;
			//dblY = 1 - dblY;
			//dblX /= 2;
			//dblY /= 2;
			//dblX *= tileCount;
			//dblY *= tileCount;
			//int x = (int)Math.Floor(dblX);
			//int y = (int)Math.Floor(dblY);
			//return new UnwrappedTileId(zoom, x, y);

			int x = (int)Math.Floor((1 + dblX) / 2 * tileCount);
			int y = (int)Math.Floor((1 - dblY) / 2 * tileCount);
			return new UnwrappedTileId(zoom, x, y);
		}


	}
}
