//-----------------------------------------------------------------------
// <copyright file="TileResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Mapbox.Map
{
	using Platform;
	using System;
	using Mapbox.Unity.Telemetry;

	internal sealed class TileResource : IResource
	{
		readonly string _query;

		internal TileResource(string query)
		{
			_query = query;
		}

		public static TileResource MakeRaster(CanonicalTileId id, string styleUrl)
		{
			return new TileResource(string.Format("{0}/{1}", MapUtils.NormalizeStaticStyleURL(styleUrl ?? "mapbox://styles/mapbox/satellite-v9"), id));
		}

		internal static TileResource MakeRetinaRaster(CanonicalTileId id, string styleUrl)
		{
			return new TileResource(string.Format("{0}/{1}@2x", MapUtils.NormalizeStaticStyleURL(styleUrl ?? "mapbox://styles/mapbox/satellite-v9"), id));
		}

		public static TileResource MakeClassicRaster(CanonicalTileId id, string mapId)
		{
			return new TileResource(string.Format("{0}/{1}.png", MapUtils.MapIdToUrl(mapId ?? "mapbox.satellite"), id));
		}

		internal static TileResource MakeClassicRetinaRaster(CanonicalTileId id, string mapId)
		{
			return new TileResource(string.Format("{0}/{1}@2x.png", MapUtils.MapIdToUrl(mapId ?? "mapbox.satellite"), id));
		}

		public static TileResource MakeRawPngRaster(CanonicalTileId id, string mapId)
		{
			return new TileResource(string.Format("{0}/{1}.pngraw", MapUtils.MapIdToUrl(mapId ?? "mapbox.terrain-rgb"), id));
		}

		public static TileResource MakeVector(CanonicalTileId id, string mapId)
		{
			return new TileResource(string.Format("{0}/{1}.vector.pbf", MapUtils.MapIdToUrl(mapId ?? "mapbox.mapbox-streets-v7"), id));
		}

		internal static TileResource MakeStyleOptimizedVector(CanonicalTileId id, string mapId, string optimizedStyleId, string modifiedDate)
		{
			return new TileResource(string.Format("{0}/{1}.vector.pbf?style={2}@{3}", MapUtils.MapIdToUrl(mapId ?? "mapbox.mapbox-streets-v7"), id, optimizedStyleId, modifiedDate));
		}

		public string GetUrl()
		{
			var uriBuilder = new UriBuilder(_query);
			if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
			{
				uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + TelemetryFactory.EventQuery;
			}
			else
			{
				uriBuilder.Query = TelemetryFactory.EventQuery;
			}

			return uriBuilder.ToString();
		}
	}
}
