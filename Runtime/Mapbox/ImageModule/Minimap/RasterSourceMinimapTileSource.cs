using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Draws imagery straight from a raster source, instead of from the texture the map put on the
    /// tile. That is what lets a minimap show a different style from the main map - satellite on the
    /// map, streets on the minimap - because a tile has only one imagery slot and whichever layer
    /// writes it last wins.
    ///
    /// Pair it with a layer whose mode is <c>DataOnly</c>, so that layer fetches and caches without
    /// drawing over the map:
    /// <code>
    /// if (map.MapVisualizer.TryGetLayerModule&lt;CustomImageryLayerModule&gt;(out var streets))
    ///     minimap.SetTileSource(new RasterSourceMinimapTileSource(streets.RasterSource));
    /// </code>
    ///
    /// Takes a plain <c>Source&lt;RasterData&gt;</c> rather than a layer module, so any raster
    /// source works and this stays in the imagery module with no dependency on custom imagery.
    /// </summary>
    public class RasterSourceMinimapTileSource<T> : IMinimapTileSource where T : RasterData
    {
        private readonly Source<T> _source;
        private readonly int _maxCoarserLevels;

        /// <param name="source">Where to read imagery from - a layer module's <c>RasterSource</c>.</param>
        /// <param name="maxCoarserLevels">How many levels above a tile to look when the tile's own
        /// imagery is not there. Needed because a source can deliberately fetch coarser than the map
        /// draws (a <c>DataZoomOffset</c> of -2 means one fetch per 16 tiles), and because a fetch
        /// clamped by the source's zoom limits lands coarser still. Searching rather than being told
        /// the offset means this cannot disagree with the layer's own setting, and the finest
        /// available data always wins. The default covers an offset of -4.</param>
        public RasterSourceMinimapTileSource(Source<T> source, int maxCoarserLevels = 4)
        {
            _source = source;
            _maxCoarserLevels = Mathf.Max(0, maxCoarserLevels);
        }

        public MinimapTileTexture GetTexture(UnityMapTile tile)
        {
            if (_source == null)
            {
                return MinimapTileTexture.None;
            }

            var tileId = tile.CanonicalTileId;
            var floorZ = Mathf.Max(0, tileId.Z - _maxCoarserLevels);

            // Finest first, so a tile that has its own imagery never renders an ancestor's.
            for (var z = tileId.Z; z >= floorZ; z--)
            {
                var candidate = z == tileId.Z ? tileId : tileId.ParentAt(z);
                if (!_source.GetInstantData(candidate, out var data) || data == null || data.Texture == null)
                {
                    continue;
                }

                // Sub-rect from the data's OWN tile id, not the candidate id: the source may hand
                // back something coarser again than what was asked for.
                var scaleOffset = tileId.CalculateScaleOffsetAtZoom(data.TileId.Z);
                return new MinimapTileTexture(
                    data.Texture,
                    new Rect(scaleOffset.z, scaleOffset.w, scaleOffset.x, scaleOffset.y));
            }

            return MinimapTileTexture.None;
        }
    }

    /// <summary>
    /// <see cref="RasterSourceMinimapTileSource{T}"/> for plain imagery, which is the common case.
    /// Terrain needs the generic form, since <c>TerrainData</c> sources are <c>Source&lt;TerrainData&gt;</c>
    /// and C# generics are not covariant.
    /// </summary>
    public class RasterSourceMinimapTileSource : RasterSourceMinimapTileSource<RasterData>
    {
        public RasterSourceMinimapTileSource(Source<RasterData> source, int maxCoarserLevels = 4)
            : base(source, maxCoarserLevels)
        {
        }
    }
}
