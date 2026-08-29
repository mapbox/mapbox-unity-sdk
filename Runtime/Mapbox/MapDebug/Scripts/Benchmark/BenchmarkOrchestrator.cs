using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.MapDebug.Scripts;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Camera waypoint for multi-point benchmark animation paths.
    /// </summary>
    [Serializable]
    public struct CameraWaypoint
    {
        [Tooltip("Latitude")] public double Lat;
        [Tooltip("Longitude")] public double Lng;
        [Tooltip("Zoom level (0 = use default)")] public float Zoom;
        [Tooltip("Camera pitch in degrees (0 = top-down, 60 = oblique)")] public float Pitch;
        [Tooltip("Camera bearing in degrees (0 = north)")] public float Bearing;

        public CameraWaypoint(double lat, double lng, float zoom = 0, float pitch = 0, float bearing = 0)
        {
            Lat = lat;
            Lng = lng;
            Zoom = zoom;
            Pitch = pitch;
            Bearing = bearing;
        }
    }

    /// <summary>
    /// Main orchestrator for the benchmark/visual test system.
    /// Drives the full pipeline: pre-cache → animate camera → record video → gather stats → dump JSON.
    /// Supports simple A-to-B animation or multi-waypoint paths with Catmull-Rom interpolation.
    /// </summary>
    public class BenchmarkOrchestrator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The instrumented map behaviour")]
        public LoggingMapBehaviour Map;

        [Header("Animation — Simple A-to-B (used when Waypoints is empty)")]
        [Tooltip("Start latitude")]
        public double StartLat = 40.7128;

        [Tooltip("Start longitude")]
        public double StartLng = -74.0060;

        [Tooltip("End latitude")]
        public double EndLat = 40.7580;

        [Tooltip("End longitude")]
        public double EndLng = -73.9855;

        [Tooltip("Zoom level at start")]
        public float StartZoom = 16f;

        [Tooltip("Zoom level at end")]
        public float EndZoom = 16f;

        [Tooltip("Camera pitch at start (0 = top-down, 60 = oblique)")]
        public float StartPitch = 45f;

        [Tooltip("Camera pitch at end")]
        public float EndPitch = 45f;

        [Tooltip("Camera bearing at start (0 = north)")]
        public float StartBearing;

        [Tooltip("Camera bearing at end")]
        public float EndBearing = 90f;

        [Header("Animation — Waypoint Path (overrides A-to-B when populated)")]
        [Tooltip("Camera waypoints for multi-point paths. Zoom/Pitch/Bearing of 0 inherits from previous waypoint.")]
        public List<CameraWaypoint> Waypoints = new List<CameraWaypoint>();

        [Tooltip("Duration of one full path traversal in seconds. 0 = no animation (manual stop mode).")]
        public float AnimationDuration = 10f;

        [Tooltip("Reverse direction after each sweep")]
        public bool PingPong = true;

        [Tooltip("Number of full ping-pong cycles (forward + backward = 1 cycle)")]
        public int PingPongCycles = 2;

        [Header("Stats Collectors")]
        [Tooltip("Enable network request stats")]
        public bool CollectNetworkStats = true;

        [Tooltip("Enable cache performance stats")]
        public bool CollectCacheStats = true;

        [Tooltip("Enable task/mesh gen stats")]
        public bool CollectTaskStats = true;

        [Tooltip("Enable tile state stats")]
        public bool CollectTileStats = true;

        [Tooltip("Enable FPS/memory/system stats")]
        public bool CollectSystemStats = true;

        [Header("Settings")]
        [Tooltip("Pre-cache tiles before recording for deterministic results")]
        public bool PreCacheData = true;

        [Tooltip("Block network during recording (requires pre-cache)")]
        public bool CacheOnlyMode = true;

        [Tooltip("Show stats overlay burned into video")]
        public bool ShowStatsOverlay;

        [Tooltip("Start benchmark automatically on play")]
        public bool AutoStart = true;

        [Tooltip("Key to stop recording manually")]
        public KeyCode StopKey = KeyCode.F12;

        [Header("Output")]
        [Tooltip("Output directory for video + JSON bundle. Defaults to persistentDataPath/benchmarks.")]
        public string OutputDirectory;

        [Header("Components (auto-created if empty)")]
        public BenchmarkPreCacher PreCacher;
        public BenchmarkVideoRecorder VideoRecorder;
        public NetworkStatsCollector NetworkStats;
        public CacheStatsCollector CacheStats;
        public TaskStatsCollector TaskStats;
        public TileStatsCollector TileStats;
        public SystemStatsCollector SystemStats;

        public enum Phase { Idle, PreCaching, Recording, Dumping, Complete }

        [Header("Status")]
        public Phase CurrentPhase = Phase.Idle;

        private List<IBenchmarkStats> _activeCollectors = new List<IBenchmarkStats>();
        private string _runId;
        private bool _isRunning;
        private bool _stopRequested;

        private void Start()
        {
            EnsureComponents();

            if (AutoStart)
            {
                Map.Initialized += map =>
                {
                    map.LoadViewCompleted += () => StartCoroutine(RunBenchmark());
                };
            }
        }

        private void LateUpdate()
        {
            if (!_isRunning || CurrentPhase != Phase.Recording)
                return;

            if (Input.GetKeyDown(StopKey))
                _stopRequested = true;

            foreach (var collector in _activeCollectors)
                collector.SampleFrame();
        }

        private void OnGUI()
        {
            if (!ShowStatsOverlay || !_isRunning || CurrentPhase != Phase.Recording)
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = Color.white;

            float y = 10;
            foreach (var collector in _activeCollectors)
            {
                string overlay = collector.Overlay();
                if (!string.IsNullOrEmpty(overlay))
                {
                    float height = 20;
                    GUI.DrawTexture(new Rect(5, y - 2, 500, height + 4), Texture2D.grayTexture);
                    GUI.Label(new Rect(10, y, 490, height), overlay, style);
                    y += height + 6;
                }
            }
        }

        [ContextMenu("Start Benchmark")]
        public void StartBenchmarkFromMenu()
        {
            StartCoroutine(RunBenchmark());
        }

        [ContextMenu("Stop Benchmark")]
        public void StopBenchmark()
        {
            _stopRequested = true;
        }

        public IEnumerator RunBenchmark()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[BenchmarkOrchestrator] Benchmark already running.");
                yield break;
            }

            _isRunning = true;
            _stopRequested = false;
            _runId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputDir = GetOutputDirectory();
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            SetupCollectors();

            // Suppress MapLogger overlay during recording to avoid visual collision
            var mapLogger = FindObjectOfType<MapLogger>();
            bool wasLogging = mapLogger != null && mapLogger.PrintScreen;
            if (mapLogger != null) mapLogger.PrintScreen = false;

            // Phase 1: Pre-cache
            if (PreCacheData)
            {
                CurrentPhase = Phase.PreCaching;
                Debug.Log("[BenchmarkOrchestrator] Phase: PreCaching");

                if (PreCacher != null)
                {
                    PreCacher.ComputeTilesForPath(
                        new LatitudeLongitude(StartLat, StartLng),
                        new LatitudeLongitude(EndLat, EndLng),
                        StartZoom, AnimationDuration);

                    bool preCacheDone = false;
                    PreCacher.PreCacheCompleted += () => preCacheDone = true;
                    yield return PreCacher.PreCacheAllTiles(Map.MapboxMap);
                    while (!preCacheDone)
                        yield return null;
                }

                if (CacheOnlyMode)
                {
                    var loggingFetcher = Map.DataFetchingManager as LoggingDataFetchingManager;
                    if (loggingFetcher?.CacheOnlySource != null)
                    {
                        loggingFetcher.CacheOnlySource.BlockNetwork = true;
                        Debug.Log("[BenchmarkOrchestrator] Cache-only mode enabled.");
                    }
                }
            }

            // Phase 2: Record
            CurrentPhase = Phase.Recording;
            Debug.Log("[BenchmarkOrchestrator] Phase: Recording");

            foreach (var collector in _activeCollectors)
                collector.Begin();

            VideoRecorder?.StartRecording(_runId);

            if (AnimationDuration > 0)
                yield return AnimateCamera();
            else
                yield return WaitForManualStop();

            // Hold the final frame while recorder flushes — prevents low-res flash in video
            if (VideoRecorder != null && VideoRecorder.IsRecording)
            {
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                VideoRecorder.StopRecording();
            }

            // Phase 3: Dump
            CurrentPhase = Phase.Dumping;
            Debug.Log("[BenchmarkOrchestrator] Phase: Dumping");

            foreach (var collector in _activeCollectors)
                collector.End();

            var json = BuildOutputJson();
            var jsonPath = Path.Combine(outputDir, $"{_runId}.json");
            File.WriteAllText(jsonPath, json.ToString(Newtonsoft.Json.Formatting.Indented));
            Debug.Log($"[BenchmarkOrchestrator] Stats saved to: {jsonPath}");

            if (VideoRecorder != null && VideoRecorder.CurrentOutputPath != null)
                Debug.Log($"[BenchmarkOrchestrator] Video saved to: {VideoRecorder.CurrentOutputPath}");

            // Disable cache-only mode
            if (CacheOnlyMode)
            {
                var loggingFetcher = Map.DataFetchingManager as LoggingDataFetchingManager;
                if (loggingFetcher?.CacheOnlySource != null)
                    loggingFetcher.CacheOnlySource.BlockNetwork = false;
            }

            // Restore MapLogger overlay
            if (mapLogger != null) mapLogger.PrintScreen = wasLogging;

            CurrentPhase = Phase.Complete;
            _isRunning = false;
            Debug.Log("[BenchmarkOrchestrator] Benchmark complete.");
        }

        private IEnumerator AnimateCamera()
        {
            // Build effective waypoint list — use Waypoints if populated, otherwise fall back to A-to-B
            var points = Waypoints != null && Waypoints.Count >= 2
                ? Waypoints
                : new List<CameraWaypoint>
                {
                    new CameraWaypoint(StartLat, StartLng, StartZoom, StartPitch, StartBearing),
                    new CameraWaypoint(EndLat, EndLng, EndZoom, EndPitch, EndBearing)
                };

            // Fill in zero zoom/pitch/bearing from previous waypoint
            for (int i = 1; i < points.Count; i++)
            {
                var wp = points[i];
                if (wp.Zoom == 0) wp.Zoom = points[i - 1].Zoom;
                if (wp.Pitch == 0) wp.Pitch = points[i - 1].Pitch;
                // Bearing 0 is valid (north), so we don't auto-fill it
                points[i] = wp;
            }

            float elapsed = 0f;
            float totalDuration = AnimationDuration * (PingPong ? PingPongCycles * 2 : 1);
            int segmentCount = points.Count - 1;

            // Set starting position
            Map.MapboxMap.ChangeView(
                new LatitudeLongitude(points[0].Lat, points[0].Lng),
                points[0].Zoom, points[0].Pitch, points[0].Bearing);

            while (elapsed < totalDuration && !_stopRequested)
            {
                elapsed += Time.deltaTime;

                float t;
                if (PingPong)
                {
                    float cycleTime = elapsed % (AnimationDuration * 2);
                    if (cycleTime > AnimationDuration)
                        t = 1f - (cycleTime - AnimationDuration) / AnimationDuration;
                    else
                        t = cycleTime / AnimationDuration;
                }
                else
                {
                    // Clamp to [0,1] — no modulo, prevents snap-back on the last frame
                    t = Mathf.Clamp01(elapsed / totalDuration);
                }

                t = Mathf.SmoothStep(0f, 1f, t);

                // Map t [0,1] to a segment index and local t within that segment
                float scaledT = t * segmentCount;
                int seg = Mathf.Min((int)scaledT, segmentCount - 1);
                float localT = scaledT - seg;
                localT = Mathf.SmoothStep(0f, 1f, localT);

                // Catmull-Rom control points: p0, p1, p2, p3
                var p1 = points[seg];
                var p2 = points[seg + 1];
                var p0 = seg > 0 ? points[seg - 1] : p1;
                var p3 = seg + 2 < points.Count ? points[seg + 2] : p2;

                var lat = CatmullRom(p0.Lat, p1.Lat, p2.Lat, p3.Lat, localT);
                var lng = CatmullRom(p0.Lng, p1.Lng, p2.Lng, p3.Lng, localT);
                var zoom = (float)CatmullRom(p0.Zoom, p1.Zoom, p2.Zoom, p3.Zoom, localT);
                var pitch = (float)CatmullRom(p0.Pitch, p1.Pitch, p2.Pitch, p3.Pitch, localT);
                var bearing = Mathf.Lerp(p1.Bearing, p2.Bearing, localT);

                Map.MapboxMap.ChangeView(
                    new LatitudeLongitude(lat, lng), zoom, pitch, bearing);

                yield return null;
            }
        }

        /// <summary>
        /// Catmull-Rom spline interpolation for smooth curves through waypoints.
        /// </summary>
        private static double CatmullRom(double p0, double p1, double p2, double p3, float t)
        {
            double tt = t * t;
            double ttt = tt * t;
            return 0.5 * (
                2.0 * p1 +
                (-p0 + p2) * t +
                (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * tt +
                (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * ttt);
        }

        private IEnumerator WaitForManualStop()
        {
            Debug.Log($"[BenchmarkOrchestrator] Recording until stopped (press {StopKey} or call StopBenchmark).");
            while (!_stopRequested)
                yield return null;
        }

        private void EnsureComponents()
        {
            if (PreCacher == null) PreCacher = GetComponent<BenchmarkPreCacher>();
            if (VideoRecorder == null) VideoRecorder = GetComponent<BenchmarkVideoRecorder>();
            if (NetworkStats == null) NetworkStats = GetComponent<NetworkStatsCollector>();
            if (CacheStats == null) CacheStats = GetComponent<CacheStatsCollector>();
            if (TaskStats == null) TaskStats = GetComponent<TaskStatsCollector>();
            if (TileStats == null) TileStats = GetComponent<TileStatsCollector>();
            if (SystemStats == null) SystemStats = GetComponent<SystemStatsCollector>();
        }

        private void SetupCollectors()
        {
            _activeCollectors.Clear();

            if (CollectNetworkStats && NetworkStats != null)
            {
                NetworkStats.SetDataFetchingManager(Map.DataFetchingManager);
                _activeCollectors.Add(NetworkStats);
            }

            if (CollectCacheStats && CacheStats != null)
            {
                CacheStats.SetDataFetchingManager(Map.DataFetchingManager);
                CacheStats.SetCaches(Map.LoggingFileCache, Map.LoggingSqliteCache);
                _activeCollectors.Add(CacheStats);
            }

            if (CollectTaskStats && TaskStats != null)
            {
                TaskStats.SetTaskManager(Map.UnityContext.TaskManager);
                _activeCollectors.Add(TaskStats);
            }

            if (CollectTileStats && TileStats != null)
            {
                TileStats.SetMap(Map.MapboxMap);
                _activeCollectors.Add(TileStats);
            }

            if (CollectSystemStats && SystemStats != null)
            {
                SystemStats.MapBehaviour = Map;
                _activeCollectors.Add(SystemStats);
            }
        }

        private JObject BuildOutputJson()
        {
            var root = new JObject
            {
                ["version"] = 1,
                ["runId"] = _runId,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
            };

            if (VideoRecorder?.CurrentOutputPath != null)
                root["videoFile"] = Path.GetFileName(VideoRecorder.CurrentOutputPath) + ".mp4";

            root["config"] = new JObject
            {
                ["preCacheData"] = PreCacheData,
                ["cacheOnlyMode"] = CacheOnlyMode,
                ["showOverlay"] = ShowStatsOverlay,
                ["animationDuration"] = AnimationDuration,
                ["pingPong"] = PingPong,
                ["pingPongCycles"] = PingPongCycles,
                ["videoResolution"] = $"{VideoRecorder?.Width ?? 0}x{VideoRecorder?.Height ?? 0}",
                ["videoFrameRate"] = VideoRecorder?.FrameRate ?? 0
            };

            if (PreCacher != null)
            {
                root["preCacheStats"] = new JObject
                {
                    ["totalTilesToCache"] = PreCacher.TotalTilesToCache,
                    ["tilesetIds"] = PreCacher.TilesetIds != null
                        ? new JArray(PreCacher.TilesetIds)
                        : new JArray()
                };
            }

            foreach (var collector in _activeCollectors)
            {
                string key = collector.GetType().Name
                    .Replace("StatsCollector", "")
                    .ToLowerInvariant();
                root[key] = collector.Export();
            }

            return root;
        }

        private string GetOutputDirectory()
        {
            if (!string.IsNullOrEmpty(OutputDirectory))
                return OutputDirectory;
            return Path.Combine(Application.persistentDataPath, "benchmarks");
        }
    }
}
