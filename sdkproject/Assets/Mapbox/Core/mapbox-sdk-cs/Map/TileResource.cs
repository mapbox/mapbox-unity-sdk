//-----------------------------------------------------------------------
// <copyright file="TileResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.Map
{
    using Platform;

    internal sealed class TileResource : IResource
    {
        private readonly string query;

        internal TileResource(string query)
        {
            this.query = query;
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

        public string GetUrl()
        {
            return this.query;
        }
    }
}
