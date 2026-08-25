using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.Filters;
using UnityEngine;

namespace Mapbox.LandmarkModule
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Landmark Polygon Filter")]
    public class LandmarkPolygonFilterObject : FilterBaseObject
    {
        private LandmarkPolygonFilter _filter;

        public override ILayerFeatureFilterComparer Filter
        {
            get
            {
                if (_filter == null)
                    _filter = new LandmarkPolygonFilter();
                return _filter;
            }
        }

        public void AddLandmarkPolygons(CanonicalTileId z14TileId, List<Vector3[]> polygons)
        {
            // Ensure the filter instance exists
            _ = Filter;
            _filter.AddLandmarkPolygons(z14TileId, polygons);
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
    public class LandmarkPolygonFilter : FilterBase
    {
        private readonly Dictionary<CanonicalTileId, LandmarkTileData> _landmarksAtZ14 =
            new Dictionary<CanonicalTileId, LandmarkTileData>();

        // Reusable buffer for transformed feature vertices, avoids per-call allocation
        private Vector3[] _transformBuffer = Array.Empty<Vector3>();

        public override bool Try(VectorFeatureUnity feature)
        {
            if (_landmarksAtZ14.Count == 0)
                return true;

            // IMPORTANT: CanonicalTileId is a struct and ParentAt() mutates it in place.
            // We must save the original tile ID before calling ParentAt, otherwise
            // the scale/offset calculation would use z14→z14 (identity) instead of
            // the correct zN→z14 transform.
            var featureTileId = feature.TileId;
            var z14Parent = new CanonicalTileId(featureTileId.Z, featureTileId.X, featureTileId.Y)
                .ParentAt(14);

            // Compute the scale/offset to transform feature coordinates to z14 space
            // For z14 features this is identity (scale=1, offset=0)
            // For z15 features scale=0.5, for z16 scale=0.25, etc.
            var scaleOffset = featureTileId.CalculateTopLeftScaleOffsetAtZoom(z14Parent.Z);

            // Look up landmark data for the parent z14 tile.
            // Neighbor tile coverage is handled by polygon replication in AddLandmarkPolygons —
            // polygons that extend beyond tile boundaries are copied into neighboring z14 tiles,
            // so a single lookup here is sufficient.
            if (!_landmarksAtZ14.TryGetValue(z14Parent, out var landmarkData))
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
            if (_transformBuffer.Length != vertices.Count)
                _transformBuffer = new Vector3[vertices.Count];

            var scaleX = scaleOffset[0];
            var scaleZ = scaleOffset[1];
            var offsetX = scaleOffset[2];
            var offsetZ = scaleOffset[3];

            for (int i = 0; i < vertices.Count; i++)
            {
                ref var target = ref _transformBuffer[i];
                target.x = vertices[i].x * scaleX + offsetX;
                target.y = vertices[i].y;
                target.z = vertices[i].z * scaleZ - offsetZ;
            }

            return _transformBuffer;
        }

        public void AddLandmarkPolygons(CanonicalTileId z14TileId, List<Vector3[]> polygons)
        {
            if (!_landmarksAtZ14.ContainsKey(z14TileId))
                _landmarksAtZ14[z14TileId] = new LandmarkTileData();

            var tileData = _landmarksAtZ14[z14TileId];
            foreach (var polygon in polygons)
            {
                tileData.Polygons.Add(polygon);
                tileData.BoundingBoxes.Add(MapboxGeometryUtilities.BoundsOfVertices(polygon));
                tileData.Sources.Add(z14TileId);
            }

            // Replicate polygons that extend beyond tile boundaries into neighbor tiles
            ReplicateToNeighbors(z14TileId, polygons);
        }

        /// <summary>
        /// Removes all polygons that originated from the given source tile,
        /// including replicated copies in neighbor tiles.
        /// </summary>
        public void RemoveLandmarkPolygonsFromSource(CanonicalTileId sourceTileId)
        {
            var tilesToRemove = new List<CanonicalTileId>();
            foreach (var pair in _landmarksAtZ14)
            {
                var tileData = pair.Value;
                for (int i = tileData.Sources.Count - 1; i >= 0; i--)
                {
                    if (tileData.Sources[i].Equals(sourceTileId))
                    {
                        tileData.Polygons.RemoveAt(i);
                        tileData.BoundingBoxes.RemoveAt(i);
                        tileData.Sources.RemoveAt(i);
                    }
                }

                if (tileData.Polygons.Count == 0)
                    tilesToRemove.Add(pair.Key);
            }

            foreach (var tile in tilesToRemove)
                _landmarksAtZ14.Remove(tile);
        }

        /// <summary>
        /// Replicates landmark polygons to neighboring z14 tiles when they extend beyond tile boundaries.
        /// Landmark footprints in 0-1 tile space can exceed these bounds near tile edges.
        /// Same pattern as LandmarkToBuildingComponentConnector.AddLandmarkData/AddPolyTo.
        /// </summary>
        private void ReplicateToNeighbors(CanonicalTileId originalId, List<Vector3[]> polygons)
        {
            foreach (var polygon in polygons)
            {
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
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X - 1, originalId.Y),
                        polygon);

                if (extendsRight)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X + 1, originalId.Y),
                        polygon);

                if (extendsTop)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X, originalId.Y - 1),
                        polygon);

                if (extendsBottom)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X, originalId.Y + 1),
                        polygon);

                // Diagonal neighbors — polygon extends beyond both X and Z boundaries
                if (extendsLeft && extendsTop)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X - 1, originalId.Y - 1),
                        polygon);

                if (extendsRight && extendsTop)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X + 1, originalId.Y - 1),
                        polygon);

                if (extendsLeft && extendsBottom)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X - 1, originalId.Y + 1),
                        polygon);

                if (extendsRight && extendsBottom)
                    AddPolyToNeighbor(originalId, new CanonicalTileId(originalId.Z, originalId.X + 1, originalId.Y + 1),
                        polygon);
            }
        }

        private void AddPolyToNeighbor(CanonicalTileId originalId, CanonicalTileId neighborId, Vector3[] polygon)
        {
            var dif = new Vector3(neighborId.X - originalId.X, 0, neighborId.Y - originalId.Y);
            var newPoly = new Vector3[polygon.Length];
            for (var i = 0; i < polygon.Length; i++)
            {
                newPoly[i] = new Vector3(polygon[i].x - dif.x, polygon[i].y, polygon[i].z + dif.z);
            }

            if (!_landmarksAtZ14.ContainsKey(neighborId))
                _landmarksAtZ14[neighborId] = new LandmarkTileData();

            _landmarksAtZ14[neighborId].Polygons.Add(newPoly);
            _landmarksAtZ14[neighborId].BoundingBoxes.Add(MapboxGeometryUtilities.BoundsOfVertices(newPoly));
            _landmarksAtZ14[neighborId].Sources.Add(originalId);
        }

        private class LandmarkTileData
        {
            public readonly List<Vector3[]> Polygons = new List<Vector3[]>();
            public readonly List<Bounds> BoundingBoxes = new List<Bounds>();
            public readonly List<CanonicalTileId> Sources = new List<CanonicalTileId>();
        }
    }
}
