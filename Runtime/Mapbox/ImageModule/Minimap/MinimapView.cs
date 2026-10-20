using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;

namespace Mapbox.ImageModule.Minimap
{
    /// <summary>
    /// Keeps a minimap in step with the map's active tiles. Owns no scene objects and knows nothing
    /// about UI: it reads the visualizer, asks an <see cref="IMinimapTileSource"/> what each tile
    /// shows, asks a <see cref="MinimapProjection"/> where it goes, and hands the result to an
    /// <see cref="IMinimapRenderer"/>.
    ///
    /// <b>It polls <c>ActiveTiles</c> every frame rather than following tile lifecycle events, and
    /// that is the whole design.</b> <c>ActiveTiles</c> is complete at every instant it can be
    /// read - the visualizer shows a replacement tile before pooling what it replaces, synchronously,
    /// inside one call - so a snapshot of it never has a hole. Mirroring it incrementally from
    /// <c>TileLoaded</c> and <c>TileUnloading</c> reintroduces one, and worse: the unload arrives
    /// and hides a tile immediately, while <c>TileLoaded</c> fires only once a tile reaches its own
    /// detail level - never for a Temporary stand-in or a Filler, and never at all for a tile that
    /// does not finish. So the replacement stays blank, and tiles that are plainly on screen are
    /// missed entirely. That gap shows as flicker on every zoom change. Reconciling against the
    /// dictionary inherits its guarantee instead of trying to rebuild it, and needs less code.
    /// </summary>
    public class MinimapView
    {
        private readonly IMapVisualizer _visualizer;
        private readonly IMapInformation _mapInformation;
        private readonly IMinimapRenderer _renderer;

        private readonly HashSet<UnwrappedTileId> _tracked = new HashSet<UnwrappedTileId>();

        // Reused across frames: reconciliation runs every frame, and collecting departures into a
        // fresh list each time would allocate per frame for the entire session.
        private readonly List<UnwrappedTileId> _departedScratch = new List<UnwrappedTileId>();

        public MinimapProjection Projection { get; }

        /// <summary>
        /// What each tile shows. Swappable at runtime - assigning a new source takes effect on the
        /// next frame, with no need to rebuild anything, because textures are re-read every frame
        /// anyway.
        /// </summary>
        public IMinimapTileSource TileSource { get; set; }

        /// <summary>
        /// Degrees to rotate the whole minimap, or null to keep it north-up.
        ///
        /// Left as a caller-supplied value rather than read from <c>MapInformation.Bearing</c> here
        /// because the sign and offset that look right depend on how the camera rig is wired and on
        /// which way the renderer's own axes point.
        /// </summary>
        public float? OrientationDegrees { get; set; }

        public MinimapView(
            IMapVisualizer visualizer,
            IMapInformation mapInformation,
            IMinimapRenderer renderer,
            IMinimapTileSource tileSource,
            MinimapProjection projection = null)
        {
            _visualizer = visualizer;
            _mapInformation = mapInformation;
            _renderer = renderer;
            TileSource = tileSource;
            Projection = projection ?? new MinimapProjection();
        }

        /// <summary>
        /// Bring the minimap up to date. Call once per frame.
        /// </summary>
        public void Update()
        {
            if (_visualizer == null || _mapInformation == null || _renderer == null || TileSource == null)
            {
                return;
            }

            Projection.Refresh(_mapInformation);

            var activeTiles = _visualizer.ActiveTiles;

            // Arrivals and updates first, departures second. Ordering matters: a tile replacing
            // another in the same area must be drawn before the one it replaces is dropped, or the
            // minimap shows a hole for a frame - which is the flicker this class exists to avoid.
            foreach (var pair in activeTiles)
            {
                if (_tracked.Add(pair.Key))
                {
                    _renderer.AddTile(pair.Key);
                }

                // Re-read position and texture for every tile, every frame, not just new ones.
                // Two reasons: the tile's data can arrive at any point after it becomes active, and
                // UnityMapTile instances are pooled and reissued under different ids, so the
                // instance behind a given id is only trustworthy for the frame it was read in.
                // Keying off the dictionary means reuse needs no special handling at all - the old
                // id simply departs and the new one arrives.
                _renderer.UpdateTile(
                    pair.Key,
                    Projection.GetTileRect(pair.Key),
                    TileSource.GetTexture(pair.Value));
            }

            _departedScratch.Clear();
            foreach (var tileId in _tracked)
            {
                if (!activeTiles.ContainsKey(tileId))
                {
                    _departedScratch.Add(tileId);
                }
            }

            for (var i = 0; i < _departedScratch.Count; i++)
            {
                _tracked.Remove(_departedScratch[i]);
                _renderer.RemoveTile(_departedScratch[i]);
            }

            _renderer.SetRootOffset(Projection.GetRootOffset());
            _renderer.SetOrientation(OrientationDegrees ?? 0f);
        }

        public void Clear()
        {
            _tracked.Clear();
            _departedScratch.Clear();
            _renderer?.Clear();
        }
    }
}
