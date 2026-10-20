using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;

namespace Mapbox.BaseModule.Data.Interfaces
{
    public interface IMapVisualizer
    {
        public void Load(TileCover tileCover);
        public void LoadSnapshot(TileCover tileCover);
        IEnumerator Initialize();
        IEnumerator LoadTileCoverToMemory(TileCover tileCover);
        void OnDestroy();
        Dictionary<UnwrappedTileId, UnityMapTile> ActiveTiles { get; }
        bool TryGetLayerModule<T>(out T layerModule) where T : ILayerModule;

        /// <summary>
        /// A tile reached its own detail level. Fires for a subset of tiles only: a coarser
        /// Temporary stand-in or a synthesized Filler is drawn without raising this, and a tile
        /// whose modules never all report ready never raises it at all. Use it for the tile's data,
        /// not to track what is on screen - read <see cref="ActiveTiles"/> for that. Does not pair
        /// one-to-one with <see cref="TileUnloading"/>.
        /// </summary>
        event Action<UnityMapTile> TileLoaded;

        /// <summary>
        /// A tile is leaving the active set and is about to be pooled, whatever fidelity it reached.
        /// Still in <see cref="ActiveTiles"/> while this runs, and its data is cleared immediately
        /// after - read anything you need now. Fires for tiles <see cref="TileLoaded"/> never
        /// announced.
        /// </summary>
        event Action<UnityMapTile> TileUnloading;
    }
}