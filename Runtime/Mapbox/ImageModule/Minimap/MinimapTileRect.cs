using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Where and how large one tile should be drawn, in the minimap's own units, relative to the
    /// minimap's centre.
    ///
    /// Deliberately not a <c>RectTransform</c> or any other UI type: the projection that produces
    /// these knows nothing about how they are drawn, so the same numbers serve a UI renderer, a
    /// world-space quad renderer, or a test that never renders at all. Tiles are square, hence one
    /// <see cref="Size"/> rather than a <c>Vector2</c>.
    /// </summary>
    public struct MinimapTileRect
    {
        /// <summary>Offset from the minimap centre. +X is east, +Y is north.</summary>
        public readonly Vector2 Position;

        /// <summary>Edge length. Coarser tiles are proportionally larger than finer ones.</summary>
        public readonly float Size;

        public MinimapTileRect(Vector2 position, float size)
        {
            Position = position;
            Size = size;
        }
    }
}
