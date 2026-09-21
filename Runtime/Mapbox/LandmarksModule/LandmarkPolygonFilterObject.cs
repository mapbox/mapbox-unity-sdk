using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.Filters;
using UnityEngine;

namespace Mapbox.LandmarksModule
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Landmark Polygon Filter")]
    public class LandmarkPolygonFilterObject : FilterBaseObject
    {
        // volatile + lock: the getter is reachable from mesh-generation worker threads (the filter
        // stack is evaluated inside VectorLayerModule's DataAction, which TaskManager runs on
        // Task.Run) as well as from the main thread, so an unsynchronised lazy create could hand two
        // callers two different filters and lose whichever set of polygons went into the loser.
        private volatile LandmarkPolygonFilter _filter;
        private readonly object _filterLock = new object();

        public override ILayerFeatureFilterComparer Filter => GetOrCreateFilter();

        private LandmarkPolygonFilter GetOrCreateFilter()
        {
            var filter = _filter;
            if (filter != null) return filter;

            lock (_filterLock)
            {
                return _filter ?? (_filter = new LandmarkPolygonFilter());
            }
        }

        public void AddLandmarkPolygons(CanonicalTileId z14TileId, List<Vector3[]> polygons)
        {
            GetOrCreateFilter().AddLandmarkPolygons(z14TileId, polygons);
        }

        public void RemoveLandmarkPolygonsFromSource(CanonicalTileId z14TileId)
        {
            _filter?.RemoveLandmarkPolygonsFromSource(z14TileId);
        }
    }

    /// <summary>
    /// Filter that rejects vector features whose geometry intersects with landmark footprint polygons.
    ///
    /// Landmark polygons are stored at z14 tile space. During Try(), each feature's geometry is
    /// transformed to z14 space using CalculateTopLeftScaleOffsetAtZoom for comparison.
    /// This avoids duplicating polygon data across zoom levels and works at any zoom.
    ///
    /// Performance: The per-feature cost is a bbox transform (2 multiply-adds) + bounds intersection
    /// check, which rejects 99%+ of features. Only features near a landmark pay for the full polygon
    /// transform and intersection test.
    /// </summary>
    internal class LandmarkPolygonFilter : FilterBase
    {
        /// <summary>
        /// Current polygon set, treated as immutable once published. Readers take the reference once
        /// and never look at the field again; writers build a replacement and swap it in.
        ///
        /// This is a real concurrency requirement, not caution. Try() runs on a mesh-generation
        /// worker thread - VectorLayerVisualizer evaluates the filter stack inside
        /// VectorLayerModule's DataAction, which TaskManager dispatches through Task.Run - while
        /// AddLandmarkPolygons and RemoveLandmarkPolygonsFromSource are called from the main thread
        /// as landmark tiles finish and are disposed. Structurally modifying a Dictionary or List
        /// while another thread enumerates it gives torn reads, InvalidOperationException, or an
        /// outright hang inside the dictionary.
        ///
        /// Copy-on-write rather than a lock because the ratio is lopsided: reads happen per feature
        /// per tile on several worker threads, writes once per landmark tile completion. Readers
        /// never block, and writers pay one dictionary copy plus a clone of only the tiles they
        /// touch.
        /// </summary>
        private volatile Dictionary<CanonicalTileId, LandmarkTileData> _landmarksAtZ14 =
            new Dictionary<CanonicalTileId, LandmarkTileData>();

        // Serialises writers against each other; readers never take it.
        private readonly object _writeLock = new object();

        // Scratch for transformed feature vertices. Per-thread rather than per-instance: several
        // mesh-generation tasks run at once and a single shared array had them overwriting each
        // other's vertices mid-comparison.
        [ThreadStatic] private static Vector3[] _transformBuffer;

        public override bool Try(VectorFeatureUnity feature)
        {
            // One read of the field; everything below works off this snapshot, so a concurrent
            // write cannot change the collection under us.
            var landmarks = _landmarksAtZ14;
            if (landmarks.Count == 0)
                return true;

            var featureTileId = feature.TileId;
            var z14Parent = featureTileId.ParentAt(LandmarksLayerModule.LandmarkDataZoom);

            // Compute the scale/offset to transform feature coordinates to z14 space
            // For z14 features this is identity (scale=1, offset=0)
            // For z15 features scale=0.5, for z16 scale=0.25, etc.
            var scaleOffset = featureTileId.CalculateTopLeftScaleOffsetAtZoom(z14Parent.Z);

            // Look up landmark data for the parent z14 tile.
            // Neighbor tile coverage is handled by polygon replication in AddLandmarkPolygons —
            // polygons that extend beyond tile boundaries are copied into neighboring z14 tiles,
            // so a single lookup here is sufficient.
            if (!landmarks.TryGetValue(z14Parent, out var landmarkData))
                return true;

            if (FeatureIntersectsLandmarks(feature, landmarkData, scaleOffset))
                return false;

            return true;
        }

        private bool FeatureIntersectsLandmarks(VectorFeatureUnity feature, LandmarkTileData landmarkData,
            Vector4 scaleOffset)
        {
            foreach (var submesh in feature.Points)
            {
                if (submesh.Count < 3)
                    continue;

                // Compute feature bbox in z14 space (fast: just transform min/max)
                var featureBbox = BoundsOfListTransformed(submesh, scaleOffset);

                for (var i = 0; i < landmarkData.Polygons.Count; i++)
                {
                    // Fast rejection: bbox check
                    if (!landmarkData.BoundingBoxes[i].Intersects(featureBbox))
                        continue;

                    // Separation heuristic
                    var separation =
                        MapboxGeometryUtilities.GetBoundsSeparation(landmarkData.BoundingBoxes[i], featureBbox);
                    if (separation > 0.001f)
                        continue;

                    // Full polygon intersection: transform feature vertices to z14 space
                    var transformedFeature = TransformToZ14(submesh, scaleOffset);

                    if (MapboxGeometryUtilities.PolygonsIntersect(landmarkData.Polygons[i], transformedFeature))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Computes a bounding box for a list of vertices after applying scale/offset transform to z14 space.
        /// Transforms every vertex and tracks min/max — required because scale/offset is uniform,
        /// but we still need to iterate all vertices to find the actual extents.
        /// </summary>
        private Bounds BoundsOfListTransformed(List<Vector3> vertices, Vector4 scaleOffset)
        {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            for (int i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                var tx = v.x * scaleOffset[0] + scaleOffset[2];
                var tz = v.z * scaleOffset[1] - scaleOffset[3];

                if (tx < minX) minX = tx;
                if (tx > maxX) maxX = tx;
                if (tz < minZ) minZ = tz;
                if (tz > maxZ) maxZ = tz;
            }

            var center = new Vector3((maxX + minX) / 2, 0, (maxZ + minZ) / 2);
            var size = new Vector3(maxX - minX, 1, maxZ - minZ);
            return new Bounds(center, size);
        }

        /// <summary>
        /// Transforms feature polygon vertices from their tile space to z14 space.
        /// Uses a reusable buffer to avoid per-call allocation.
        /// Only called when bbox check passes (rare).
        /// </summary>
        private Vector3[] TransformToZ14(List<Vector3> vertices, Vector4 scaleOffset)
        {
            var buffer = _transformBuffer;
            if (buffer == null || buffer.Length != vertices.Count)
            {
                buffer = new Vector3[vertices.Count];
                _transformBuffer = buffer;
            }

            var scaleX = scaleOffset[0];
            var scaleZ = scaleOffset[1];
            var offsetX = scaleOffset[2];
            var offsetZ = scaleOffset[3];

            for (int i = 0; i < vertices.Count; i++)
            {
                ref var target = ref buffer[i];
                target.x = vertices[i].x * scaleX + offsetX;
                target.y = vertices[i].y;
                target.z = vertices[i].z * scaleZ - offsetZ;
            }

            return buffer;
        }

        public void AddLandmarkPolygons(CanonicalTileId z14TileId, List<Vector3[]> polygons)
        {
            if (polygons == null || polygons.Count == 0) return;

            lock (_writeLock)
            {
                // Build a replacement rather than editing in place. Anything already published may
                // be mid-enumeration on a worker thread, so neither the dictionary nor a tile's
                // lists may be touched once they are visible - only the tiles this call actually
                // changes get cloned, the rest are shared by reference and stay immutable.
                var next = new Dictionary<CanonicalTileId, LandmarkTileData>(_landmarksAtZ14);

                foreach (var polygon in polygons)
                {
                    if (polygon == null || polygon.Length == 0) continue;
                    AppendPolygon(next, z14TileId, z14TileId, polygon);
                }

                // Replicate polygons that extend beyond tile boundaries into neighbor tiles
                ReplicateToNeighbors(next, z14TileId, polygons);

                _landmarksAtZ14 = next;
            }
        }

        /// <summary>
        /// Removes all polygons that originated from the given source tile,
        /// including replicated copies in neighbor tiles.
        /// </summary>
        public void RemoveLandmarkPolygonsFromSource(CanonicalTileId sourceTileId)
        {
            lock (_writeLock)
            {
                var current = _landmarksAtZ14;
                Dictionary<CanonicalTileId, LandmarkTileData> next = null;

                foreach (var pair in current)
                {
                    var tileData = pair.Value;
                    if (!tileData.HasSource(sourceTileId)) continue;

                    // First affected tile: only now is a copy worth making.
                    next = next ?? new Dictionary<CanonicalTileId, LandmarkTileData>(current);

                    var trimmed = tileData.WithoutSource(sourceTileId);
                    if (trimmed.Polygons.Count == 0)
                    {
                        next.Remove(pair.Key);
                    }
                    else
                    {
                        next[pair.Key] = trimmed;
                    }
                }

                if (next != null) _landmarksAtZ14 = next;
            }
        }

        /// <summary>
        /// Adds one polygon to a tile in the pending set, cloning that tile's entry first so the
        /// version already published to readers is left untouched.
        /// </summary>
        private static void AppendPolygon(Dictionary<CanonicalTileId, LandmarkTileData> pending,
            CanonicalTileId tileId, CanonicalTileId sourceId, Vector3[] polygon)
        {
            var tileData = pending.TryGetValue(tileId, out var existing)
                ? existing.Clone()
                : new LandmarkTileData();

            tileData.Polygons.Add(polygon);
            tileData.BoundingBoxes.Add(MapboxGeometryUtilities.BoundsOfVertices(polygon));
            tileData.Sources.Add(sourceId);

            pending[tileId] = tileData;
        }

        private static void ReplicateToNeighbors(Dictionary<CanonicalTileId, LandmarkTileData> pending,
            CanonicalTileId originalId, List<Vector3[]> polygons)
        {
            foreach (var polygon in polygons)
            {
                if (polygon == null || polygon.Length == 0) continue;

                var minX = float.MaxValue;
                var maxX = float.MinValue;
                var minZ = float.MaxValue;
                var maxZ = float.MinValue;

                for (var i = 0; i < polygon.Length; i++)
                {
                    if (polygon[i].x < minX) minX = polygon[i].x;
                    if (polygon[i].x > maxX) maxX = polygon[i].x;
                    if (polygon[i].z < minZ) minZ = polygon[i].z;
                    if (polygon[i].z > maxZ) maxZ = polygon[i].z;
                }

                var extendsLeft = minX < 0;
                var extendsRight = maxX > 1;
                var extendsTop = maxZ > 0;
                var extendsBottom = minZ < -1;

                // Cardinal neighbors
                if (extendsLeft)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X - 1, originalId.Y),
                        polygon);

                if (extendsRight)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X + 1, originalId.Y),
                        polygon);

                if (extendsTop)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X, originalId.Y - 1),
                        polygon);

                if (extendsBottom)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X, originalId.Y + 1),
                        polygon);

                // Diagonal neighbors — polygon extends beyond both X and Z boundaries
                if (extendsLeft && extendsTop)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X - 1, originalId.Y - 1),
                        polygon);

                if (extendsRight && extendsTop)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X + 1, originalId.Y - 1),
                        polygon);

                if (extendsLeft && extendsBottom)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X - 1, originalId.Y + 1),
                        polygon);

                if (extendsRight && extendsBottom)
                    AddPolyToNeighbor(pending, originalId, new CanonicalTileId(originalId.Z, originalId.X + 1, originalId.Y + 1),
                        polygon);
            }
        }

        private static void AddPolyToNeighbor(Dictionary<CanonicalTileId, LandmarkTileData> pending,
            CanonicalTileId originalId, CanonicalTileId neighborId, Vector3[] polygon)
        {
            var dif = new Vector3(neighborId.X - originalId.X, 0, neighborId.Y - originalId.Y);
            var newPoly = new Vector3[polygon.Length];
            for (var i = 0; i < polygon.Length; i++)
            {
                newPoly[i] = new Vector3(polygon[i].x - dif.x, polygon[i].y, polygon[i].z + dif.z);
            }

            AppendPolygon(pending, neighborId, originalId, newPoly);
        }

        /// <summary>
        /// Polygons for one z14 tile. Mutated only while still private to a writer; once stored in a
        /// published dictionary it is treated as immutable, which is what lets readers enumerate it
        /// without locking.
        /// </summary>
        private class LandmarkTileData
        {
            public readonly List<Vector3[]> Polygons = new List<Vector3[]>();
            public readonly List<Bounds> BoundingBoxes = new List<Bounds>();
            public readonly List<CanonicalTileId> Sources = new List<CanonicalTileId>();

            public LandmarkTileData Clone()
            {
                var copy = new LandmarkTileData();
                copy.Polygons.AddRange(Polygons);
                copy.BoundingBoxes.AddRange(BoundingBoxes);
                copy.Sources.AddRange(Sources);
                return copy;
            }

            public bool HasSource(CanonicalTileId sourceId)
            {
                for (var i = 0; i < Sources.Count; i++)
                {
                    if (Sources[i].Equals(sourceId)) return true;
                }

                return false;
            }

            /// <summary>A copy with every entry contributed by the given source tile left out.</summary>
            public LandmarkTileData WithoutSource(CanonicalTileId sourceId)
            {
                var copy = new LandmarkTileData();
                for (var i = 0; i < Sources.Count; i++)
                {
                    if (Sources[i].Equals(sourceId)) continue;
                    copy.Polygons.Add(Polygons[i]);
                    copy.BoundingBoxes.Add(BoundingBoxes[i]);
                    copy.Sources.Add(Sources[i]);
                }

                return copy;
            }
        }
    }
}
