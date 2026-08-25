using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using Mapbox.UnityMapService.DataSources;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.ImageModule.Terrain
{
    /// <summary>
    /// Terrain layer module. Orchestrates loading of terrain-RGB raster tiles from a
    /// <see cref="Source{TerrainData}"/>, configures CPU elevation extraction based on
    /// settings, and hands each tile through a <see cref="TerrainStrategy"/> that produces
    /// render + optional collider meshes.
    /// </summary>
    public class TerrainLayerModule : ITerrainLayerModule
    {
        private TerrainLayerModuleSettings _settings;
        private Source<TerrainData> _rasterSource;
        private HashSet<CanonicalTileId> _retainedTerrainTiles;
        private TerrainStrategy _terrainStrategy;
        // Owns the Min/MaxElevation bookkeeping that used to live in MapboxMapVisualizer.
        // Constructed in Initialize, from the tile-lifecycle source handed in there, and
        // disposed in OnDestroy. The visualizer never references this directly.
        private TerrainBoundsTracker _boundsTracker;
        // One-shot guard so QueryElevation doesn't spam the console on every call when the
        // user genuinely disabled CPU extraction.
        private bool _elevationDisabledWarningLogged;
        // Reused across GetDataId(IEnumerable) calls to avoid allocating a fresh HashSet
        // plus iterator chain from Where/Select/Distinct on every tile-cover update.
        private readonly HashSet<CanonicalTileId> _dataIdScratch = new HashSet<CanonicalTileId>();

        public TerrainLayerModule(Source<TerrainData> source, TerrainLayerModuleSettings settings) : base()
        {
            _settings = settings;
            _retainedTerrainTiles = new HashSet<CanonicalTileId>();
            _rasterSource = source;
            _terrainStrategy = new ElevatedTerrainStrategy();
        }
        
        public virtual IEnumerator Initialize(ITileLifecycleSource tileLifecycle)
        {
            // Subscribed before the first yield, so the tracker is watching before any tile can
            // load - the rest of this method waits on network work.
            // Idempotent: if a tracker is already running (re-init scenarios), keep it.
            // Constructing a second one would leak the first (coroutine + subscriptions).
            if (_boundsTracker == null && tileLifecycle != null)
            {
                _boundsTracker = new TerrainBoundsTracker(tileLifecycle);
            }

            yield return _rasterSource.Initialize();
            if (_rasterSource is TerrainSource terrainSource)
            {
                terrainSource.ExtractCpuElevationData = _settings.NeedsCpuElevation;
            }
            if (!_settings.ExtractCpuElevationData && _settings.NeedsCpuElevation)
            {
                var reason = !_settings.UseShaderTerrain
                    ? "UseShaderTerrain is off (CPU-elevation rendering needs the data)"
                    : "colliderOptions.addCollider is on (terrain collider needs the data)";
                Debug.LogWarning($"[Mapbox] TerrainLayerModuleSettings.ExtractCpuElevationData is unchecked, but {reason}. Extraction has been force-enabled; the checkbox has no effect in this configuration.");
            }
            _terrainStrategy.Initialize(_settings.ElevationLayerProperties);
            if(_settings.LoadBackgroundTextures)
            {
                _rasterSource?.DownloadAndCacheBaseTiles();
            }
        }

        public virtual void LoadTempTile(UnityMapTile unityTile)
        {
            if (IsZinSupportedRange(unityTile.CanonicalTileId.Z) == false)
            {
                unityTile.TerrainContainer.DisableTerrain();
                return;
            }

            var targetTileId = GetDataId(unityTile.CanonicalTileId);
            var parentTileId = targetTileId;
            for (int i = parentTileId.Z; i >= 2; i--)
            {
                parentTileId.MoveToParent();
                if (_rasterSource.GetInstantData(parentTileId, out var instantData) && IsTileReady(instantData))
                {
                    unityTile.TerrainContainer.SetTerrainData(instantData, _settings.UseShaderTerrain, TileContainerState.Temporary);
                    _terrainStrategy.RegisterTile(unityTile, !_settings.UseShaderTerrain);
                    return;
                }
            }
        }

        public virtual bool LoadInstant(UnityMapTile unityTile)
        {
            if (IsZinSupportedRange(unityTile.CanonicalTileId.Z) == false)
            {
                unityTile.TerrainContainer.DisableTerrain();
                return true;
            }

            var targetTileId = GetDataId(unityTile.CanonicalTileId);
            if (_rasterSource.GetInstantData(targetTileId, out var instantData) && IsTileReady(instantData))
            {
                unityTile.TerrainContainer.SetTerrainData(instantData, _settings.UseShaderTerrain, TileContainerState.Final);
                _terrainStrategy.RegisterTile(unityTile, !_settings.UseShaderTerrain);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Readiness gate used by <see cref="LoadInstant"/> and <see cref="LoadTempTile"/>.
        /// Shader-elevation rendering only needs the raster texture and can display a tile
        /// as soon as the texture exists. CPU-elevation rendering still needs the per-pixel
        /// <c>float[]</c> before displacing verts, so it must wait for extraction to finish.
        /// </summary>
        private bool IsTileReady(TerrainData data)
        {
            return _settings.UseShaderTerrain ? data.IsTextureReady : data.IsElevationDataReady;
        }

        public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
        {
            var isReady = true;
            _retainedTerrainTiles.Clear();
            foreach (var tileId in retainedTiles)
            {
                _retainedTerrainTiles.Add(GetDataId(tileId));
            }
            
            isReady = _rasterSource.RetainTiles(_retainedTerrainTiles);
            return isReady;
        }
        
        public void UpdatePositioning(IMapInformation mapInfo)
        {
            
        }
                
        public void OnDestroy()
        {
            _boundsTracker?.Dispose();
            _boundsTracker = null;
            _rasterSource.OnDestroy();
            _terrainStrategy?.OnDestroy();
        }
        
        //COROUTINE METHODS only used in initialization so far
        #region coroutine methods
        public virtual IEnumerator LoadTileData(CanonicalTileId tileId, Action<TerrainData> callback = null)
        {
            return _rasterSource.LoadTileCoroutine(tileId, callback);
        }

        public virtual IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
        {
            // Materialize before passing to the coroutine: the IEnumerable returned by
            // GetDataId is the shared _dataIdScratch set. If LoadTilesCoroutine consumes
            // lazily and another GetDataId call clears the scratch before it finishes,
            // the consumer would see the wrong contents.
            var materialized = new List<CanonicalTileId>(GetDataId(tiles));
            yield return _rasterSource.LoadTilesCoroutine(materialized);
        }

        public IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles)
        {
            // Same materialization rationale: callers iterate this lazily downstream and
            // would otherwise read a mutated _dataIdScratch after another GetDataId call.
            var materialized = new List<CanonicalTileId>(GetDataId(tiles));
            // Explicit lambda (not method-group): LoadTileData's optional callback
            // parameter makes the method-group conversion ambiguous between Select's
            // two overloads (Func<T,R> vs Func<T,int,R>).
            return materialized.Select(t => LoadTileData(t)).Where(x => x != null);
        }
        #endregion
        
        
        //PRIVATE METHODS
        private bool IsZinSupportedRange(int targetZ)
        {
            return _settings.RejectTilesOutsideZoom.x <= targetZ && _settings.RejectTilesOutsideZoom.y >= targetZ;
        }
        
        private CanonicalTileId GetDataId(CanonicalTileId tileId)
        {
            var maxZoom = _settings.DataSettings.ClampDataLevelToMax;
            var currentZ = tileId.Z;
            var targetZ = (int)Mathf.Max(currentZ - 2, _settings.RejectTilesOutsideZoom.x);
            if (targetZ >= maxZoom)
            {
                return tileId.ParentAt(maxZoom);
            }
            else
            {
                return tileId.ParentAt(targetZ);;
            }
        }
        
        
        //API
        
        /// <summary>
        /// This method will only search the memory cache for available data.
        /// </summary>
        /// <param name="tileId">TileId of the elevation data</param>
        /// <param name="x">Point in texture coordinate space, origin is bottom left.</param>
        /// <param name="y">Point in texture coordinate space, origin is bottom left.</param>
        /// <returns></returns>
        public float QueryElevation(CanonicalTileId tileId, float x, float y)
        {
            // Lossy by design: 0 is returned both for genuine sea level and for "no data", and the
            // caller cannot tell which. Kept for the many callers that have no useful response to
            // "unknown" anyway. Anything that would act differently should use TryQueryElevation.
            TryQueryElevation(tileId, x, y, out var elevation);
            return elevation;
        }

        /// <summary>
        /// Elevation at a point, and whether it is actually known.
        ///
        /// Only searches the memory cache, exactly as <see cref="QueryElevation"/> does - it walks
        /// up to 5 levels of ancestors looking for a loaded data tile. The difference is the return
        /// value: false means no data was found (not loaded yet, or CPU elevation extraction is
        /// switched off entirely), where QueryElevation reports that as 0 - indistinguishable from
        /// a real sea-level reading. That distinction matters because mesh generation is one-shot:
        /// SnapTerrainModifier writes whatever it gets straight into vertex positions and nothing
        /// re-snaps them when terrain arrives later, so a 0 that meant "not loaded yet" is baked in
        /// permanently and reads as a building sunk into a hillside.
        /// </summary>
        /// <param name="tileId">TileId of the elevation data</param>
        /// <param name="x">Point in texture coordinate space, origin is bottom left.</param>
        /// <param name="y">Point in texture coordinate space, origin is bottom left.</param>
        /// <param name="elevation">The elevation, or 0 when this returns false.</param>
        /// <returns>True if elevation data for this point was available.</returns>
        public bool TryQueryElevation(CanonicalTileId tileId, float x, float y, out float elevation)
        {
            elevation = 0f;

            // No CPU elevation will ever exist in this configuration, so say so immediately rather
            // than walking the ancestors and reporting a found-but-undecoded tile as a real answer.
            if (!_settings.NeedsCpuElevation)
            {
                if (!_elevationDisabledWarningLogged)
                {
                    _elevationDisabledWarningLogged = true;
                    Debug.LogWarning("[Mapbox] QueryElevation was called but TerrainLayerModuleSettings.ExtractCpuElevationData is disabled, so no CPU elevation data exists. Returning 0 for every query. Enable ExtractCpuElevationData if anything in your scene snaps to terrain (vector features, buildings, colliders, custom elevation lookups).");
                }

                return false;
            }

            var originalTileId = tileId;
            var targetTileId = tileId;
            for (int i = 0; i < 5; i++)
            {
                // IsElevationDataReady, not merely "a data tile exists". The texture arrives before
                // the elevation array: TerrainSource decodes elevation asynchronously, and
                // QueryHeightData answers a silent 0 while ElevationValues is still null. Returning
                // true there would report "sea level" for every tile whose decode is in flight -
                // precisely the case this method exists to tell apart from a real reading, and the
                // one a caller is most likely to hit, since it is the window right after a fetch.
                if (_rasterSource.GetInstantData(targetTileId, out var instantData) &&
                    instantData.IsElevationDataReady)
                {
                    elevation = instantData.QueryHeightData(originalTileId, x, y);
                    return true;
                }
                targetTileId.MoveToParent();
            }

            return false;
        }
        
        /// <summary>
        /// Maps a set of render tile ids to the distinct data tile ids they sample.
        /// </summary>
        /// <remarks>
        /// The returned enumerable is a reusable per-instance scratch set — the caller
        /// MUST finish iterating before invoking <see cref="GetDataId(IEnumerable{CanonicalTileId})"/>
        /// again (the next call clears the same set). Do not retain a reference past the
        /// loop body.
        /// </remarks>
        public IEnumerable<CanonicalTileId> GetDataId(IEnumerable<CanonicalTileId> tileIdList)
        {
            // With the data-zoom offset of 2, up to 16 render tile ids collapse to the
            // same data id. Reuse the scratch set to avoid allocating a HashSet plus
            // Where/Select/Distinct iterator chain per call.
            _dataIdScratch.Clear();
            foreach (var tileId in tileIdList)
            {
                if (!IsZinSupportedRange(tileId.Z))
                {
                    continue;
                }
                _dataIdScratch.Add(GetDataId(tileId));
            }
            return _dataIdScratch;
        }

        /// <summary>
        /// This method will cause tile requests if data isn't already available on memory cache. Use with caution.
        ///
        /// The callback is invoked exactly once on every path, including failure - callers park
        /// coroutines on it. Note 0 is reported both for genuine sea level and for a fetch that
        /// failed, 404'd or was cancelled; there is no caller today that could act on the
        /// difference, so it is not surfaced.
        /// </summary>
        /// <param name="latLng">Latitude longitude of the location you want to query</param>
        /// <param name="callback">Callback method returning the elevation in float</param>
        /// <returns></returns>
        public IEnumerator GetElevationData(LatitudeLongitude latLng, Action<float> callback = null)
        {
            var tileId = Conversions.LatitudeLongitudeToTileId(latLng, 14).Canonical;
            var dataFound = false;
            for (int i = 14; i >= 2; i--)
            {
                if (_rasterSource.GetInstantData(tileId, out var instantData))
                {
                    var tilePos = Conversions.LatitudeLongitudeToInTile01(latLng, instantData.TileId);
                    callback?.Invoke(instantData.QueryHeightData(tilePos));
                    dataFound = true;
                    break;
                }
                else
                {
                    tileId = tileId.GetParentTileId;
                }
            }

            if (!dataFound)
            {
                tileId = Conversions.LatitudeLongitudeToTileId(latLng, 14).Canonical;
                yield return LoadTileData(tileId, terrainData =>
                {
                    // A failed, 404'd or cancelled fetch arrives here as null. Dereferencing it threw
                    // inside this lambda, which meant the caller's callback was never invoked at all
                    // and anything awaiting an elevation answer waited for good.
                    if (terrainData == null)
                    {
                        callback?.Invoke(0f);
                        return;
                    }

                    var tilePos = Conversions.LatitudeLongitudeToInTile01(latLng, terrainData.TileId);
                    callback?.Invoke(terrainData.QueryHeightData(tilePos));
                });
            }
        }
    }
}
