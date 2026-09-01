using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.MapDebug.Scripts.Benchmark;
using Mapbox.MapDebug.Scripts.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Runtime.BaseModule
{
    /// <summary>
    /// Backlog 21e. Repeats an identical multi-city lap (load, travel, zoom, unload) several times
    /// in one continuous run and compares the last lap against the first, looking for state that
    /// should return to roughly where it started but instead keeps climbing - a cache, a list, a
    /// pool, a task queue, or the scene's own GameObject count.
    ///
    /// <para><b>Why laps, not one long journey.</b> A single 10-15 minute tour through ever-different
    /// terrain confounds "is this leaking" with "this particular place is just heavier." Running the
    /// exact same workload N times isolates drift: whatever changes between an early lap and a late
    /// one is attributable to repetition, not to where the camera happens to be.</para>
    ///
    /// <para><b>Records video, like <see cref="VisualTests"/></b> - one clip per lap rather than one
    /// unbroken file, since <see cref="BenchmarkOrchestrator.RunBenchmark"/> starts a fresh recording
    /// per call. Several shorter clips in sequence are no harder to review than one long one.</para>
    ///
    /// <para><b>First run this at 2 laps, not <see cref="LapCount"/>.</b> Nothing about the thresholds
    /// below has been validated against a real run yet - they are a first cut, not a calibrated gate.
    /// Read the logged per-lap table before trusting the assertions.</para>
    ///
    /// <para><b><see cref="Warm"/> and <see cref="Cold"/> are two tests, not a flag, but they cannot
    /// mean "every lap is warm" versus "every lap is cold": <c>Setup()</c> builds one cache instance
    /// and shares it across every lap in a run, deliberately - that sharing across a long-lived map
    /// and scene is the whole point of a soak test, and rebuilding the map each lap to force it cold
    /// would just make this N independent short cold-cache runs instead. So only lap 1 can ever be
    /// genuinely cold; by lap 2 the tour's tiles are already resolved either way. What the two tests
    /// actually control is whether that one cold lap happens before the measured comparison
    /// (<see cref="Warm"/>) or as its first entry, visible in the table
    /// (<see cref="Cold"/>) - and <see cref="Cold"/>'s growth check correctly excludes lap 1 from the
    /// comparison rather than pretending it's representative.</b> <see cref="Warm"/> cannot see a leak
    /// specific to the fetch/decode path, since <c>GetInstantData</c> resolves every tile before
    /// <c>DataFetchingManager</c>'s queue or <c>PbfSource</c>/<c>ImageSource</c>'s <c>_waitingList</c>
    /// are ever touched once warm; <see cref="Cold"/> exercises that path for its own cold lap 1, at
    /// the cost of one lap's numbers being a known outlier rather than useful trend data.</para>
    /// </summary>
    public class SoakTest
    {
        private const string SceneName = "DebugScene";

        /// <summary>~110s per lap. 6 laps is a little over 11 minutes - inside the 10-15 minute ask.</summary>
        private const int LapCount = 6;
        private const float LapSeconds = 110f;

        /// <summary>
        /// SystemStatsCollector samples every frame and its buffer is sized up front; the default
        /// 36,000-frame cap is minutes' worth at typical Editor rates, not the ~11 total minutes this
        /// test runs. Sized generously (frame data is ~64 bytes) rather than tuned precisely, since
        /// silently truncating the very data this test exists to look at is the worse failure.
        /// </summary>
        private const int SystemStatsMaxFrames = 500_000;

        /// <summary>
        /// One lap: each city gets an overview, a street-level dive, a flyover, and a return to
        /// overview, before travelling on. The long jump between cities' final and next city's first
        /// waypoint is the "unload everything, load somewhere new" transition this test is watching.
        /// </summary>
        private static readonly Dictionary<string, LatitudeLongitude> CityCenters =
            new Dictionary<string, LatitudeLongitude>
            {
                ["Paris"] = new LatitudeLongitude(48.8584, 2.2945),
                ["London"] = new LatitudeLongitude(51.5007, -0.1246),
                ["Berlin"] = new LatitudeLongitude(52.5163, 13.3777),
            };

        private LoggingMapBehaviour _mapBehaviour;
        private BenchmarkOrchestrator _orchestrator;
        private GameObject _testRoot;

        /// <summary>
        /// One untimed pass through the tour warms the shared cache before lap 1, so every measured
        /// lap - lap 1 included - is equally warm and directly comparable. Cannot see a leak specific
        /// to the fetch/decode path; see the class doc for why.
        /// </summary>
        [UnityTest]
        public IEnumerator Warm()
        {
            yield return Setup();

            Debug.Log("[SoakTest:Warm] Warm-up pass: touring once, unmeasured, before any lap is measured.");
            yield return _orchestrator.RunBenchmark();
            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase,
                "The warm-up pass did not reach Complete.");

            yield return RunMeasuredLaps("Warm", excludeFirstLapFromComparison: false);
        }

        /// <summary>
        /// No warm-up: lap 1 reaches the network for everything and is a genuine cold lap, exercising
        /// DataFetchingManager's queue and PbfSource/ImageSource's _waitingList the way <see cref="Warm"/>
        /// never does. From lap 2 on, the shared cache is already warm from lap 1 - same as every lap
        /// in <see cref="Warm"/> - so the growth check compares lap 2 against the last lap, not lap 1,
        /// which would be comparing a cold lap against warm ones and calling the difference "drift".
        /// </summary>
        [UnityTest]
        public IEnumerator Cold()
        {
            yield return Setup();

            yield return RunMeasuredLaps("Cold", excludeFirstLapFromComparison: true);
        }

        private IEnumerator RunMeasuredLaps(string label, bool excludeFirstLapFromComparison)
        {
            var laps = new List<JObject>();
            var gameObjectCountsPerLap = new List<int>();

            for (var lap = 0; lap < LapCount; lap++)
            {
                Debug.Log($"[SoakTest:{label}] Lap {lap + 1}/{LapCount} starting.");

                yield return _orchestrator.RunBenchmark();
                Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase,
                    $"Lap {lap + 1} did not reach Complete.");

                laps.Add(BuildLapSnapshot(lap));
                gameObjectCountsPerLap.Add(Object.FindObjectsOfType<GameObject>().Length);
            }

            LogLapTable(label, laps, gameObjectCountsPerLap);

            var baselineIndex = excludeFirstLapFromComparison ? 1 : 0;
            if (excludeFirstLapFromComparison)
            {
                Debug.Log($"[SoakTest:{label}] Comparing lap 2 against the last lap; lap 1 was the "
                    + "cold outlier and is excluded from the growth check, not averaged into it.");
            }
            AssertNoUnboundedGrowth(label, laps, gameObjectCountsPerLap, baselineIndex);

            yield return Cleanup();
        }

        private IEnumerator Setup()
        {
            LogAssert.ignoreFailingMessages = true;

            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            _mapBehaviour = Object.FindObjectOfType<LoggingMapBehaviour>();
            Assert.IsNotNull(_mapBehaviour, "DebugScene must contain a LoggingMapBehaviour");
            _mapBehaviour.InitializeOnStart = false;

            var existingOrchestrator = Object.FindObjectOfType<BenchmarkOrchestrator>();
            if (existingOrchestrator != null)
                Object.DestroyImmediate(existingOrchestrator);

            var paris = CityCenters["Paris"];
            _mapBehaviour.MapInformation.SetInformation(paris, 11f, 30f, 0f);

            var mapReady = false;
            _mapBehaviour.Initialized += map => { map.LoadViewCompleted += () => mapReady = true; };
            yield return _mapBehaviour.Initialize();

            var elapsed = 0f;
            const float timeout = 30f;
            while (!mapReady && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(mapReady, "Map failed to initialize within timeout.");

            _testRoot = new GameObject("SoakTest");

            _orchestrator = _testRoot.AddComponent<BenchmarkOrchestrator>();
            _orchestrator.Map = _mapBehaviour;
            _orchestrator.AutoStart = false;
            _orchestrator.ShowStatsOverlay = false;
            _orchestrator.PingPong = false;
            _orchestrator.PingPongCycles = 1;
            _orchestrator.AnimationDuration = LapSeconds;

            // Real network throughout, matching what a 10-15 minute session of actual use looks
            // like - a soak test that only replays a pre-warmed cache would miss exactly the
            // fetch/decode/mesh-gen churn it exists to watch.
            _orchestrator.PreCacheData = false;
            _orchestrator.CacheOnlyMode = false;

            _orchestrator.Waypoints = BuildLapWaypoints();

            _orchestrator.VideoRecorder = _testRoot.AddComponent<BenchmarkVideoRecorder>();

            _orchestrator.NetworkStats = _testRoot.AddComponent<NetworkStatsCollector>();
            _orchestrator.CacheStats = _testRoot.AddComponent<CacheStatsCollector>();
            _orchestrator.TaskStats = _testRoot.AddComponent<TaskStatsCollector>();
            _orchestrator.TileStats = _testRoot.AddComponent<TileStatsCollector>();
            _orchestrator.SystemStats = _testRoot.AddComponent<SystemStatsCollector>();
            _orchestrator.SystemStats.MaxFrames = SystemStatsMaxFrames;
        }

        private static List<CameraWaypoint> BuildLapWaypoints()
        {
            var points = new List<CameraWaypoint>();
            foreach (var pair in CityCenters)
            {
                var c = pair.Value;
                points.Add(new CameraWaypoint(c.Latitude, c.Longitude, 11f, 30f, 0f));
                points.Add(new CameraWaypoint(c.Latitude, c.Longitude, 17f, 60f, 0f));
                points.Add(new CameraWaypoint(
                    c.Latitude, c.Longitude - 0.006, 16.5f, 65f, 90f));
                points.Add(new CameraWaypoint(
                    c.Latitude, c.Longitude + 0.006, 16.5f, 65f, 90f));
                points.Add(new CameraWaypoint(c.Latitude, c.Longitude, 11f, 30f, 0f));
            }
            // Close the loop so every lap starts and ends at the same place.
            var first = points[0];
            points.Add(first);
            return points;
        }

        private JObject BuildLapSnapshot(int lapIndex)
        {
            return new JObject
            {
                ["lap"] = lapIndex + 1,
                ["system"] = _orchestrator.SystemStats.Export(),
                ["tiles"] = _orchestrator.TileStats.Export(),
                ["cache"] = _orchestrator.CacheStats.Export(),
                ["tasks"] = _orchestrator.TaskStats.Export(),
            };
        }

        private static void LogLapTable(List<JObject> laps, List<int> gameObjectCounts)
        {
            var rows = new List<string>
            {
                "lap  peakHeapMB  allocMB  avgTiles  maxTiles  gcCollections  gameObjects"
            };
            for (var i = 0; i < laps.Count; i++)
            {
                var s = laps[i]["system"];
                var t = laps[i]["tiles"];
                rows.Add(
                    $"{i + 1,3}  {(float)s["peakManagedHeapMB"],10:F1}  {(float)s["managedAllocatedMB"],7:F1}  " +
                    $"{(float)t["avgTileCount"],8:F1}  {(int)t["maxTileCount"],8}  " +
                    $"{(int)s["gcCollections"],13}  {gameObjectCounts[i],11}");
            }
            Debug.Log("[SoakTest] Per-lap trend:\n" + string.Join("\n", rows));
        }

        /// <summary>
        /// First-cut thresholds, not a calibrated gate - see the class doc. Compares the last lap
        /// against a chosen baseline lap rather than lap-to-lap, since a single noisy lap (one slow
        /// network request, one GC pause landing badly) shouldn't fail the run; a sustained difference
        /// between the baseline and the last lap is much harder to explain away as noise.
        /// </summary>
        private static void AssertNoUnboundedGrowth(string label, List<JObject> laps, List<int> gameObjectCounts,
            int baselineIndex)
        {
            var baseline = laps[baselineIndex];
            var last = laps[laps.Count - 1];
            var baselineLapNumber = baselineIndex + 1;

            AssertBoundedGrowth(label, "peakManagedHeapMB", baselineLapNumber,
                (float)baseline["system"]["peakManagedHeapMB"], (float)last["system"]["peakManagedHeapMB"],
                maxGrowthFraction: 0.5f);

            AssertBoundedGrowth(label, "avgTileCount", baselineLapNumber,
                (float)baseline["tiles"]["avgTileCount"], (float)last["tiles"]["avgTileCount"],
                maxGrowthFraction: 0.5f);

            AssertBoundedGrowth(label, "gameObjectCount", baselineLapNumber,
                gameObjectCounts[baselineIndex], gameObjectCounts[gameObjectCounts.Count - 1],
                maxGrowthFraction: 0.5f);
        }

        private static void AssertBoundedGrowth(string label, string metric, int baselineLapNumber,
            float baselineValue, float lastValue, float maxGrowthFraction)
        {
            if (baselineValue <= 0f)
            {
                Debug.LogWarning(
                    $"[SoakTest:{label}] {metric} started at {baselineValue}; skipping the growth check for it.");
                return;
            }

            var growth = (lastValue - baselineValue) / baselineValue;
            Assert.LessOrEqual(growth, maxGrowthFraction,
                $"{metric} grew {growth:P0} from lap {baselineLapNumber} ({baselineValue:F1}) to the last lap "
                + $"({lastValue:F1}), over {maxGrowthFraction:P0} for an identical repeated workload. Read the "
                + "full per-lap table above before treating this as confirmed - it may mean the threshold needs "
                + "calibrating against a real run, not that something is actually unbounded.");
        }

        private IEnumerator Cleanup()
        {
            LogAssert.ignoreFailingMessages = false;
            if (_testRoot != null) Object.Destroy(_testRoot);
            yield return null;
        }
    }
}
