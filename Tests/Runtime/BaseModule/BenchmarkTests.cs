using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.MapDebug.Scripts.Benchmark;
using Mapbox.MapDebug.Scripts.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Runtime.BaseModule
{
    public class BenchmarkTests
    {
        private const string SceneName = "DebugScene";

        private LoggingMapBehaviour _mapBehaviour;
        private BenchmarkOrchestrator _orchestrator;
        private GameObject _benchmarkRoot;

        // ───────────────────────────────────────────────────────────────────
        // City routes — each entry defines a start position and waypoints.
        // To add a new city: add an entry here and it will run all tests.
        // ───────────────────────────────────────────────────────────────────

        private static readonly Dictionary<string, CityRoute> Routes = new Dictionary<string, CityRoute>
        {
            ["Paris"] = new CityRoute
            {
                Start = new LatitudeLongitude(48.8584, 2.2945),
                StartZoom = 13f,
                Waypoints = new List<CameraWaypoint>
                {
                    // Eiffel Tower — overview
                    new CameraWaypoint(48.8584, 2.2945, 13f, 45f, 0f),
                    // Arc de Triomphe — zoom in, flyby over rooftops
                    new CameraWaypoint(48.8738, 2.2950, 16.5f, 60f, 30f),
                    // Louvre — street level, rotated south
                    new CameraWaypoint(48.8606, 2.3376, 17f, 65f, 180f),
                    // Back to Eiffel Tower — zoom out
                    new CameraWaypoint(48.8584, 2.2945, 13f, 45f, 0f),
                }
            },
            ["London"] = new CityRoute
            {
                Start = new LatitudeLongitude(51.5007, -0.1246),
                StartZoom = 13f,
                Waypoints = new List<CameraWaypoint>
                {
                    // Big Ben / Parliament — overview
                    new CameraWaypoint(51.5007, -0.1246, 13f, 45f, 0f),
                    // Tower Bridge — zoom in, look east along the Thames
                    new CameraWaypoint(51.5055, -0.0754, 16.5f, 60f, 90f),
                    // The Shard — street level flyby
                    new CameraWaypoint(51.5045, -0.0865, 17f, 65f, 200f),
                    // Back to Parliament — zoom out
                    new CameraWaypoint(51.5007, -0.1246, 13f, 45f, 0f),
                }
            },
            ["Berlin"] = new CityRoute
            {
                Start = new LatitudeLongitude(52.5163, 13.3777),
                StartZoom = 13f,
                Waypoints = new List<CameraWaypoint>
                {
                    // Brandenburg Gate — overview
                    new CameraWaypoint(52.5163, 13.3777, 13f, 45f, 0f),
                    // Alexanderplatz / TV Tower — zoom in, look north-east
                    new CameraWaypoint(52.5219, 13.4132, 16.5f, 60f, 45f),
                    // Museum Island — street level pass
                    new CameraWaypoint(52.5169, 13.4019, 17f, 65f, 240f),
                    // Back to Brandenburg Gate — zoom out
                    new CameraWaypoint(52.5163, 13.3777, 13f, 45f, 0f),
                }
            },
        };

        private struct CityRoute
        {
            public LatitudeLongitude Start;
            public float StartZoom;
            public List<CameraWaypoint> Waypoints;
        }

        private static IEnumerable CityNames
        {
            get
            {
                foreach (var key in Routes.Keys)
                    yield return new TestCaseData(key).Returns(null);
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // Setup / teardown — driven by city name parameter
        // ───────────────────────────────────────────────────────────────────

        private IEnumerator SetupForCity(string city)
        {
            LogAssert.ignoreFailingMessages = true;

            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            _mapBehaviour = Object.FindObjectOfType<LoggingMapBehaviour>();
            Assert.IsNotNull(_mapBehaviour, "DebugScene must contain a LoggingMapBehaviour");

            _mapBehaviour.InitializeOnStart = false;

            var existingOrchestrator = Object.FindObjectOfType<BenchmarkOrchestrator>();
            if (existingOrchestrator != null)
                Object.DestroyImmediate(existingOrchestrator);

            var route = Routes[city];

            _mapBehaviour.MapInformation.SetInformation(
                route.Start, route.StartZoom, 45f, 0f);

            bool mapReady = false;
            _mapBehaviour.Initialized += map =>
            {
                map.LoadViewCompleted += () => mapReady = true;
            };
            yield return _mapBehaviour.Initialize();

            float timeout = 30f;
            float elapsed = 0f;
            while (!mapReady && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(mapReady, $"Map failed to initialize within timeout for {city}");

            yield return WaitForFetchesToComplete(60f);

            _benchmarkRoot = new GameObject($"BenchmarkTest_{city}");

            _orchestrator = _benchmarkRoot.AddComponent<BenchmarkOrchestrator>();
            _orchestrator.Map = _mapBehaviour;
            _orchestrator.AutoStart = false;
            _orchestrator.PreCacheData = false;
            _orchestrator.CacheOnlyMode = false;
            _orchestrator.ShowStatsOverlay = false;

            _orchestrator.Waypoints = route.Waypoints;
            _orchestrator.AnimationDuration = 30f;
            _orchestrator.PingPong = false;
            _orchestrator.PingPongCycles = 1;

            _orchestrator.NetworkStats = _benchmarkRoot.AddComponent<NetworkStatsCollector>();
            _orchestrator.CacheStats = _benchmarkRoot.AddComponent<CacheStatsCollector>();
            _orchestrator.TaskStats = _benchmarkRoot.AddComponent<TaskStatsCollector>();
            _orchestrator.TileStats = _benchmarkRoot.AddComponent<TileStatsCollector>();
            _orchestrator.SystemStats = _benchmarkRoot.AddComponent<SystemStatsCollector>();
        }

        private IEnumerator Cleanup()
        {
            LogAssert.ignoreFailingMessages = false;

            if (_benchmarkRoot != null)
                Object.Destroy(_benchmarkRoot);

            yield return null;
        }

        private IEnumerator WaitForFetchesToComplete(float timeout)
        {
            var fetcher = _mapBehaviour.DataFetchingManager as LoggingDataFetchingManager;
            if (fetcher == null)
                yield break;

            yield return new WaitForSeconds(1f);

            float elapsed = 0f;
            float idleTime = 0f;
            const float requiredIdleTime = 2f;

            while (elapsed < timeout)
            {
                if (fetcher.FetchQueueCount == 0 && fetcher.ActiveFetchCount == 0)
                {
                    idleTime += Time.deltaTime;
                    if (idleTime >= requiredIdleTime)
                    {
                        Debug.Log($"[BenchmarkTests] All fetches completed after {elapsed:F1}s");
                        yield break;
                    }
                }
                else
                {
                    idleTime = 0f;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning($"[BenchmarkTests] Fetch drain timed out after {timeout}s " +
                $"(queue: {fetcher.FetchQueueCount}, active: {fetcher.ActiveFetchCount})");
        }

        // ───────────────────────────────────────────────────────────────────
        // Tests — parameterized by city name
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs the full benchmark animation and validates the complete stats pipeline.
        /// Checks that the orchestrator reaches Phase.Complete and that all 5 collectors
        /// (System, Tile, Network, Cache, Task) produce valid JSON with expected keys and
        /// non-zero values. Also verifies the camera loop returns near its start position.
        /// This is the main integration test — if this passes, the benchmark system works end-to-end.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator BenchmarkFullRun(string city)
        {
            yield return SetupForCity(city);

            yield return _orchestrator.RunBenchmark();

            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase);

            // --- System stats: frames were recorded, duration is positive ---
            var systemJson = _orchestrator.SystemStats.Export();
            Assert.IsTrue(systemJson.ContainsKey("totalFrames"), "Missing totalFrames");
            Assert.IsTrue((int)systemJson["totalFrames"] > 0, "totalFrames should be > 0");
            Assert.IsTrue(systemJson.ContainsKey("fps"), "Missing fps");
            Assert.IsTrue(systemJson.ContainsKey("peakMemoryMB"), "Missing peakMemoryMB");
            float durationSeconds = (float)systemJson["durationSeconds"];
            Assert.IsTrue(durationSeconds > 0, $"Expected duration > 0, got {durationSeconds}");

            // --- Tile stats: tile cover was tracked ---
            var tileJson = _orchestrator.TileStats.Export();
            Assert.IsTrue(tileJson.ContainsKey("avgTileCount"), "Missing avgTileCount");
            Assert.IsTrue(tileJson.ContainsKey("totalTileChanges"), "Missing totalTileChanges");

            // --- Network stats: requests were tracked ---
            var networkJson = _orchestrator.NetworkStats.Export();
            Assert.IsTrue(networkJson.ContainsKey("requestsFired"), "Missing requestsFired");
            Assert.IsTrue(networkJson.ContainsKey("latencyMs"), "Missing latencyMs");

            // --- Cache stats: cache layer was tracked ---
            var cacheJson = _orchestrator.CacheStats.Export();
            Assert.IsTrue(cacheJson.ContainsKey("totalRequests"), "Missing totalRequests");
            Assert.IsTrue(cacheJson.ContainsKey("layerBreakdown"), "Missing layerBreakdown");

            // --- Task stats: mesh generation was tracked ---
            var taskJson = _orchestrator.TaskStats.Export();
            Assert.IsTrue(taskJson.ContainsKey("tasksCreated"), "Missing tasksCreated");
            Assert.IsTrue(taskJson.ContainsKey("meshGenTimeMs"), "Missing meshGenTimeMs");

            // --- Camera loop: route ends back near start (first == last waypoint) ---
            var endLat = _mapBehaviour.MapInformation.LatitudeLongitude.Latitude;
            var endLng = _mapBehaviour.MapInformation.LatitudeLongitude.Longitude;
            var firstWp = _orchestrator.Waypoints[0];
            Assert.AreEqual(firstWp.Lat, endLat, 0.01,
                $"Lat should return near start. Expected: {firstWp.Lat}, Got: {endLat}");
            Assert.AreEqual(firstWp.Lng, endLng, 0.01,
                $"Lng should return near start. Expected: {firstWp.Lng}, Got: {endLng}");

            yield return Cleanup();
        }

        /// <summary>
        /// Verifies that CacheOnlyFileSource.BlockNetwork prevents any network requests
        /// from succeeding during a benchmark run. Fetches are still attempted (requestsFired > 0)
        /// but the CacheOnlySource intercepts them and returns cancelled responses, so
        /// requestsSucceeded must be 0. This is the mechanism used during cache-only recording
        /// to guarantee deterministic replay from pre-cached data.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator CacheOnlyModeBlocksNetwork(string city)
        {
            yield return SetupForCity(city);

            var loggingFetcher = _mapBehaviour.DataFetchingManager as LoggingDataFetchingManager;
            Assert.IsNotNull(loggingFetcher, "Expected LoggingDataFetchingManager");
            Assert.IsNotNull(loggingFetcher.CacheOnlySource, "Expected CacheOnlySource");

            loggingFetcher.CacheOnlySource.BlockNetwork = true;

            yield return _orchestrator.RunBenchmark();

            var networkJson = _orchestrator.NetworkStats.Export();
            int requestsSucceeded = (int)networkJson["requestsSucceeded"];
            Assert.AreEqual(0, requestsSucceeded,
                $"Expected 0 successful network requests in cache-only mode, got {requestsSucceeded}");

            loggingFetcher.CacheOnlySource.BlockNetwork = false;

            yield return Cleanup();
        }

        /// <summary>
        /// Records an allocation baseline that a later run can be compared against.
        ///
        /// <see cref="BenchmarkFullRun"/> re-fetches over the network, so its allocations and
        /// timings carry network variance and two runs of it do not compare. This one pre-caches the
        /// whole route first and then blocks the network for the recorded pass, so what gets measured
        /// is decode, mesh generation and rendering out of cache - which is what the mesh-modifier
        /// rework is meant to move.
        ///
        /// The orchestrator's own PreCacheData path is deliberately not used:
        /// <c>ComputeTilesForPath</c> interpolates a straight line between two points at a single
        /// zoom, which cannot cover a multi-waypoint route that changes zoom on every leg. It also
        /// reads the orchestrator's scalar Start/End fields, which the waypoint routes never set - so
        /// enabling it here would pre-cache the default coordinates and then block the network over
        /// an empty cache. This walks the legs itself instead.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator AllocationBaseline(string city)
        {
            yield return SetupForCity(city);

            var waypoints = _orchestrator.Waypoints;
            var preCacher = _benchmarkRoot.AddComponent<BenchmarkPreCacher>();
            _orchestrator.PreCacher = preCacher;

            // _tilesToCache resets on every ComputeTilesForPath call, so each leg has to be computed
            // and fetched before the next one starts. The tile cache accumulates across them.
            float legDuration = _orchestrator.AnimationDuration / Mathf.Max(1, waypoints.Count - 1);
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                var a = waypoints[i];
                var b = waypoints[i + 1];

                // A waypoint zoom of 0 means "inherit", so fall back to where the map currently is.
                float legZoom = Mathf.Max(a.Zoom, b.Zoom);
                if (legZoom <= 0)
                    legZoom = _mapBehaviour.MapInformation.Zoom;

                preCacher.ComputeTilesForPath(
                    new LatitudeLongitude(a.Lat, a.Lng),
                    new LatitudeLongitude(b.Lat, b.Lng),
                    legZoom,
                    legDuration);

                bool legDone = false;
                System.Action handler = () => legDone = true;
                preCacher.PreCacheCompleted += handler;
                yield return preCacher.PreCacheAllTiles(_mapBehaviour.MapboxMap);
                while (!legDone)
                    yield return null;
                preCacher.PreCacheCompleted -= handler;
            }

            yield return WaitForFetchesToComplete(120f);

            // Put the camera back where the route starts, so the recorded pass begins from the same
            // place the waypoint animation does.
            var first = waypoints[0];
            _mapBehaviour.MapboxMap.ChangeView(
                new LatitudeLongitude(first.Lat, first.Lng), first.Zoom, first.Pitch, first.Bearing);
            yield return WaitForFetchesToComplete(60f);

            var loggingFetcher = _mapBehaviour.DataFetchingManager as LoggingDataFetchingManager;
            Assert.IsNotNull(loggingFetcher?.CacheOnlySource,
                "Expected a CacheOnlySource to block the network with");
            loggingFetcher.CacheOnlySource.BlockNetwork = true;

            // Both off: this test has already pre-cached, and the orchestrator only honours
            // CacheOnlyMode inside its PreCacheData branch.
            _orchestrator.PreCacheData = false;
            _orchestrator.CacheOnlyMode = false;

            yield return _orchestrator.RunBenchmark();

            loggingFetcher.CacheOnlySource.BlockNetwork = false;

            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase);

            var networkJson = _orchestrator.NetworkStats.Export();
            Assert.AreEqual(0, (int)networkJson["requestsSucceeded"],
                "The recorded pass reached the network, so this run is not a comparable baseline. " +
                "Pre-caching probably did not cover the route.");

            var systemJson = _orchestrator.SystemStats.Export();
            Assert.IsTrue(systemJson.ContainsKey("managedAllocatedBytes"),
                "Missing managedAllocatedBytes - is SystemStatsCollector up to date?");

            long allocatedBytes = (long)systemJson["managedAllocatedBytes"];
            Assert.IsTrue(allocatedBytes > 0,
                "No allocations were recorded. The counter comes from ProfilerRecorder, which "
                + "only reports in the Editor or a development build - check that before "
                + "concluding the run allocated nothing.");

            var tileJson = _orchestrator.TileStats.Export();
            int tileChanges = (int)tileJson["totalTileChanges"];
            int totalFrames = (int)systemJson["totalFrames"];

            double allocatedMB = allocatedBytes / (1024.0 * 1024.0);
            double kbPerTileChange = tileChanges > 0 ? allocatedBytes / 1024.0 / tileChanges : 0;
            double kbPerFrame = totalFrames > 0 ? allocatedBytes / 1024.0 / totalFrames : 0;

            // One greppable line per city - this is the baseline to record before the rework.
            Debug.Log(
                $"[AllocationBaseline] {city}: {allocatedMB:F1} MB allocated over {tileChanges} tile " +
                $"changes and {totalFrames} frames " +
                $"({kbPerTileChange:F1} KB per tile change, {kbPerFrame:F1} KB per frame).");

            // The shape matters more than the total: a steady per-frame floor and a few enormous
            // spikes sum to the same number but call for completely different fixes.
            var perFrame = systemJson["allocPerFrame"];
            if (perFrame != null)
            {
                Debug.Log(
                    $"[AllocationBaseline] {city} per frame: median {(long)perFrame["medianBytes"] / 1024.0:F1} KB, "
                    + $"p95 {(long)perFrame["p95Bytes"] / 1024.0:F1} KB, "
                    + $"p99 {(long)perFrame["p99Bytes"] / 1024.0:F1} KB, "
                    + $"max {(long)perFrame["maxBytes"] / 1024.0:F1} KB; "
                    + $"{(int)perFrame["framesAllocatingNothing"]} frames allocated nothing, "
                    + $"{(int)perFrame["framesOver100KB"]} allocated over 100 KB.");
            }

            yield return Cleanup();
        }
    }
}
