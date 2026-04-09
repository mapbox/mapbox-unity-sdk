using System;
using System.Collections;
using System.IO;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Map;
using Mapbox.MapDebug.Scripts.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Collects task and mesh generation metrics: task counts, mesh gen timing,
    /// vertex/triangle counts. Drop onto any scene with a LoggingMapBehaviour (or assign in Inspector).
    /// </summary>
    public class TaskStatsCollector : MonoBehaviour, IBenchmarkStats
    {
        [Tooltip("Map behaviour for auto-wiring in standalone mode. Uses FindObjectOfType if empty.")]
        public LoggingMapBehaviour MapBehaviour;

        [Tooltip("Show overlay text on screen")]
        public bool ShowOverlay;

        [Tooltip("Maximum per-frame samples to record")]
        public int MaxFrames = 36000;

        [Tooltip("Run standalone — Begin on Start, SampleFrame on LateUpdate")]
        public bool Standalone;

        [Tooltip("Key to dump stats to file in standalone mode")]
        public KeyCode DumpKey = KeyCode.F11;

        private struct FrameData
        {
            public int ActiveTaskCount;
        }

        private FrameData[] _frames;
        private int _frameIndex;
        private bool _recording;

        // Aggregate counters — updated via events
        private int _tasksCreated;
        private int _tasksCancelled;
        private int _meshGenCount;
        private float _meshGenTimeMin, _meshGenTimeMax;
        private double _meshGenTimeSum;
        private long _meshGenVerticesTotal;
        private long _meshGenTrianglesTotal;

        // References
        private TaskManager _taskManager;
        private LoggingTaskManager _loggingTaskManager;

        // Snapshot of LoggingTaskManager counters at Begin
        private int _startTasksCreated;
        private int _startTasksCancelled;

        private IEnumerator Start()
        {
            if (!Standalone) yield break;

            if (MapBehaviour == null)
                MapBehaviour = FindObjectOfType<LoggingMapBehaviour>();

            if (MapBehaviour != null)
            {
                if (MapBehaviour.InitializationStatus == InitializationStatus.WaitingForInitialization)
                {
                    bool ready = false;
                    MapBehaviour.Initialized += _ => ready = true;
                    while (!ready) yield return null;
                }

                SetTaskManager(MapBehaviour.UnityContext.TaskManager);
            }

            Begin();
        }

        private void LateUpdate()
        {
            if (Standalone && _recording)
                SampleFrame();

            if (Standalone && Input.GetKeyDown(DumpKey) && _recording)
            {
                End();
                DumpToFile(Path.Combine(Application.persistentDataPath, $"task_stats_{DateTime.Now:yyyyMMdd_HHmmss}.json"));
                Begin();
            }
        }

        public void SetTaskManager(TaskManager taskManager)
        {
            _taskManager = taskManager;
            _loggingTaskManager = taskManager as LoggingTaskManager;
        }

        public void Begin()
        {
            _frames = new FrameData[MaxFrames];
            _frameIndex = 0;
            _tasksCreated = 0;
            _tasksCancelled = 0;
            _meshGenCount = 0;
            _meshGenTimeMin = float.MaxValue;
            _meshGenTimeMax = 0;
            _meshGenTimeSum = 0;
            _meshGenVerticesTotal = 0;
            _meshGenTrianglesTotal = 0;

            if (_loggingTaskManager != null)
            {
                _startTasksCreated = _loggingTaskManager.TotalTaskCreatedCount;
                _startTasksCancelled = _loggingTaskManager.TotalCancelledCount;
            }

            if (_taskManager != null)
            {
                _taskManager.TaskFinished += OnTaskFinished;
                _taskManager.TaskCancelled += OnTaskCancelled;
            }

            _recording = true;
        }

        public void SampleFrame()
        {
            if (!_recording || _frameIndex >= _frames.Length)
                return;

            ref var frame = ref _frames[_frameIndex];
            if (_loggingTaskManager != null)
                frame.ActiveTaskCount = _loggingTaskManager.ActiveTaskCount;

            _frameIndex++;
        }

        public void End()
        {
            _recording = false;

            if (_taskManager != null)
            {
                _taskManager.TaskFinished -= OnTaskFinished;
                _taskManager.TaskCancelled -= OnTaskCancelled;
            }
        }

        public JObject Export()
        {
            int tasksCreated = _loggingTaskManager != null
                ? _loggingTaskManager.TotalTaskCreatedCount - _startTasksCreated
                : _tasksCreated;
            int tasksCancelled = _loggingTaskManager != null
                ? _loggingTaskManager.TotalCancelledCount - _startTasksCancelled
                : _tasksCancelled;

            float avgMeshGenTime = _meshGenCount > 0 ? (float)(_meshGenTimeSum / _meshGenCount) : 0;
            long avgVertices = _meshGenCount > 0 ? _meshGenVerticesTotal / _meshGenCount : 0;
            long avgTriangles = _meshGenCount > 0 ? _meshGenTrianglesTotal / _meshGenCount : 0;

            var result = new JObject
            {
                ["tasksCreated"] = tasksCreated,
                ["tasksCancelled"] = tasksCancelled,
                ["meshGenCount"] = _meshGenCount,
                ["meshGenTimeMs"] = new JObject
                {
                    ["min"] = _meshGenCount > 0 ? Math.Round(_meshGenTimeMin * 1000, 2) : 0,
                    ["max"] = _meshGenCount > 0 ? Math.Round(_meshGenTimeMax * 1000, 2) : 0,
                    ["avg"] = Math.Round(avgMeshGenTime * 1000, 2)
                },
                ["meshGenVertices"] = new JObject
                {
                    ["total"] = _meshGenVerticesTotal,
                    ["avg"] = avgVertices
                },
                ["meshGenTriangles"] = new JObject
                {
                    ["total"] = _meshGenTrianglesTotal,
                    ["avg"] = avgTriangles
                }
            };

            return result;
        }

        public string Overlay()
        {
            if (!ShowOverlay || !_recording || _frameIndex == 0)
                return string.Empty;

            float avgMs = _meshGenCount > 0 ? (float)(_meshGenTimeSum / _meshGenCount * 1000) : 0;
            return string.Format("Tasks | Active: {0} Created: {1} | MeshGen: {2} avg {3:F1}ms",
                _frames[_frameIndex - 1].ActiveTaskCount,
                _loggingTaskManager != null
                    ? _loggingTaskManager.TotalTaskCreatedCount - _startTasksCreated
                    : _tasksCreated,
                _meshGenCount, avgMs);
        }

        public void DumpToFile(string path)
        {
            var json = Export();
            File.WriteAllText(path, json.ToString());
            Debug.Log($"[TaskStatsCollector] Stats saved to: {path}");
        }

        private void OnTaskFinished(TaskWrapper task)
        {
            if (!_recording) return;

            if (task is MeshGenTaskWrapper<MeshGenTaskWrapperResult> meshTask)
            {
                float time = task.FinishedTime - task.StartingTime;
                _meshGenCount++;
                if (time < _meshGenTimeMin) _meshGenTimeMin = time;
                if (time > _meshGenTimeMax) _meshGenTimeMax = time;
                _meshGenTimeSum += time;

                if (meshTask.DataResult?.Data != null)
                {
                    foreach (var layerKvp in meshTask.DataResult.Data)
                    {
                        foreach (var featureKvp in layerKvp.Value)
                        {
                            foreach (var meshData in featureKvp.Value)
                            {
                                if (meshData.Vertices != null)
                                    _meshGenVerticesTotal += meshData.Vertices.Count;
                                if (meshData.Triangles != null)
                                {
                                    foreach (var triList in meshData.Triangles)
                                        _meshGenTrianglesTotal += triList.Count / 3;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnTaskCancelled(TaskWrapper task)
        {
            if (!_recording) return;
            _tasksCancelled++;
        }
    }
}
