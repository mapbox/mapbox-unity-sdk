//-----------------------------------------------------------------------
// <copyright file="ClassicRasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
	/// <summary>
	///    A raster tile from the Mapbox Map API, a encoded image representing a geographic
	///    bounding box. Usually JPEG or PNG encoded.
	/// See <see cref="T:Mapbox.Map.RasterTile"/> for usage.
    /// Read more about <see href="https://www.mapbox.com/api-documentation/legacy/static-classic/"> static classic maps </see>.
	/// </summary>
	public class ClassicRasterTile : RasterTile
	{
		internal override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeClassicRaster(Id, tilesetId);
		}
	}
}
