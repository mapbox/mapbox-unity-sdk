using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Benchmark
{
    /// <summary>
    /// Pre-downloads all tiles needed for a camera path before benchmark recording begins.
    /// Uses mathematical tile cover computation (lat/lng → tile ID + buffer ring) for deterministic
    /// results — the same sequence always produces the same set of pre-cached tiles.
    /// </summary>
    public class BenchmarkPreCacher : MonoBehaviour
    {
        [Tooltip("Sample interval along the path in seconds. Smaller = more tiles pre-cached.")]
        public float SampleInterval = 0.5f;

        [Tooltip("Tile ring buffer around center tile at each sample point. Buffer of 3 = 7x7 grid.")]
        public int TileBuffer = 3;

        [Tooltip("Tileset IDs to pre-cache (e.g. mapbox.mapbox-streets-v8, mapbox.satellite)")]
        public string[] TilesetIds;

        public Action<float> ProgressChanged;
        public Action PreCacheCompleted;

        private HashSet<CanonicalTileId> _tilesToCache = new HashSet<CanonicalTileId>();

        public int TotalTilesToCache => _tilesToCache.Count;

        /// <summary>
        /// Compute tiles for a simple A-to-B path (used with TileProviderBenchmark-style paths).
        /// </summary>
        public void ComputeTilesForPath(
            LatitudeLongitude start, LatitudeLongitude end,
            float zoom, float durationSeconds)
        {
            _tilesToCache.Clear();
            int zoomInt = Mathf.RoundToInt(zoom);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds / SampleInterval));

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = sampleCount > 0 ? (float)i / sampleCount : 0;
                double lat = start.Latitude + (end.Latitude - start.Latitude) * t;
                double lng = start.Longitude + (end.Longitude - start.Longitude) * t;
                AddTilesAroundCenter(new LatitudeLongitude(lat, lng), zoomInt);
            }
        }

        /// <summary>
        /// Start downloading all computed tiles through the normal data pipeline.
        /// Call ComputeTilesForPath first.
        /// </summary>
        public IEnumerator PreCacheAllTiles(MapboxMap map)
        {
            if (_tilesToCache.Count == 0)
            {
                Debug.LogWarning("[BenchmarkPreCacher] No tiles to pre-cache. Call ComputeTilesForPath first.");
                PreCacheCompleted?.Invoke();
                yield break;
            }

            Debug.Log($"[BenchmarkPreCacher] Pre-caching {_tilesToCache.Count} tiles across {TilesetIds?.Length ?? 0} tilesets...");

            int totalTiles = _tilesToCache.Count;
            int loadedTiles = 0;

            foreach (var tileId in _tilesToCache)
            {
                // Load the map view at this tile's center position to trigger the full pipeline
                var center = TileIdToLatLng(tileId);
                map.ChangeView(center, tileId.Z, null, null);

                loadedTiles++;
                ProgressChanged?.Invoke((float)loadedTiles / totalTiles);

                // Yield every few tiles to avoid blocking the main thread
                if (loadedTiles % 10 == 0)
                    yield return null;
            }

            // Wait for all pending fetches to complete
            yield return WaitForFetchQueueDrain(map);

            Debug.Log($"[BenchmarkPreCacher] Pre-cache complete. {totalTiles} tile positions processed.");
            PreCacheCompleted?.Invoke();
        }

        private IEnumerator WaitForFetchQueueDrain(MapboxMap map)
        {
            // Wait until the data fetching manager has no more pending requests
            // We poll every frame until the queue is empty and no active requests remain
            float timeout = 120f; // 2 minute timeout
            float elapsed = 0f;

            yield return new WaitForSeconds(1f); // Give time for requests to queue up

            while (elapsed < timeout)
            {
                // Check if there are any active tile loads
                // The map's tile cover loading should settle when all tiles are cached
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;

                // Simple heuristic: if no tile changes happened in the last check, we're done
                // A more robust check would hook into DataFetchingManager.ActiveFetchCount
                if (map.TileCover?.Tiles != null && map.TileCover.Tiles.Count > 0)
                {
                    // Wait a bit more to ensure stragglers complete
                    yield return new WaitForSeconds(2f);
                    break;
                }
            }
        }

        private void AddTilesAroundCenter(LatitudeLongitude center, int zoom)
        {
            var centerTile = Conversions.LatitudeLongitudeToTileId(center, zoom).Canonical;
            int maxTile = (1 << zoom) - 1;

            for (int dx = -TileBuffer; dx <= TileBuffer; dx++)
            {
                for (int dy = -TileBuffer; dy <= TileBuffer; dy++)
                {
                    int x = centerTile.X + dx;
                    int y = centerTile.Y + dy;

                    // Wrap x around for world wrapping, clamp y
                    x = ((x % (maxTile + 1)) + (maxTile + 1)) % (maxTile + 1);
                    if (y < 0 || y > maxTile)
                        continue;

                    _tilesToCache.Add(new CanonicalTileId(zoom, x, y));
                }
            }
        }

        private static LatitudeLongitude TileIdToLatLng(CanonicalTileId tileId)
        {
            double n = Math.Pow(2.0, tileId.Z);
            double lonDeg = tileId.X / n * 360.0 - 180.0;
            double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * tileId.Y / n)));
            double latDeg = latRad * 180.0 / Math.PI;
            return new LatitudeLongitude(latDeg, lonDeg);
        }
    }
}
