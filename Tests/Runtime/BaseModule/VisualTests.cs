using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// <summary>
    /// Video-only visual regression tests. Each test records a short camera motion
    /// (zoom in, zoom out, flyover, 360 rotation) per city. No JSON stats — just video
    /// files for manual comparison between SDK versions.
    /// Requires Unity Recorder package (UNITY_RECORDER define).
    /// </summary>
    public class VisualTests
    {
        private const string SceneName = "DebugScene";

        private LoggingMapBehaviour _mapBehaviour;
        private BenchmarkOrchestrator _orchestrator;
        private GameObject _testRoot;

        // ───────────────────────────────────────────────────────────────────
        // City centers — used as the anchor point for all camera motions.
        // ───────────────────────────────────────────────────────────────────

        private static readonly Dictionary<string, LatitudeLongitude> CityCenters =
            new Dictionary<string, LatitudeLongitude>
            {
                ["Paris"] = new LatitudeLongitude(48.8584, 2.2945),
                ["London"] = new LatitudeLongitude(51.5007, -0.1246),
                ["Berlin"] = new LatitudeLongitude(52.5163, 13.3777),
            };

        private static IEnumerable CityNames
        {
            get
            {
                foreach (var key in CityCenters.Keys)
                    yield return new TestCaseData(key).Returns(null);
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // Setup / teardown
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads DebugScene, initializes map at the given position/zoom, creates
        /// orchestrator with video recorder attached. No stats collectors.
        /// </summary>
        private IEnumerator SetupWithVideo(string city, float startZoom, float startPitch = 45f, float startBearing = 0f)
        {
            LogAssert.ignoreFailingMessages = true;

            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            _mapBehaviour = Object.FindObjectOfType<LoggingMapBehaviour>();
            Assert.IsNotNull(_mapBehaviour, "DebugScene must contain a LoggingMapBehaviour");

            _mapBehaviour.InitializeOnStart = false;

            var existingOrchestrator = Object.FindObjectOfType<BenchmarkOrchestrator>();
            if (existingOrchestrator != null)
                Object.DestroyImmediate(existingOrchestrator);

            var center = CityCenters[city];

            _mapBehaviour.MapInformation.SetInformation(center, startZoom, startPitch, startBearing);

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

            _testRoot = new GameObject($"VisualTest_{city}");

            _orchestrator = _testRoot.AddComponent<BenchmarkOrchestrator>();
            _orchestrator.Map = _mapBehaviour;
            _orchestrator.AutoStart = false;
            _orchestrator.PreCacheData = false;
            _orchestrator.CacheOnlyMode = false;
            _orchestrator.ShowStatsOverlay = false;
            _orchestrator.PingPong = false;
            _orchestrator.PingPongCycles = 1;

            // Video recorder — produces the output file for visual comparison
            var recorder = _testRoot.AddComponent<BenchmarkVideoRecorder>();
            _orchestrator.VideoRecorder = recorder;
        }

        private IEnumerator Cleanup()
        {
            LogAssert.ignoreFailingMessages = false;

            if (_testRoot != null)
                Object.Destroy(_testRoot);

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
                        Debug.Log($"[VisualTests] All fetches completed after {elapsed:F1}s");
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

            Debug.LogWarning($"[VisualTests] Fetch drain timed out after {timeout}s " +
                $"(queue: {fetcher.FetchQueueCount}, active: {fetcher.ActiveFetchCount})");
        }

        // ───────────────────────────────────────────────────────────────────
        // Tests — parameterized by city
        // ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fixed position, zoom 10→17 with pitch tilting from overview to street level.
        /// Catches: tile loading at each zoom level, LOD transitions, texture pop-in.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator ZoomIn(string city)
        {
            yield return SetupWithVideo(city, startZoom: 10f, startPitch: 30f);

            var center = CityCenters[city];
            _orchestrator.Waypoints = new List<CameraWaypoint>
            {
                new CameraWaypoint(center.Latitude, center.Longitude, 10f, 30f, 0f),
                new CameraWaypoint(center.Latitude, center.Longitude, 17f, 60f, 0f),
            };
            _orchestrator.AnimationDuration = 8f;

            yield return _orchestrator.RunBenchmark();
            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase);

            yield return Cleanup();
        }

        /// <summary>
        /// Fixed position, zoom 17→10 with pitch tilting from street level to overview.
        /// Catches: tile disposal, parent tile fallback, memory cleanup visuals.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator ZoomOut(string city)
        {
            yield return SetupWithVideo(city, startZoom: 17f, startPitch: 60f);

            var center = CityCenters[city];
            _orchestrator.Waypoints = new List<CameraWaypoint>
            {
                new CameraWaypoint(center.Latitude, center.Longitude, 17f, 60f, 0f),
                new CameraWaypoint(center.Latitude, center.Longitude, 10f, 30f, 0f),
            };
            _orchestrator.AnimationDuration = 8f;

            yield return _orchestrator.RunBenchmark();
            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase);

            yield return Cleanup();
        }

        /// <summary>
        /// ~1.5km straight line at zoom 16.5, steep pitch (65°), looking east.
        /// Catches: tile streaming while moving, building pop-in, seams between tiles.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator StraightFlyover(string city)
        {
            var center = CityCenters[city];

            // Start offset ~0.75km west of center
            yield return SetupWithVideo(city, startZoom: 16.5f, startPitch: 65f, startBearing: 90f);

            _orchestrator.Waypoints = new List<CameraWaypoint>
            {
                new CameraWaypoint(center.Latitude, center.Longitude - 0.009, 16.5f, 65f, 90f),
                new CameraWaypoint(center.Latitude, center.Longitude + 0.009, 16.5f, 65f, 90f),
            };
            _orchestrator.AnimationDuration = 10f;

            yield return _orchestrator.RunBenchmark();
            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase);

            yield return Cleanup();
        }

        /// <summary>
        /// Fixed position at zoom 16, full 360° bearing rotation.
        /// Catches: all-angle rendering, shadow direction, building face consistency.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(CityNames))]
        public IEnumerator Rotation360(string city)
        {
            yield return SetupWithVideo(city, startZoom: 16f, startPitch: 50f);

            var center = CityCenters[city];
            _orchestrator.Waypoints = new List<CameraWaypoint>
            {
                new CameraWaypoint(center.Latitude, center.Longitude, 16f, 50f, 0f),
                new CameraWaypoint(center.Latitude, center.Longitude, 16f, 50f, 360f),
            };
            _orchestrator.AnimationDuration = 10f;

            yield return _orchestrator.RunBenchmark();
            Assert.AreEqual(BenchmarkOrchestrator.Phase.Complete, _orchestrator.CurrentPhase);

            yield return Cleanup();
        }
    }
}
