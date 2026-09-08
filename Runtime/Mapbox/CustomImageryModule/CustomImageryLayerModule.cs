using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.ImageModule;
using UnityEngine;

namespace Mapbox.CustomImageryModule
{
    /// <summary>
    /// How a custom imagery layer takes part in the map.
    /// </summary>
    public enum CustomImageryMode
    {
        /// <summary>
        /// Draws on the map tiles, and a tile waits for this imagery before it counts as loaded.
        /// The original behaviour, and what a single imagery layer wants.
        /// </summary>
        Display,

        /// <summary>
        /// Draws on the map tiles, but never holds a tile back. The imagery appears whenever it
        /// arrives instead of the tile waiting for it.
        /// </summary>
        DisplayNonBlocking,

        /// <summary>
        /// Fetches, caches and keeps imagery without drawing it or holding tiles back. For a second
        /// imagery source that something else reads - a minimap on a different style, an offline
        /// pre-cache - since a tile has only one imagery slot and two drawing layers would overwrite
        /// each other.
        /// </summary>
        DataOnly
    }

    /// <summary>
    /// A custom imagery layer that can stay out of the map's way, and can fetch at a coarser zoom
    /// than the tiles it belongs to.
    ///
    /// Exists because <see cref="StaticApiLayerModule"/> always publishes into
    /// <c>UnityMapTile.ImageContainer</c>, which holds exactly one <c>RasterData</c> per tile. Two
    /// imagery layers therefore fight over that slot, last writer winning, and the visible map -
    /// not just whoever wanted the second source - ends up wrong. <see cref="CustomImageryMode.DataOnly"/>
    /// is the way to have a second source at all.
    ///
    /// Every override here maps render tile ids through <see cref="GetDataId"/>, the same trick
    /// <c>TerrainLayerModule</c> uses to share one data tile across sixteen render tiles. Placement
    /// needs no work: consumers derive the sub-rect from the data's own tile id via
    /// <c>CalculateScaleOffsetAtZoom</c>, so a coarser ancestor already renders the correct quadrant.
    /// </summary>
    public class CustomImageryLayerModule : StaticApiLayerModule
    {
        private readonly CustomImageryMode _mode;
        private readonly int _dataZoomOffset;

        // Per-frame scratch for RetainTiles. Kept apart from the coroutine list below: the source
        // enumerates what LoadTiles hands it across several frames, so a buffer shared with a
        // per-frame caller can be cleared out from under it mid-enumeration.
        private readonly HashSet<CanonicalTileId> _retainScratch = new HashSet<CanonicalTileId>();
        private readonly List<CanonicalTileId> _loadScratch = new List<CanonicalTileId>();

        public CustomImageryLayerModule(
            Source<RasterData> source,
            StaticLayerModuleSettings settings,
            CustomImageryMode mode,
            int dataZoomOffset)
            : base(source, settings)
        {
            _mode = mode;
            _dataZoomOffset = dataZoomOffset;
        }

        /// <summary>
        /// The tile actually fetched for a given render tile. With a negative offset this is an
        /// ancestor, so one fetch covers 4^offset render tiles.
        ///
        /// Clamped so it can never ask for something the source cannot serve: not finer than the
        /// render tile itself, not past <c>ClampDataLevelToMax</c>, and not below the layer's own
        /// minimum zoom.
        /// </summary>
        protected CanonicalTileId GetDataId(CanonicalTileId tileId)
        {
            if (_dataZoomOffset == 0)
            {
                return tileId;
            }

            var targetZ = tileId.Z + _dataZoomOffset;
            targetZ = Mathf.Max(targetZ, (int)_settings.RejectTilesOutsideZoom.x);
            targetZ = Mathf.Min(targetZ, _settings.DataSettings.ClampDataLevelToMax);
            targetZ = Mathf.Clamp(targetZ, 0, tileId.Z);

            return targetZ == tileId.Z ? tileId : tileId.ParentAt(targetZ);
        }

        private bool IsZoomSupported(int z)
        {
            // Reimplemented rather than reused: the base class keeps its copy private.
            return _settings.RejectTilesOutsideZoom.x <= z && _settings.RejectTilesOutsideZoom.y >= z;
        }

        public override bool LoadInstant(UnityMapTile unityTile)
        {
            if (_mode == CustomImageryMode.DataOnly)
            {
                // True unconditionally, and that is load-bearing rather than lazy: MapboxMapVisualizer.CreateTile
                // promotes a tile only once EVERY module returns true, so a layer that draws nothing
                // must never report "not yet" - it would hold the whole tile cover at Temporary and
                // TileLoaded would never fire for any of it.
                return true;
            }

            if (!IsZoomSupported(unityTile.CanonicalTileId.Z))
            {
                return true;
            }

            if (_rasterSource.GetInstantData(GetDataId(unityTile.CanonicalTileId), out var instantData))
            {
                unityTile.ImageContainer.SetImageData(instantData);
                return true;
            }

            return _mode == CustomImageryMode.DisplayNonBlocking;
        }

        public override void LoadTempTile(UnityMapTile unityTile)
        {
            if (_mode == CustomImageryMode.DataOnly)
            {
                return;
            }

            // The base walks up from the render tile looking for any ancestor already in memory, so
            // it finds the offset tile on its own - no id mapping needed here.
            base.LoadTempTile(unityTile);
        }

        public override bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
        {
            if (_dataZoomOffset == 0)
            {
                return base.RetainTiles(retainedTiles);
            }

            // Retain the tiles actually held, not the render tiles: retaining ids the source never
            // fetched would let the cache evict the ones it did.
            _retainScratch.Clear();
            foreach (var tileId in retainedTiles)
            {
                _retainScratch.Add(GetDataId(tileId));
            }

            return _rasterSource.RetainTiles(_retainScratch);
        }

        /// <summary>
        /// Also covers the initial tile-cover pre-warm: <c>GetTileCoverCoroutines</c> is not virtual,
        /// but it builds its coroutines by calling this, so redirecting here redirects that too.
        /// </summary>
        public override IEnumerator LoadTileData(CanonicalTileId tileId, Action<MapboxTileData> callback = null)
        {
            return base.LoadTileData(GetDataId(tileId), callback);
        }

        public override IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
        {
            if (_dataZoomOffset == 0)
            {
                yield return base.LoadTiles(tiles);
                yield break;
            }

            // Materialized, and distinct per call site: the source enumerates this on its own
            // schedule over several frames.
            _loadScratch.Clear();
            foreach (var tileId in tiles)
            {
                var dataId = GetDataId(tileId);
                if (!_loadScratch.Contains(dataId))
                {
                    _loadScratch.Add(dataId);
                }
            }

            yield return base.LoadTiles(_loadScratch);
        }
    }
}
