//-----------------------------------------------------------------------
// <copyright file="ClassicRasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Mapbox.Platform;

namespace Mapbox.Map
{
	/// <summary>
	///    A raster tile from the Mapbox Map API, a encoded image representing a geographic
	///    bounding box. Usually JPEG or PNG encoded.
	/// See <see cref="T:Mapbox.Map.RasterTile"/> for usage.
    /// Read more about <see href="https://www.mapbox.com/api-documentation/legacy/static-classic/"> static classic maps </see>.
	/// </summary>
	public class CustomRasterTile : RasterTile
	{
		internal override void Initialize(IFileSource fileSource, CanonicalTileId canonicalTileId, string tilesetId, Action p)
		{
			Cancel();

			_state = State.Loading;
			_id = canonicalTileId;
			_tilesetId = tilesetId;
			_callback = p;

			fileSource.CustomImageRequest(MakeTileResource(tilesetId).GetUrl(), HandleTileResponse, tileId: _id, tilesetId: tilesetId);
		}

		internal override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeCustomRaster(Id, tilesetId);
		}
	}
}
