using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Turns map state into tile rectangles. Pure math over <see cref="IMapInformation"/> - no UI,
    /// no scene objects, no per-tile state - so it can be unit tested and reused by any renderer.
    ///
    /// Two problems this solves that a naive "one tile = N pixels" layout does not:
    ///
    /// <b>Mixed zoom levels.</b> The active tile set routinely spans several zooms at once, because
    /// the visualizer fills gaps with coarser <c>Temporary</c> and <c>Filler</c> stand-ins while
    /// real tiles load. Laying every tile out at a fixed size therefore misaligns coarse against
    /// fine. Instead every tile is expressed in units of one shared reference zoom via
    /// <c>2^(referenceZoom - tileZoom)</c>, so a tile one level coarser is drawn twice as wide and
    /// lands where its four finer siblings would have.
    ///
    /// <b>Discrete zoom ticks.</b> <see cref="IMapInformation.AbsoluteZoom"/> is the camera's
    /// continuous zoom floored, so using it alone holds the minimap at one scale for a whole zoom
    /// level and then pops at the integer boundary. <see cref="EffectiveTileSize"/> scales by the
    /// fractional part instead: 1x at the reference zoom, approaching 2x just before the next tick,
    /// halving at the tick where the reference itself advances. The product stays continuous
    /// through the flip.
    /// </summary>
    public class MinimapProjection
    {
        /// <summary>
        /// Size of one reference-zoom tile, in the renderer's units, before continuous-zoom and
        /// <see cref="ZoomOffset"/> scaling.
        /// </summary>
        public float TileSize = 300f;

        /// <summary>
        /// Zoom levels away from the map's own zoom. 0 matches the main view; negative shows a wider
        /// area. Applied as a scale factor on the whole layout rather than by loading different
        /// tiles - the minimap draws whatever the map has active, just larger or smaller.
        ///
        /// Fractional values are meaningful: this only ever reaches
        /// <see cref="EffectiveTileSize"/>'s exponent, so -1.5 is a valid three-quarters-of-a-level
        /// step. It does not have to be whole to keep tiles aligned either - alignment is
        /// <see cref="ReferenceZoom"/>'s job, and the effective tile size is already fractional
        /// whenever the map sits between two zoom levels.
        /// </summary>
        public float ZoomOffset;

        /// <summary>
        /// Web-mercator point the minimap centres on. Null uses
        /// <see cref="IMapInformation.CenterMercator"/>, which is right for a map that recentres as
        /// it moves.
        ///
        /// It is not right for every camera. The SDK's two example cameras implement different
        /// models: <c>SlippyMapCamera</c> reports a new centre to the map as it pans, so
        /// <c>CenterMercator</c> follows the view; <c>Moving3dCamera</c> keeps the map origin fixed
        /// and flies the camera over it, never reporting a centre at all - there,
        /// <c>CenterMercator</c> is the world origin the tiles are positioned against and stays put
        /// for the whole session while the view travels away from it. A minimap centred on it would
        /// hold the origin while the tiles marched off the panel.
        ///
        /// So for a camera that moves rather than recentres, set this to where the view actually is.
        /// <c>MinimapScript.ViewCenter</c> does it from a Transform.
        /// </summary>
        public Vector2d? CenterOverride;

        private IMapInformation _mapInformation;
        private Vector2d _center;
        private UnwrappedTileId _centerTile;

        /// <summary>
        /// The zoom every tile rect is normalized against this frame.
        ///
        /// Taken from the camera (<see cref="IMapInformation.AbsoluteZoom"/>), deliberately NOT
        /// derived from the active tiles' own zoom levels. Deriving it from the tiles was tried
        /// first and is a trap: a tile that never finishes loading and so never unloads would pin
        /// the reference to its zoom, freezing the whole minimap's scale until that tile happened
        /// to clear. The camera always reflects what the user is actually doing.
        /// </summary>
        public int ReferenceZoom { get; private set; }

        /// <summary>
        /// Units per reference-zoom tile after continuous-zoom and <see cref="ZoomOffset"/>
        /// scaling. This, not <see cref="TileSize"/>, is what tile rects are built from.
        /// </summary>
        public float EffectiveTileSize { get; private set; }

        /// <summary>
        /// Recompute the frame's shared basis. Call once per frame before reading any rect, so
        /// every tile in a frame is laid out against the same reference.
        /// </summary>
        public void Refresh(IMapInformation mapInformation)
        {
            _mapInformation = mapInformation;
            ReferenceZoom = mapInformation.AbsoluteZoom;
            EffectiveTileSize = TileSize * Mathf.Pow(2, mapInformation.Zoom - ReferenceZoom + ZoomOffset);
            _center = CenterOverride ?? mapInformation.CenterMercator;
            _centerTile = Conversions.WebMercatorToTileId(_center, ReferenceZoom);
        }

        /// <summary>
        /// Where a tile of any zoom level belongs, relative to the minimap centre.
        /// </summary>
        public MinimapTileRect GetTileRect(UnwrappedTileId tileId)
        {
            var scale = Mathf.Pow(2, ReferenceZoom - tileId.Z);
            var x = (_centerTile.X + 0.5f) - (tileId.X * scale + scale * 0.5f);
            var y = (_centerTile.Y + 0.5f) - (tileId.Y * scale + scale * 0.5f);

            return new MinimapTileRect(
                new Vector2(-x * EffectiveTileSize, y * EffectiveTileSize),
                EffectiveTileSize * scale);
        }

        /// <summary>
        /// Sub-tile offset of the whole minimap.
        ///
        /// <see cref="GetTileRect"/> positions tiles against the centre *tile*, which is an integer
        /// id, so on its own it snaps the minimap to tile boundaries and leaves it up to half a
        /// tile off. This makes up the remainder from live camera state.
        ///
        /// Computed from the camera rather than from any tracked tile's Unity transform on purpose:
        /// a tile stuck as a stand-in never has its transform updated to match the current view, so
        /// calibrating off an arbitrary tile shifts the entire minimap by however stale that one
        /// tile happens to be.
        /// </summary>
        public Vector2 GetRootOffset()
        {
            var referenceWorldSize = Conversions.TileSizeInUnitySpace(ReferenceZoom, _mapInformation.Scale);
            var corner = Conversions.TileTopLeftInUnitySpace(
                _centerTile.Canonical, _center, _mapInformation.Scale);
            var centre = new Vector2(
                corner.x + referenceWorldSize * 0.5f,
                corner.z - referenceWorldSize * 0.5f);

            return centre / referenceWorldSize * EffectiveTileSize;
        }
    }
}
