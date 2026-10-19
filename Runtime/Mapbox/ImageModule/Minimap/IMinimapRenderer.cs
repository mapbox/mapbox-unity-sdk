using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Draws what <see cref="MinimapView"/> works out. Implement this to render the minimap
    /// somewhere other than a UI canvas - world-space quads on a cockpit display, sprites, a
    /// render texture composited elsewhere.
    ///
    /// The view calls <see cref="UpdateTile"/> for every tracked tile every frame, so an
    /// implementation should treat it as a hot path: look the tile up, set what changed, return.
    /// Positions and sizes arrive in whatever units the view was configured with (pixels, for the
    /// shipped UI renderer).
    /// </summary>
    public interface IMinimapRenderer
    {
        /// <summary>A tile entered the minimap. Acquire whatever draws it.</summary>
        void AddTile(UnwrappedTileId tileId);

        /// <summary>
        /// Per-frame state for a tile: where it goes, how big it is, and what it shows. A texture
        /// of <see cref="MinimapTileTexture.None"/> means "nothing to draw yet" - hide the tile
        /// rather than dropping it, because its data usually arrives a frame or two later.
        /// </summary>
        void UpdateTile(UnwrappedTileId tileId, MinimapTileRect rect, MinimapTileTexture texture);

        /// <summary>A tile left the minimap. Release whatever was drawing it.</summary>
        void RemoveTile(UnwrappedTileId tileId);

        /// <summary>
        /// Sub-tile offset of the whole minimap, so the view centre lands on the map centre rather
        /// than on the centre tile's corner. Recomputed every frame.
        /// </summary>
        void SetRootOffset(Vector2 offset);

        /// <summary>
        /// Rotation of the whole minimap in degrees, or 0 for a north-up minimap.
        /// </summary>
        void SetOrientation(float degrees);

        /// <summary>Release everything. Called when the view is torn down.</summary>
        void Clear();
    }
}
