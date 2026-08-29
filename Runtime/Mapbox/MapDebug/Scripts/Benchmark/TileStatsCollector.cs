using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Collects per-frame tile state: tile count, tiles added/removed, zoom distribution.
    /// Drop onto any scene with a LoggingMapBehaviour (or assign in Inspector).
    /// </summary>
    public class TileStatsCollector : MonoBehaviour, IBenchmarkStats
    {
        [Tooltip("Map behaviour for auto-wiring in standalone mode. Uses FindObjectOfType if empty.")]
        public LoggingMapBehaviour MapBehaviour;

        [Tooltip("Show overlay text on screen")]
        public bool ShowOverlay;

        [Tooltip("Maximum frames to record")]
        public int MaxFrames = 36000;

        [Tooltip("Run standalone — Begin on Start, SampleFrame on LateUpdate")]
        public bool Standalone;

        [Tooltip("Key to dump stats to file in standalone mode")]
        public KeyCode DumpKey = KeyCode.F8;

        private struct FrameData
        {
            public int TileCount;
            public int TilesAdded;
            public int TilesRemoved;
            public int LoadedTileCount;
        }

        private FrameData[] _frames;
        private int _frameIndex;
        private bool _recording;

        // Pre-allocated for delta computation — no per-frame allocation
        private HashSet<UnwrappedTileId> _previousTiles = new HashSet<UnwrappedTileId>();
        private HashSet<UnwrappedTileId> _tempSet = new HashSet<UnwrappedTileId>();

        // Pre-allocated zoom distribution
        private int[] _zoomDistribution = new int[23];

        // Running aggregates
        private int _minTileCount, _maxTileCount;
        private long _tileCountSum;
        private long _totalTileChanges;

        private MapboxMap _map;

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

                _map = MapBehaviour.MapboxMap;
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
                DumpToFile(Path.Combine(Application.persistentDataPath, $"tile_stats_{DateTime.Now:yyyyMMdd_HHmmss}.json"));
                Begin();
            }
        }

        public void SetMap(MapboxMap map)
        {
            _map = map;
        }

        public void Begin()
        {
            if (_map == null && MapBehaviour != null)
                _map = MapBehaviour.MapboxMap;

            _frames = new FrameData[MaxFrames];
            _frameIndex = 0;
            _previousTiles.Clear();
            _minTileCount = int.MaxValue;
            _maxTileCount = 0;
            _tileCountSum = 0;
            _totalTileChanges = 0;
            _recording = true;
        }

        public void SampleFrame()
        {
            if (!_recording || _frameIndex >= _frames.Length || _map?.TileCover?.Tiles == null)
                return;

            var tiles = _map.TileCover.Tiles;
            int tileCount = tiles.Count;

            // Compute delta using pre-allocated sets
            int added = 0;
            int removed = 0;

            foreach (var tile in tiles)
            {
                if (!_previousTiles.Contains(tile))
                    added++;
            }
            foreach (var tile in _previousTiles)
            {
                if (!tiles.Contains(tile))
                    removed++;
            }

            // Update previous set (swap pattern to avoid alloc)
            _previousTiles.Clear();
            foreach (var tile in tiles)
                _previousTiles.Add(tile);

            // Zoom distribution
            Array.Clear(_zoomDistribution, 0, _zoomDistribution.Length);
            foreach (var tile in tiles)
            {
                if (tile.Z >= 0 && tile.Z < _zoomDistribution.Length)
                    _zoomDistribution[tile.Z]++;
            }

            int loadedCount = 0;
            if (_map.MapVisualizer != null)
                loadedCount = _map.MapVisualizer.ActiveTiles.Count;

            ref var frame = ref _frames[_frameIndex];
            frame.TileCount = tileCount;
            frame.TilesAdded = added;
            frame.TilesRemoved = removed;
            frame.LoadedTileCount = loadedCount;

            // Running aggregates
            if (tileCount < _minTileCount) _minTileCount = tileCount;
            if (tileCount > _maxTileCount) _maxTileCount = tileCount;
            _tileCountSum += tileCount;
            _totalTileChanges += added + removed;

            _frameIndex++;
        }

        public void End()
        {
            _recording = false;
        }

        public JObject Export()
        {
            int count = _frameIndex;
            float avgTileCount = count > 0 ? _tileCountSum / (float)count : 0;

            // Final zoom distribution snapshot from last frame's data
            var zoomDist = new JObject();
            for (int z = 0; z < _zoomDistribution.Length; z++)
            {
                if (_zoomDistribution[z] > 0)
                    zoomDist[z.ToString()] = _zoomDistribution[z];
            }

            var result = new JObject
            {
                ["avgTileCount"] = Math.Round(avgTileCount, 1),
                ["minTileCount"] = _minTileCount == int.MaxValue ? 0 : _minTileCount,
                ["maxTileCount"] = _maxTileCount,
                ["totalTileChanges"] = _totalTileChanges,
                ["lastZoomDistribution"] = zoomDist
            };

            return result;
        }

        public string Overlay()
        {
            if (!ShowOverlay || !_recording || _frameIndex == 0)
                return string.Empty;

            ref var f = ref _frames[_frameIndex - 1];
            return string.Format("Tiles: {0} (+{1}/-{2}) | Loaded: {3}",
                f.TileCount, f.TilesAdded, f.TilesRemoved, f.LoadedTileCount);
        }

        public void DumpToFile(string path)
        {
            var json = Export();
            File.WriteAllText(path, json.ToString());
            Debug.Log($"[TileStatsCollector] Stats saved to: {path}");
        }
    }
}
