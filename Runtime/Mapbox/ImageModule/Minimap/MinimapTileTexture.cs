using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// What one tile contributes to the minimap: a texture plus the sub-rect of it that actually
    /// belongs to this tile.
    ///
    /// The sub-rect is not decoration. Tile data can be borrowed from a coarser ancestor - terrain
    /// data is shared by up to 16 render tiles, and imagery falls back the same way - so the
    /// texture often covers a larger area than the tile drawing it. The SDK's own tile shader
    /// handles this by passing <c>CalculateScaleOffsetAtZoom</c> into the material as a
    /// scale/offset; a minimap that ignores it renders the wrong region of the world for any tile
    /// on borrowed data, which looks like tiles showing their neighbours' terrain.
    /// </summary>
    public struct MinimapTileTexture
    {
        public readonly Texture Texture;
        public readonly Rect UvRect;

        /// <summary>
        /// No texture available yet. A tile is legitimately active and tracked before its data
        /// arrives, so this is the normal state for a tile mid-load, not an error.
        /// </summary>
        public static readonly MinimapTileTexture None = new MinimapTileTexture(null, new Rect(0, 0, 1, 1));

        public MinimapTileTexture(Texture texture, Rect uvRect)
        {
            Texture = texture;
            UvRect = uvRect;
        }

        public bool HasTexture => Texture != null;
    }
}
