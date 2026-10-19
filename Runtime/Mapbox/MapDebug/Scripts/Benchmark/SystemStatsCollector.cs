using System;
using System.IO;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Newtonsoft.Json.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Collects system-level performance metrics: FPS, frame time, memory, GC pressure.
    /// Can be used standalone on any scene — no SDK dependencies required.
    /// Optionally reads camera lat/lng/zoom/pitch/bearing from MapInformation.
    /// </summary>
    public class SystemStatsCollector : MonoBehaviour, IBenchmarkStats
    {
        [Tooltip("Map information source for camera position data. Optional — leave null for system-only stats.")]
        public MapBehaviourCore MapBehaviour;

        [Tooltip("Show overlay text on screen")]
        public bool ShowOverlay;

        [Tooltip("Maximum frames to record. Pre-allocated at Begin().")]
        public int MaxFrames = 36000;

        [Tooltip("Run standalone — Begin on Start, SampleFrame on LateUpdate")]
        public bool Standalone;

        [Tooltip("Key to dump stats to file in standalone mode")]
        public KeyCode DumpKey = KeyCode.F7;

        private struct FrameData
        {
            public float DeltaTimeMs;
            public float TotalMemoryMB;
            public float ManagedHeapMB;
            public int GCCollectionCount;
            public long AllocatedBytes;
            public Vector3 CameraPosition;
            public double Lat, Lng;
            public float Zoom, Pitch, Bearing;
        }

        private FrameData[] _frames;
        private int _frameIndex;
        private bool _recording;

        // Running aggregates
        private float _minFps, _maxFps;
        private double _fpsSum;
        private float _maxFrameTimeMs;
        private float _peakMemoryMB;
        private float _peakManagedHeapMB;
        private int _startGCCount;
        // GC.GetTotalAllocatedBytes needs .NET Standard 2.1 and the SDK has to compile at 2.0, so
        // the per-frame profiler counter is summed instead. It reports only when the profiler is
        // available - Editor and development builds - which is where benchmarks are run anyway.
        private ProfilerRecorder _gcAllocRecorder;
        private long _allocatedBytes;

        private IMapInformation _mapInfo;

        private void Start()
        {
            if (Standalone)
                Begin();
        }

        private void LateUpdate()
        {
            if (Standalone && _recording)
                SampleFrame();

            if (Standalone && Input.GetKeyDown(DumpKey) && _recording)
            {
                End();
                DumpToFile(Path.Combine(Application.persistentDataPath, $"system_stats_{DateTime.Now:yyyyMMdd_HHmmss}.json"));
                Begin();
            }
        }

        public void Begin()
        {
            _frames = new FrameData[MaxFrames];
            _frameIndex = 0;
            _minFps = float.MaxValue;
            _maxFps = 0;
            _fpsSum = 0;
            _maxFrameTimeMs = 0;
            _peakMemoryMB = 0;
            _peakManagedHeapMB = 0;
            _startGCCount = GC.CollectionCount(0);
            // Cumulative managed bytes allocated, which gcCollections only hints at: a run can
            // allocate heavily and collect rarely, or the reverse. This is the number the
            // per-feature allocations in the mesh-modifier stack move.
            _allocatedBytes = 0;
            if (_gcAllocRecorder.Valid)
                _gcAllocRecorder.Dispose();
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _mapInfo = MapBehaviour != null ? MapBehaviour.MapInformation : null;
            _recording = true;
        }

        public void SampleFrame()
        {
            if (!_recording || _frameIndex >= _frames.Length)
                return;

            float dt = Time.unscaledDeltaTime;
            float dtMs = dt * 1000f;
            float fps = dt > 0f ? 1f / dt : 0f;
            float totalMem = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            float managedMem = Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);

            ref var frame = ref _frames[_frameIndex];
            frame.DeltaTimeMs = dtMs;
            frame.TotalMemoryMB = totalMem;
            frame.ManagedHeapMB = managedMem;
            frame.GCCollectionCount = GC.CollectionCount(0);
            long frameAlloc = _gcAllocRecorder.Valid ? _gcAllocRecorder.LastValue : 0;
            frame.AllocatedBytes = frameAlloc;
            _allocatedBytes += frameAlloc;

            var cam = Camera.main;
            if (cam != null)
                frame.CameraPosition = cam.transform.position;

            if (_mapInfo != null)
            {
                frame.Lat = _mapInfo.LatitudeLongitude.Latitude;
                frame.Lng = _mapInfo.LatitudeLongitude.Longitude;
                frame.Zoom = _mapInfo.Zoom;
                frame.Pitch = _mapInfo.Pitch;
                frame.Bearing = _mapInfo.Bearing;
            }

            // Update running aggregates
            if (fps < _minFps) _minFps = fps;
            if (fps > _maxFps) _maxFps = fps;
            _fpsSum += fps;
            if (dtMs > _maxFrameTimeMs) _maxFrameTimeMs = dtMs;
            if (totalMem > _peakMemoryMB) _peakMemoryMB = totalMem;
            if (managedMem > _peakManagedHeapMB) _peakManagedHeapMB = managedMem;

            _frameIndex++;
        }

        public void End()
        {
            _recording = false;
            if (_gcAllocRecorder.Valid)
                _gcAllocRecorder.Dispose();
        }

        public JObject Export()
        {
            int count = _frameIndex;
            float avgFps = count > 0 ? (float)(_fpsSum / count) : 0;
            int gcCollections = GC.CollectionCount(0) - _startGCCount;
            long allocatedBytes = _allocatedBytes;

            // Compute percentiles from sorted FPS values
            float p1Fps = 0, p5Fps = 0, p99FrameTimeMs = 0;
            if (count > 0)
            {
                var fpsValues = new float[count];
                var dtValues = new float[count];
                for (int i = 0; i < count; i++)
                {
                    fpsValues[i] = _frames[i].DeltaTimeMs > 0 ? 1000f / _frames[i].DeltaTimeMs : 0;
                    dtValues[i] = _frames[i].DeltaTimeMs;
                }
                Array.Sort(fpsValues);
                Array.Sort(dtValues);
                p1Fps = fpsValues[Math.Max(0, (int)(count * 0.01f))];
                p5Fps = fpsValues[Math.Max(0, (int)(count * 0.05f))];
                p99FrameTimeMs = dtValues[Math.Min(count - 1, (int)(count * 0.99f))];
            }

            float durationSeconds = 0;
            for (int i = 0; i < count; i++)
                durationSeconds += _frames[i].DeltaTimeMs / 1000f;

            // Distribution of per-frame allocation. A flat ~15 KB on every frame and a few frames
            // allocating megabytes produce the same total; only the shape says which one this is.
            var allocPerFrame = new JObject();
            if (count > 0)
            {
                var allocValues = new long[count];
                int framesOverA100KB = 0;
                int framesAllocatingNothing = 0;
                for (int i = 0; i < count; i++)
                {
                    allocValues[i] = _frames[i].AllocatedBytes;
                    if (allocValues[i] == 0) framesAllocatingNothing++;
                    if (allocValues[i] > 100 * 1024) framesOverA100KB++;
                }
                Array.Sort(allocValues);

                allocPerFrame["medianBytes"] = allocValues[count / 2];
                allocPerFrame["p95Bytes"] = allocValues[Math.Min(count - 1, (int)(count * 0.95f))];
                allocPerFrame["p99Bytes"] = allocValues[Math.Min(count - 1, (int)(count * 0.99f))];
                allocPerFrame["maxBytes"] = allocValues[count - 1];
                allocPerFrame["framesAllocatingNothing"] = framesAllocatingNothing;
                allocPerFrame["framesOver100KB"] = framesOverA100KB;
            }

            var result = new JObject
            {
                ["totalFrames"] = count,
                ["durationSeconds"] = Math.Round(durationSeconds, 2),
                ["fps"] = new JObject
                {
                    ["avg"] = Math.Round(avgFps, 1),
                    ["min"] = Math.Round(_minFps, 1),
                    ["max"] = Math.Round(_maxFps, 1),
                    ["p1"] = Math.Round(p1Fps, 1),
                    ["p5"] = Math.Round(p5Fps, 1)
                },
                ["frameTimeMs"] = new JObject
                {
                    ["avg"] = count > 0 ? Math.Round(durationSeconds * 1000.0 / count, 1) : 0,
                    ["max"] = Math.Round(_maxFrameTimeMs, 1),
                    ["p99"] = Math.Round(p99FrameTimeMs, 1)
                },
                ["peakMemoryMB"] = Math.Round(_peakMemoryMB, 1),
                ["peakManagedHeapMB"] = Math.Round(_peakManagedHeapMB, 1),
                ["gcCollections"] = gcCollections,
                ["managedAllocatedMB"] = Math.Round(allocatedBytes / (1024.0 * 1024.0), 1),
                ["managedAllocatedBytes"] = allocatedBytes,
                // An average says nothing about whether allocation is a steady per-frame floor or a
                // handful of spikes, and those two want completely different fixes.
                ["allocPerFrame"] = allocPerFrame
            };

            return result;
        }

        public string Overlay()
        {
            if (!ShowOverlay || !_recording || _frameIndex == 0)
                return string.Empty;

            float fps = _frames[_frameIndex - 1].DeltaTimeMs > 0
                ? 1000f / _frames[_frameIndex - 1].DeltaTimeMs : 0;
            return string.Format("FPS: {0:F0} | Mem: {1:F0}MB | GC: {2}",
                fps, _frames[_frameIndex - 1].TotalMemoryMB,
                GC.CollectionCount(0) - _startGCCount);
        }

        public void DumpToFile(string path)
        {
            var json = Export();
            File.WriteAllText(path, json.ToString());
            Debug.Log($"[SystemStatsCollector] Stats saved to: {path}");
        }
    }
}
