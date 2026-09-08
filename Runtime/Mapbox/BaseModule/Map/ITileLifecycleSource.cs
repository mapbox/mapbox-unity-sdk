using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;

namespace Mapbox.BaseModule.Map
{
    /// <summary>
    /// Read-only "subject" surface of the tile-lifecycle observer pattern. Implemented
    /// by <see cref="MapboxMapVisualizer"/> and handed to every layer module through
    /// <c>ILayerModule.Initialize</c>; consumed by the modules that want it (and helpers they own,
    /// e.g. terrain bounds tracking). Modules with no interest in tile lifecycle ignore it.
    ///
    /// Carrying only what an observer needs lets helpers depend on this narrow contract
    /// instead of the full <see cref="MapboxMapVisualizer"/>, which simplifies testing
    /// and keeps modules from reaching into orchestration internals.
    /// </summary>
    public interface ITileLifecycleSource
    {
        IMapInformation MapInformation { get; }

        /// <summary>
        /// Map tile finished loading with targeted detail level data. The tile is in
        /// <c>ActiveTiles</c> by the time this fires.
        ///
        /// <b>Fires for a subset of tiles only,</b> and that catches people out. A tile is drawn as
        /// soon as it enters the active set - as a coarser <c>Temporary</c> stand-in, or a
        /// <c>Filler</c> synthesized from neighbouring zoom levels - and neither of those raises
        /// this. Promotion to Finished needs every layer module to report ready, so a tile whose
        /// modules never all become ready is visible on screen and never raises it at all.
        ///
        /// <b>So do not use this to track which tiles are on screen.</b> There is no event for that;
        /// read <c>MapVisualizer.ActiveTiles</c> instead, which is complete at every instant it can
        /// be read - the visualizer shows a replacement tile before pooling what it replaces, inside
        /// one synchronous call. Use this event for the tile's <i>data</i>: elevation, imagery, mesh
        /// features.
        ///
        /// <b>Not paired one-to-one with <see cref="TileUnloading"/>,</b> which fires for every tile
        /// leaving whatever fidelity it reached. Expect unload callbacks for tiles this never
        /// announced, and make teardown tolerant of ids it never saw.
        /// </summary>
        event Action<UnityMapTile> TileLoaded;

        /// <summary>
        /// Map tile is about to be pooled. Still in <c>ActiveTiles</c> when the event
        /// fires; the visualizer removes it and calls <c>Recycle</c> immediately after.
        /// Observers that need to read per-tile state (TerrainData, etc.) before it's
        /// cleared should do so during this callback.
        ///
        /// Fires for <b>every</b> tile leaving the active set, whatever fidelity it reached - so it
        /// does not pair with <see cref="TileLoaded"/>, which fires only for tiles that reached
        /// their own detail level. This will fire for tiles <see cref="TileLoaded"/> never
        /// announced.
        /// </summary>
        event Action<UnityMapTile> TileUnloading;

        IReadOnlyDictionary<UnwrappedTileId, UnityMapTile> ActiveTiles { get; }
    }
}
