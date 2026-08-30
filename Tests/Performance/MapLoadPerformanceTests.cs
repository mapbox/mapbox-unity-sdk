using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.ImageModule;
using Mapbox.ImageModule.Terrain;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.Tests;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using Mapbox.VectorModule;
using Mapbox.VectorModule.ComponentSystem;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer;
using Mapbox.VectorModule.MeshGeneration;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace Mapbox.PerformanceTests
{
    /// <summary>
    /// How long a map takes to go from "load requested" to "first view complete", per module setup.
    ///
    /// This is its own assembly so it can be run on its own: it is slow by nature, it reaches the
    /// network on its first pass, and it is meant to be run occasionally to see where overall
    /// performance stands rather than on every change.
    ///
    /// <para><b>What the number covers.</b> <c>MapboxMap.LoadMapViewCoroutine</c> fires
    /// LoadViewStarting, computes the tile cover, awaits every registered module's tile-cover
    /// coroutines for the whole cover, builds the visuals in LoadSnapshot, and only then fires
    /// LoadViewCompleted. So a sample includes decode, mesh generation and visual creation for every
    /// tile of every module - not a proxy for them.</para>
    ///
    /// <para><b>Warm on purpose.</b> Each setup loads once before sampling, which fills the sqlite
    /// and file caches. Samples then run against a fresh MemoryCache but those same warm disk
    /// caches, so what is measured is the SDK's own work rather than the network - the same protocol
    /// BenchmarkTests.AllocationBaseline uses. A cold number would be dominated by request latency
    /// and would not be comparable between runs.</para>
    ///
    /// <para><b>Fixed cover.</b> UnityFixedAreaTileProvider is used rather than the quadtree
    /// provider, so every setup loads the same tile set regardless of camera state.</para>
    /// </summary>
    [Category("MapLoadPerformance")]
    public class MapLoadPerformanceTests
    {
        private const int SampleCount = 5;
        private const string TestLatLng = "60.1664427,24.9318587";

        /// <summary>Module compositions, named as they appear in the results.</summary>
        private static readonly string[] Setups =
        {
            "Imagery",
            "Imagery+Terrain",
            "Imagery+VectorBuildings",
            "Imagery+ComponentBuildings",
            "Imagery+ComponentBuildings+Roads",
            "Imagery+Terrain+VectorBuildings"
        };

        // Shared across every sample so the disk caches stay warm; only the memory cache is rebuilt.
        private static MapboxContext _mapboxContext;
        private static UnityContext _unityContext;
        private static TaskManager _taskManager;
        private static MockSqliteCache _sqliteCache;
        private static MockFileCache _fileCache;
        private static DataFetchingManager _dataManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (_mapboxContext != null) yield break;

            _mapboxContext = new MapboxContext();
            yield return _mapboxContext.LoadConfigurationCoroutine(false);

            _unityContext = new UnityContext();
            yield return _unityContext.Initialize();

            _taskManager = new TaskManager();
            _taskManager.Initialize();
            _unityContext.TaskManager = _taskManager;

            _sqliteCache = new MockSqliteCache(_taskManager);
            _sqliteCache.ReadySqliteDatabase();
            _fileCache = new MockFileCache(_taskManager);

            _dataManager = new DataFetchingManager(_mapboxContext.GetAccessToken(), _mapboxContext.GetSkuToken);
        }

        // ValueSource rather than TestCaseSource: a TestCaseSource feeding a [UnityTest] has to
        // return TestCaseData marked .Returns(null), or NUnit rejects the IEnumerator with
        // "Method has non-void return value, but no result is expected".
        [UnityTest, Performance]
        public IEnumerator MapLoad([ValueSource(nameof(Setups))] string setup)
        {
            Runnable.EnableRunnableInEditor();

            // Unmeasured: fills the sqlite and file caches so the samples below are not timing the
            // network. Also pays any one-off cost of building this setup's visualizers.
            yield return LoadOnce(setup, null);

            var group = new SampleGroup($"MapLoad.{setup}", SampleUnit.Millisecond);
            for (var i = 0; i < SampleCount; i++)
            {
                yield return LoadOnce(setup, group);
            }
        }

        /// <summary>
        /// Builds a whole map for this setup and times only <c>LoadMapViewCoroutine</c>. The map is
        /// rebuilt per sample because a loaded visualizer already holds its tiles as active, so a
        /// second load of the same view would do nothing and report a time near zero.
        /// </summary>
        private IEnumerator LoadOnce(string setup, SampleGroup group)
        {
            var mapInfo = new MapInformation(TestLatLng);
            mapInfo.SetInformation(null, 16, 45, null, 1000);
            mapInfo.Initialize();

            // Fresh memory cache, shared disk caches: the samples read tiles the warm-up fetched.
            var cacheManager = new MapboxCacheManager(_unityContext, new MemoryCache(), _fileCache, _sqliteCache);
            var mapService = new MapUnityService(
                _unityContext, _mapboxContext, new UnityFixedAreaTileProvider(), cacheManager, _dataManager);

            var map = new MapboxMap(mapInfo, _unityContext, mapService);
            var visualizer = new MapboxMapVisualizer(mapInfo, _unityContext, new TileCreator(_unityContext));
            map.MapVisualizer = visualizer;

            AddModules(setup, mapInfo, mapService, visualizer);

            yield return map.Initialize();

            var loaded = false;
            var stopwatch = Stopwatch.StartNew();
            Runnable.Instance.StartCoroutine(map.LoadMapViewCoroutine(() => loaded = true));
            yield return TestWait.Until(() => loaded, $"{setup}: the map view to finish loading", 120f);
            stopwatch.Stop();

            if (group != null)
            {
                Measure.Custom(group, stopwatch.Elapsed.TotalMilliseconds);
            }

            map.OnDestroy();
        }

        private static void AddModules(string setup, IMapInformation mapInfo, MapUnityService mapService,
            MapboxMapVisualizer visualizer)
        {
            var imageryTileset = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxSatellite);
            visualizer.LayerModules.Add(new StaticApiLayerModule(
                mapService.GetStaticRasterSource(new ImageSourceSettings { TilesetId = imageryTileset.Id }),
                new StaticLayerModuleSettings()));

            if (setup.Contains("Terrain"))
            {
                var terrainTileset = MapboxDefaultElevation.GetParameters(ElevationSourceType.MapboxTerrain);
                var terrainSettings = new TerrainLayerModuleSettings
                {
                    DataSettings = new ImageSourceSettings { TilesetId = terrainTileset.Id, ClampDataLevelToMax = 14 }
                };
                visualizer.LayerModules.Add(new TerrainLayerModule(
                    mapService.GetTerrainRasterSource(terrainSettings.DataSettings), terrainSettings));
            }

            if (!setup.Contains("Buildings")) return;

            var vectorTileset = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8);
            var vectorSettings = new VectorSourceSettings { TilesetId = vectorTileset.Id, ClampDataLevelToMax = 15 };
            var vectorSource = mapService.GetVectorSource(vectorSettings);
            var moduleSettings = new VectorModuleSettings { DataSettings = vectorSettings };

            if (setup.Contains("VectorBuildings"))
            {
                visualizer.LayerModules.Add(new VectorLayerModule(
                    mapInfo, vectorSource, _unityContext, ClassicBuildingVisualizers(mapInfo), moduleSettings));
            }
            else
            {
                visualizer.LayerModules.Add(new MapboxComponentsModule(
                    mapInfo, vectorSource, _unityContext,
                    ComponentVisualizers(mapInfo, withRoads: setup.Contains("Roads")), moduleSettings));
            }
        }

        /// <summary>The classic modifier-stack path: polygon plus height, as a building layer uses.</summary>
        private static Dictionary<string, List<IVectorLayerVisualizer>> ClassicBuildingVisualizers(IMapInformation mapInfo)
        {
            var stack = new ModifierStack(new ModifierStackSettings { MergeObjects = true }, null);
            stack.MeshModifiers.Add(new PolygonMeshModifier(0));
            stack.MeshModifiers.Add(new HeightModifier(new GeometryExtrusionOptions()));

            var viz = new VectorLayerVisualizer("building", mapInfo, _unityContext, null);
            viz.AddModifierStack(new List<ModifierStack> { stack });

            return new Dictionary<string, List<IVectorLayerVisualizer>>
            {
                { "building", new List<IVectorLayerVisualizer> { viz } }
            };
        }

        private static Dictionary<string, List<IVectorLayerVisualizer>> ComponentVisualizers(
            IMapInformation mapInfo, bool withRoads)
        {
            var visualizers = new Dictionary<string, List<IVectorLayerVisualizer>>
            {
                {
                    "building",
                    new List<IVectorLayerVisualizer>
                        { new BuildingComponentVisualizer("building", mapInfo, _unityContext) }
                }
            };

            if (withRoads)
            {
                visualizers.Add("road",
                    new List<IVectorLayerVisualizer> { new RoadComponentVisualizer("road", mapInfo, _unityContext) });
            }

            return visualizers;
        }
    }
}
