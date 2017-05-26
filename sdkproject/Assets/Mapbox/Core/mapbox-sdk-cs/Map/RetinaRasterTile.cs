//-----------------------------------------------------------------------
// <copyright file="RetinaClassicRasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
    /// <summary>
    /// A retin-resolution raster tile from the Mapbox Style API, an encoded image representing a geographic
    /// bounding box. Usually JPEG or PNG encoded.
    /// Like <see cref="T:Mapbox.Map.RasterTile"/>, but higher resolution.
    /// See <see href="https://www.mapbox.com/api-documentation/#retina"> retina documentation </see>.
    /// </summary>
    public class RetinaRasterTile : RasterTile
    {
        internal override TileResource MakeTileResource(string mapId)
        {
            return TileResource.MakeRetinaRaster(Id, mapId);
        }
    }
}