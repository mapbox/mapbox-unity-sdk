using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.Tests;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using Mapbox.VectorModule;
using Mapbox.VectorModule.ComponentSystem;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace Mapbox.PerformanceTests
{
    /// <summary>
    /// What a fast pan costs, and how much of it the task queue's dispatch debounce avoids.
    ///
    /// <para>The debounce exists for exactly this case. Tiles enter and leave the cover faster than
    /// their work completes, and <c>VectorLayerModule.RetainTiles</c> and
    /// <c>UnitySource.CancelFetching</c> cancel the queued tasks of tiles that left - which
    /// <c>TaskManager</c> only acts on when it dequeues them. The delay before dispatch is therefore
    /// the window in which a cancellation can still land, and the comment on that cancel loop says it
    /// is what "prevents flickers from buildings appearing in temp tiles".</para>
    ///
    /// <para>Every other measurement in this assembly loads a static view, where nothing has left the
    /// cover yet and the debounce can only look like cost. This test is the counterweight: it reports
    /// how many tasks were cancelled before running versus how many ran, so a change to the delay
    /// policy can be judged on both sides rather than on load time alone.</para>
    ///
    /// <para><b>Reading the numbers.</b> CancelledBeforeRunning going down while Started goes up means
    /// work that used to be skipped is now being done for tiles already gone. That is the cost of
    /// shortening the delay, and it is paid during movement, not on load.</para>
    /// </summary>
    [Category("MapLoadPerformance")]
    public class FastPanPerformanceTests
    {
        private const string StartLatLng = "60.1664427,24.9318587";

        /// <summary>Degrees of longitude per frame. At zoom 16 this crosses tiles every few frames.</summary>
        private const double StepPerFrame = 0.0012;
        private const int PanFrames = 120;
        private const int SampleCount = 3;

        private static MapboxContext _mapboxContext;
        private static UnityContext _unityContext;
        private static TaskManager _taskManager;
        private static MockSqliteCache _sqliteCache;
        private static MockFileCache _fileCache;

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
        }

        [UnityTest, Performance]
        public IEnumerator FastPan()
        {
            Runnable.EnableRunnableInEditor();

            // Unmeasured: pans the whole path once so the sqlite and file caches hold every tile the
            // samples will touch. Without it this measures the network, and cancellation counts would
            // reflect request latency rather than the debounce.
            yield return PanOnce(null, null, null);

            var started = new SampleGroup("FastPan.TasksStarted", SampleUnit.Undefined);
            var cancelled = new SampleGroup("FastPan.CancelledBeforeRunning", SampleUnit.Undefined);
            var duration = new SampleGroup("FastPan.DurationMs", SampleUnit.Millisecond);

            for (var i = 0; i < SampleCount; i++)
            {
                yield return PanOnce(started, cancelled, duration);
            }
        }

        private IEnumerator PanOnce(SampleGroup startedGroup, SampleGroup cancelledGroup, SampleGroup durationGroup)
        {
            var mapInfo = new MapInformation(StartLatLng);
            mapInfo.SetInformation(null, 16, 45, null, 1000);
            mapInfo.Initialize();

            var dataManager = new DataFetchingManager(_mapboxContext.GetAccessToken(), _mapboxContext.GetSkuToken);
            var cacheManager = new MapboxCacheManager(_unityContext, new MemoryCache(), _fileCache, _sqliteCache);
            var mapService = new MapUnityService(
                _unityContext, _mapboxContext, new UnityFixedAreaTileProvider(), cacheManager, dataManager);

            var map = new MapboxMap(mapInfo, _unityContext, mapService);
            var visualizer = new MapboxMapVisualizer(mapInfo, _unityContext, new TileCreator(_unityContext));
            map.MapVisualizer = visualizer;

            var imageryTileset = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxSatellite);
            visualizer.LayerModules.Add(new StaticApiLayerModule(
                mapService.GetStaticRasterSource(new ImageSourceSettings { TilesetId = imageryTileset.Id }),
                new StaticLayerModuleSettings()));

            var vectorTileset = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8);
            var vectorSettings = new VectorSourceSettings { TilesetId = vectorTileset.Id, ClampDataLevelToMax = 15 };
            visualizer.LayerModules.Add(new MapboxComponentsModule(
                mapInfo, mapService.GetVectorSource(vectorSettings), _unityContext,
                new Dictionary<string, List<IVectorLayerVisualizer>>
                {
                    {
                        "building",
                        new List<IVectorLayerVisualizer>
                            { new BuildingComponentVisualizer("building", mapInfo, _unityContext) }
                    }
                },
                new VectorModuleSettings { DataSettings = vectorSettings }));

            yield return map.Initialize();

            var loaded = false;
            Runnable.Instance.StartCoroutine(map.LoadMapViewCoroutine(() => loaded = true));
            yield return TestWait.Until(() => loaded, "FastPan: the initial view to load", 120f);

            // Counters are attached only for the pan itself, so the initial load above does not
            // contribute - it has nothing to cancel, which is the whole point of measuring separately.
            var startedCount = 0;
            var cancelledCount = 0;
            void OnStarting(TaskWrapper _) => startedCount++;
            void OnCancelled(TaskWrapper _) => cancelledCount++;
            _taskManager.TaskStarting += OnStarting;
            _taskManager.TaskCancelled += OnCancelled;

            var stopwatch = Stopwatch.StartNew();
            var origin = Conversions.StringToLatLon(StartLatLng);
            for (var frame = 0; frame < PanFrames; frame++)
            {
                map.ChangeView(new LatitudeLongitude(origin.Latitude, origin.Longitude + StepPerFrame * frame));
                yield return null;
            }
            stopwatch.Stop();

            _taskManager.TaskStarting -= OnStarting;
            _taskManager.TaskCancelled -= OnCancelled;

            if (startedGroup != null)
            {
                Measure.Custom(startedGroup, startedCount);
                Measure.Custom(cancelledGroup, cancelledCount);
                Measure.Custom(durationGroup, stopwatch.Elapsed.TotalMilliseconds);

                Assert.Greater(startedCount + cancelledCount, 0,
                    "The pan queued no tasks at all, so nothing about the debounce can be concluded from it.");
            }

            map.OnDestroy();
        }
    }
}
