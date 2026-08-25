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
        /// </summary>
        event Action<UnityMapTile> TileLoaded;

        /// <summary>
        /// Map tile is about to be pooled. Still in <c>ActiveTiles</c> when the event
        /// fires; the visualizer removes it and calls <c>Recycle</c> immediately after.
        /// Observers that need to read per-tile state (TerrainData, etc.) before it's
        /// cleared should do so during this callback.
        /// </summary>
        event Action<UnityMapTile> TileUnloading;

        IReadOnlyDictionary<UnwrappedTileId, UnityMapTile> ActiveTiles { get; }
    }
}
