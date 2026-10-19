using System;
using System.Collections;
using System.IO;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.MapDebug.Scripts.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Collects cache performance metrics: hit rates per layer, read/write counts.
    /// Hooks into DataFetchingManager.FetchFinished and reads Tile.FromCache to determine
    /// which cache layer resolved each request.
    /// Drop onto any scene with a LoggingMapBehaviour (or assign in Inspector).
    /// </summary>
    public class CacheStatsCollector : MonoBehaviour, IBenchmarkStats
    {
        [Tooltip("Map behaviour for auto-wiring in standalone mode. Uses FindObjectOfType if empty.")]
        public LoggingMapBehaviour MapBehaviour;

        [Tooltip("Show overlay text on screen")]
        public bool ShowOverlay;

        [Tooltip("Run standalone — Begin on Start, SampleFrame on LateUpdate")]
        public bool Standalone;

        [Tooltip("Key to dump stats to file in standalone mode")]
        public KeyCode DumpKey = KeyCode.F10;

        private bool _recording;

        // Per-layer hit counters
        private int _memoryCacheHits;
        private int _fileCacheHits;
        private int _sqliteCacheHits;
        private int _networkFetches; // NoCache — went to network
        private int _totalRequests;

        // Read/write counters from LoggingFileCache/LoggingSqliteCache if available
        private int _startFileCacheReads, _startFileCacheWrites;
        private int _startSqliteReads, _startSqliteWrites, _startSqliteUpdates;

        // References
        private DataFetchingManager _fetcher;
        private LoggingFileCache _fileCache;
        private LoggingSqliteCache _sqliteCache;

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
                SetCaches(MapBehaviour.LoggingFileCache, MapBehaviour.LoggingSqliteCache);
            }

            Begin();
        }

        private void LateUpdate()
        {
            if (Standalone && Input.GetKeyDown(DumpKey) && _recording)
            {
                End();
                DumpToFile(Path.Combine(Application.persistentDataPath, $"cache_stats_{DateTime.Now:yyyyMMdd_HHmmss}.json"));
                Begin();
            }
        }

        public void SetDataFetchingManager(DataFetchingManager fetcher)
        {
            _fetcher = fetcher;
        }

        public void SetCaches(LoggingFileCache fileCache, LoggingSqliteCache sqliteCache)
        {
            _fileCache = fileCache;
            _sqliteCache = sqliteCache;
        }

        public void Begin()
        {
            _memoryCacheHits = 0;
            _fileCacheHits = 0;
            _sqliteCacheHits = 0;
            _networkFetches = 0;
            _totalRequests = 0;

            if (_fileCache != null)
            {
                _startFileCacheReads = _fileCache.ReadCount;
                _startFileCacheWrites = _fileCache.SavedCount;
            }
            if (_sqliteCache != null)
            {
                _startSqliteReads = _sqliteCache.GetCount;
                _startSqliteWrites = _sqliteCache.AddCount;
                _startSqliteUpdates = _sqliteCache.UpdateCount;
            }

            if (_fetcher != null)
                _fetcher.FetchFinished += OnFetchFinished;

            _recording = true;
        }

        public void SampleFrame()
        {
            // Cache stats are event-driven, no per-frame sampling needed
        }

        public void End()
        {
            _recording = false;
            if (_fetcher != null)
                _fetcher.FetchFinished -= OnFetchFinished;
        }

        public JObject Export()
        {
            int fileCacheReads = _fileCache != null ? _fileCache.ReadCount - _startFileCacheReads : 0;
            int fileCacheWrites = _fileCache != null ? _fileCache.SavedCount - _startFileCacheWrites : 0;
            int sqliteReads = _sqliteCache != null ? _sqliteCache.GetCount - _startSqliteReads : 0;
            int sqliteWrites = _sqliteCache != null ? _sqliteCache.AddCount - _startSqliteWrites : 0;
            int sqliteUpdates = _sqliteCache != null ? _sqliteCache.UpdateCount - _startSqliteUpdates : 0;

            float fileCacheHitRate = _totalRequests > 0 ? _fileCacheHits / (float)_totalRequests : 0;
            float sqliteCacheHitRate = _totalRequests > 0 ? _sqliteCacheHits / (float)_totalRequests : 0;
            float memoryCacheHitRate = _totalRequests > 0 ? _memoryCacheHits / (float)_totalRequests : 0;

            return new JObject
            {
                ["memoryCacheHits"] = _memoryCacheHits,
                ["fileCacheHits"] = _fileCacheHits,
                ["sqliteCacheHits"] = _sqliteCacheHits,
                ["networkFetches"] = _networkFetches,
                ["totalRequests"] = _totalRequests,
                ["memoryCacheHitRate"] = Math.Round(memoryCacheHitRate, 3),
                ["fileCacheHitRate"] = Math.Round(fileCacheHitRate, 3),
                ["sqliteCacheHitRate"] = Math.Round(sqliteCacheHitRate, 3),
                ["layerBreakdown"] = new JObject
                {
                    ["memory"] = _memoryCacheHits,
                    ["file"] = _fileCacheHits,
                    ["sqlite"] = _sqliteCacheHits,
                    ["network"] = _networkFetches
                },
                ["fileCacheReads"] = fileCacheReads,
                ["fileCacheWrites"] = fileCacheWrites,
                ["sqliteReads"] = sqliteReads,
                ["sqliteWrites"] = sqliteWrites,
                ["sqliteUpdates"] = sqliteUpdates
            };
        }

        public string Overlay()
        {
            if (!ShowOverlay || !_recording)
                return string.Empty;

            return string.Format("Cache | Mem: {0} File: {1} Sqlite: {2} Net: {3} | Total: {4}",
                _memoryCacheHits, _fileCacheHits, _sqliteCacheHits, _networkFetches, _totalRequests);
        }

        public void DumpToFile(string path)
        {
            var json = Export();
            File.WriteAllText(path, json.ToString());
            Debug.Log($"[CacheStatsCollector] Stats saved to: {path}");
        }

        private void OnFetchFinished(FetchInfo info)
        {
            if (!_recording) return;

            _totalRequests++;

            switch (info.Tile.FromCache)
            {
                case CacheType.MemoryCache:
                    _memoryCacheHits++;
                    break;
                case CacheType.FileCache:
                    _fileCacheHits++;
                    break;
                case CacheType.SqliteCache:
                    _sqliteCacheHits++;
                    break;
                case CacheType.NoCache:
                case CacheType.NoCacheUpdated:
                    _networkFetches++;
                    break;
            }
        }
    }
}
