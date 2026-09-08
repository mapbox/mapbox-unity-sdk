using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Shows each tile's imagery texture, giving a minimap that matches what the map looks like
    /// from above.
    /// </summary>
    public class ImageryMinimapTileSource : IMinimapTileSource
    {
        public MinimapTileTexture GetTexture(UnityMapTile tile)
        {
            var imageData = tile.ImageContainer?.ImageData;
            if (imageData == null || imageData.Texture == null)
            {
                return MinimapTileTexture.None;
            }

            // Imagery falls back to coarser ancestor data just as terrain does - UnityTileImageContainer
            // passes this same scale/offset to the tile material - so it needs the sub-rect too.
            var scaleOffset = tile.CanonicalTileId.CalculateScaleOffsetAtZoom(imageData.TileId.Z);
            return new MinimapTileTexture(
                imageData.Texture,
                new Rect(scaleOffset.z, scaleOffset.w, scaleOffset.x, scaleOffset.y));
        }
    }
}
