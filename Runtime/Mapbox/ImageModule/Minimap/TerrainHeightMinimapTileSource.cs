using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Shows each tile's raw terrain height texture - the encoded elevation raster, which reads as
    /// a greyscale relief map.
    /// </summary>
    public class TerrainHeightMinimapTileSource : IMinimapTileSource
    {
        public MinimapTileTexture GetTexture(UnityMapTile tile)
        {
            // Both null checks are load-bearing, not defensive habit: a tile is in ActiveTiles, and
            // so tracked and asked about, from the moment it is created - well before any container
            // has data. Without them this throws every frame for every tile still loading.
            var terrainData = tile.TerrainContainer?.TerrainData;
            if (terrainData == null || terrainData.Texture == null)
            {
                return MinimapTileTexture.None;
            }

            // Terrain data is shared across up to 16 render tiles (data zoom is render zoom - 2),
            // so this texture usually covers a coarser area than the tile drawing it. Pick out the
            // sub-rect that belongs to this tile, exactly as the tile shader does.
            var scaleOffset = tile.CanonicalTileId.CalculateScaleOffsetAtZoom(terrainData.TileId.Z);
            return new MinimapTileTexture(
                terrainData.Texture,
                new Rect(scaleOffset.z, scaleOffset.w, scaleOffset.x, scaleOffset.y));
        }
    }
}
