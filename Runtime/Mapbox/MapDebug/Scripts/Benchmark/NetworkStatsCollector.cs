using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Map;
using Mapbox.MapDebug.Scripts.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Collects network request metrics: request counts, latency, byte sizes, response codes.
    /// Can be used standalone — drop onto any scene and assign MapBehaviour (or it auto-discovers).
    /// </summary>
    public class NetworkStatsCollector : MonoBehaviour, IBenchmarkStats
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
        public KeyCode DumpKey = KeyCode.F9;

        private struct FrameData
        {
            public int ActiveFetchCount;
            public int QueueDepth;
        }

        private FrameData[] _frames;
        private int _frameIndex;
        private bool _recording;

        // Aggregate counters — incremented via event callbacks
        private int _requestsFired;
        private int _requestsSucceeded;
        private int _requestsFailed;
        private int _requestsCancelled;
        private long _totalBytesDownloaded;

        // Latency tracking
        private float _latencyMin, _latencyMax;
        private double _latencySum;
        private int _latencyCount;
        private float _queueTimeMin, _queueTimeMax;
        private double _queueTimeSum;
        private int _queueTimeCount;

        // Response code histogram
        private Dictionary<long, int> _responseCodeDistribution = new Dictionary<long, int>();

        // References
        private LoggingDataFetchingManager _loggingFetcher;
        private DataFetchingManager _fetcher;

        // Track per-fetch start times for latency computation
        private Dictionary<FetchInfo, float> _fetchStartTimes = new Dictionary<FetchInfo, float>();

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

                SetDataFetchingManager(MapBehaviour.DataFetchingManager);
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
                DumpToFile(Path.Combine(Application.persistentDataPath, $"network_stats_{DateTime.Now:yyyyMMdd_HHmmss}.json"));
                Begin();
            }
        }

        public void SetDataFetchingManager(DataFetchingManager fetcher)
        {
            _fetcher = fetcher;
            _loggingFetcher = fetcher as LoggingDataFetchingManager;
        }

        public void Begin()
        {
            _frames = new FrameData[MaxFrames];
            _frameIndex = 0;
            _requestsFired = 0;
            _requestsSucceeded = 0;
            _requestsFailed = 0;
            _requestsCancelled = 0;
            _totalBytesDownloaded = 0;
            _latencyMin = float.MaxValue;
            _latencyMax = 0;
            _latencySum = 0;
            _latencyCount = 0;
            _queueTimeMin = float.MaxValue;
            _queueTimeMax = 0;
            _queueTimeSum = 0;
            _queueTimeCount = 0;
            _responseCodeDistribution.Clear();
            _fetchStartTimes.Clear();

            if (_fetcher != null)
            {
                _fetcher.FetchInitialized += OnFetchInitialized;
                _fetcher.FetchFinished += OnFetchFinished;
                _fetcher.FetchCancelled += OnFetchCancelled;
            }

            _recording = true;
        }

        public void SampleFrame()
        {
            if (!_recording || _frameIndex >= _frames.Length)
                return;

            ref var frame = ref _frames[_frameIndex];

            if (_loggingFetcher != null)
            {
                frame.ActiveFetchCount = _loggingFetcher.ActiveFetchCount;
                frame.QueueDepth = _loggingFetcher.FetchQueueCount;
            }

            _frameIndex++;
        }

        public void End()
        {
            _recording = false;

            if (_fetcher != null)
            {
                _fetcher.FetchInitialized -= OnFetchInitialized;
                _fetcher.FetchFinished -= OnFetchFinished;
                _fetcher.FetchCancelled -= OnFetchCancelled;
            }

            _fetchStartTimes.Clear();
        }

        public JObject Export()
        {
            var codesDist = new JObject();
            foreach (var kvp in _responseCodeDistribution)
                codesDist[kvp.Key.ToString()] = kvp.Value;

            var result = new JObject
            {
                ["requestsFired"] = _requestsFired,
                ["requestsSucceeded"] = _requestsSucceeded,
                ["requestsFailed"] = _requestsFailed,
                ["requestsCancelled"] = _requestsCancelled,
                ["totalBytesDownloaded"] = _totalBytesDownloaded,
                ["responseCodeDistribution"] = codesDist,
                ["latencyMs"] = new JObject
                {
                    ["min"] = _latencyCount > 0 ? Math.Round(_latencyMin * 1000, 1) : 0,
                    ["max"] = _latencyCount > 0 ? Math.Round(_latencyMax * 1000, 1) : 0,
                    ["avg"] = _latencyCount > 0 ? Math.Round(_latencySum / _latencyCount * 1000, 1) : 0
                },
                ["queueTimeMs"] = new JObject
                {
                    ["min"] = _queueTimeCount > 0 ? Math.Round(_queueTimeMin * 1000, 1) : 0,
                    ["max"] = _queueTimeCount > 0 ? Math.Round(_queueTimeMax * 1000, 1) : 0,
                    ["avg"] = _queueTimeCount > 0 ? Math.Round(_queueTimeSum / _queueTimeCount * 1000, 1) : 0
                }
            };

            return result;
        }

        public string Overlay()
        {
            if (!ShowOverlay || !_recording || _frameIndex == 0)
                return string.Empty;

            ref var f = ref _frames[_frameIndex - 1];
            return string.Format("Net | Active: {0} Queue: {1} | Fired: {2} OK: {3} Fail: {4} Cancel: {5}",
                f.ActiveFetchCount, f.QueueDepth,
                _requestsFired, _requestsSucceeded, _requestsFailed, _requestsCancelled);
        }

        public void DumpToFile(string path)
        {
            var json = Export();
            File.WriteAllText(path, json.ToString());
            Debug.Log($"[NetworkStatsCollector] Stats saved to: {path}");
        }

        private void OnFetchInitialized(FetchInfo info)
        {
            if (!_recording) return;

            _requestsFired++;
            _fetchStartTimes[info] = Time.realtimeSinceStartup;

            if (info.QueueTime > 0)
            {
                float queueTime = Time.realtimeSinceStartup - info.QueueTime;
                if (queueTime < _queueTimeMin) _queueTimeMin = queueTime;
                if (queueTime > _queueTimeMax) _queueTimeMax = queueTime;
                _queueTimeSum += queueTime;
                _queueTimeCount++;
            }
        }

        private void OnFetchFinished(FetchInfo info)
        {
            if (!_recording) return;

            long statusCode = info.Tile.StatusCode;

            if (_responseCodeDistribution.ContainsKey(statusCode))
                _responseCodeDistribution[statusCode]++;
            else
                _responseCodeDistribution[statusCode] = 1;

            if (statusCode >= 200 && statusCode < 400)
                _requestsSucceeded++;
            else
                _requestsFailed++;

            if (_fetchStartTimes.TryGetValue(info, out float startTime))
            {
                float latency = Time.realtimeSinceStartup - startTime;
                if (latency < _latencyMin) _latencyMin = latency;
                if (latency > _latencyMax) _latencyMax = latency;
                _latencySum += latency;
                _latencyCount++;
                _fetchStartTimes.Remove(info);
            }
        }

        private void OnFetchCancelled(FetchInfo info)
        {
            if (!_recording) return;

            _requestsCancelled++;
            _fetchStartTimes.Remove(info);
        }
    }
}
