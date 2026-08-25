using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarkModule;
using Mapbox.LandmarkModule.Unity;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.ComponentSystem.Data;
using UnityEngine;

public class LandmarkToBuildingComponentConnector : MonoBehaviour
{
    public MapBehaviourCore Map;
    public LandmarksLayerModuleScript LandmarksLayerModuleScript;
    public BuildingComponentVisualizerObject BuildingLayerVisualizerObject;

    private Dictionary<CanonicalTileId, BuildingData> _buildingsPerTile;
    private Dictionary<CanonicalTileId, LandmarkData> _landmarksPerTile;

    // Reusable buffer for transformed footprint vertices, avoids per-call allocation
    private Vector3[] _transformBuffer = Array.Empty<Vector3>();

    private LandmarksLayerModule _landmarksLayerModule;
    private BuildingComponentVisualizer _buildingVisualizer;

    private void Awake()
    {
        _buildingsPerTile = new Dictionary<CanonicalTileId, BuildingData>();
        _landmarksPerTile = new Dictionary<CanonicalTileId, LandmarkData>();

        Map.Initialized += _ =>
        {
            _landmarksLayerModule = (LandmarksLayerModule)LandmarksLayerModuleScript.ModuleImplementation;
            _landmarksLayerModule.TileFinished += LandmarkFootprintResults;
            _landmarksLayerModule.TileDisposed += OnLandmarkTileDisposed;

            _buildingVisualizer = (BuildingComponentVisualizer)BuildingLayerVisualizerObject.GetLayerVisualizer();
            if (_buildingVisualizer != null)
            {
                _buildingVisualizer.OnBuildingMeshCreated += OnBuildingMeshCreated;
                _buildingVisualizer.OnBuildingMeshDestroyed += OnBuildingMeshDestroyed;
            }
        };
    }

    private void OnDestroy()
    {
        if (_landmarksLayerModule != null)
        {
            _landmarksLayerModule.TileFinished -= LandmarkFootprintResults;
            _landmarksLayerModule.TileDisposed -= OnLandmarkTileDisposed;
        }

        if (_buildingVisualizer != null)
        {
            _buildingVisualizer.OnBuildingMeshCreated -= OnBuildingMeshCreated;
            _buildingVisualizer.OnBuildingMeshDestroyed -= OnBuildingMeshDestroyed;
        }
    }

    private void OnBuildingMeshCreated(CanonicalTileId tileId, GameObject go, MeshData meshData)
    {
        var buildingData = new BuildingData() { TileId = tileId, GameObject = go, MeshData = meshData };
        _buildingsPerTile[tileId] = buildingData;

        // Get the z14 parent tile where this building belongs
        var buildingAtZ14 = tileId.ParentAt(14);

        // Check against landmarks in the parent z14 tile.
        // Neighbor tile coverage is handled by landmark-side polygon replication in AddLandmarkData —
        // polygons extending beyond tile boundaries are copied into all 8 neighboring z14 tiles,
        // so a single lookup here is sufficient.
        if (_landmarksPerTile.TryGetValue(buildingAtZ14, out var landmarkData))
        {
            HandleCollision(buildingData, landmarkData);
        }
    }

    private void OnBuildingMeshDestroyed(CanonicalTileId tileId, GameObject gameObject)
    {
        _buildingsPerTile.Remove(tileId);
    }

    private void OnLandmarkTileDisposed(CanonicalTileId z14TileId)
    {
        var tilesToRemove = new List<CanonicalTileId>();
        foreach (var pair in _landmarksPerTile)
        {
            var landmarkData = pair.Value;
            for (int i = landmarkData.Sources.Count - 1; i >= 0; i--)
            {
                if (landmarkData.Sources[i].Equals(z14TileId))
                {
                    landmarkData.Polygons.RemoveAt(i);
                    landmarkData.BoundingBoxes.RemoveAt(i);
                    landmarkData.Sources.RemoveAt(i);
                }
            }

            if (landmarkData.Polygons.Count == 0)
                tilesToRemove.Add(pair.Key);
        }

        foreach (var tile in tilesToRemove)
            _landmarksPerTile.Remove(tile);
    }

    private void LandmarkFootprintResults(CanonicalTileId id, List<Vector3[]> polygons)
    {
        var tiles = AddLandmarkData(id, polygons);

        foreach (var tile in tiles)
        {
            var landmarkData = _landmarksPerTile[tile];
            foreach (var tileId in AllChildrenOf14(landmarkData.TileId))
            {
                if (_buildingsPerTile.TryGetValue(tileId, out var buildingData))
                {
                    HandleCollision(buildingData, landmarkData);
                }
            }
        }
    }

    /// <summary>
    /// Main collision detection method between building mesh and landmark polygons.
    /// Uses a multi-pass approach: fast bbox check first, then detailed polygon intersection.
    /// </summary>
    private void HandleCollision(BuildingData buildingData, LandmarkData landmarkData)
    {
        var meshData = buildingData.MeshData;

        for (var i = 0; i < meshData.MeshInfo.vertexRanges.Length; i++)
        {
            var vertexSpan = meshData.Vertices.AsSpan(meshData.MeshInfo.vertexRanges[i], meshData.MeshInfo.vertexSize[i]);
            var scaleOffsetForBuilding = buildingData.TileId.CalculateTopLeftScaleOffsetAtZoom(landmarkData.TileId.Z);
            var featureBox = MapboxGeometryUtilities.BoundsOfVertices(vertexSpan, scaleOffsetForBuilding);

            for (var landmarkIndex = 0; landmarkIndex < landmarkData.BoundingBoxes.Count; landmarkIndex++)
            {
                // FIRST PASS: Quick bounding box intersection test (precomputed Bounds, no recomputation)
                if (!landmarkData.BoundingBoxes[landmarkIndex].Intersects(featureBox))
                    continue;

                var separation = MapboxGeometryUtilities.GetBoundsSeparation(landmarkData.BoundingBoxes[landmarkIndex], featureBox);
                if (separation > 0.001f)
                    continue;

                // SECOND PASS: Extract footprint vertices
                var footprintVertexCount = meshData.MeshInfo.vertexSize[i] / 5;
                if (footprintVertexCount < 3)
                    continue;

                var buildingFootprint = vertexSpan.Slice(0, footprintVertexCount);

                var complexityRatio = (float)landmarkData.Polygons[landmarkIndex].Length / footprintVertexCount;
                if ((complexityRatio > 5.0f || complexityRatio < 0.2f) && separation > 0.0001f)
                    continue;

                // Transform building footprint to z14 space using reusable buffer
                var transformedBuildingFootprint = TransformVerticesToLandmarkSpace(buildingFootprint, scaleOffsetForBuilding);

                // THIRD PASS: Detailed polygon intersection test
                if (MapboxGeometryUtilities.PolygonsIntersect(landmarkData.Polygons[landmarkIndex], transformedBuildingFootprint))
                {
                    HideBuildingVertices(vertexSpan);
                    break;
                }
            }
        }

        buildingData.GameObject.GetComponent<MeshFilter>().mesh.SetVertices(meshData.Vertices);
    }

    /// <summary>
    /// Transforms vertices from building tile space to landmark tile space (z14).
    /// Uses a reusable buffer to avoid per-call allocation.
    /// </summary>
    private Vector3[] TransformVerticesToLandmarkSpace(Span<Vector3> vertices, Vector4 scaleOffset)
    {
        if (_transformBuffer.Length != vertices.Length)
            _transformBuffer = new Vector3[vertices.Length];

        var scaleX = scaleOffset[0];
        var scaleZ = scaleOffset[1];
        var offsetX = scaleOffset[2];
        var offsetZ = scaleOffset[3];

        for (int i = 0; i < vertices.Length; i++)
        {
            ref var target = ref _transformBuffer[i];
            target.x = vertices[i].x * scaleX + offsetX;
            target.y = vertices[i].y;
            target.z = vertices[i].z * scaleZ - offsetZ;
        }

        return _transformBuffer;
    }

    private void HideBuildingVertices(Span<Vector3> vertexSpan)
    {
        for (int j = 0; j < vertexSpan.Length; j++)
        {
            vertexSpan[j] = Vector3.zero;
        }
    }

    private IEnumerable<CanonicalTileId> AddLandmarkData(CanonicalTileId id, List<Vector3[]> polygons)
    {
        var changedTiles = new HashSet<CanonicalTileId>();
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

            var bbox = MapboxGeometryUtilities.BoundsOfVertices(polygon);

            if (!_landmarksPerTile.ContainsKey(id)) _landmarksPerTile.Add(id, new LandmarkData(id));
            _landmarksPerTile[id].BoundingBoxes.Add(bbox);
            _landmarksPerTile[id].Polygons.Add(polygon);
            _landmarksPerTile[id].Sources.Add(id);
            changedTiles.Add(id);

            var extendsLeft = minX < 0;
            var extendsRight = maxX > 1;
            var extendsTop = maxZ > 0;
            var extendsBottom = minZ < -1;

            // Cardinal neighbors
            CanonicalTileId neighbourId;
            if (extendsLeft)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X - 1, id.Y);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            if (extendsRight)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X + 1, id.Y);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            if (extendsTop)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X, id.Y - 1);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            if (extendsBottom)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X, id.Y + 1);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            // Diagonal neighbors — polygon extends beyond both X and Z boundaries
            if (extendsLeft && extendsTop)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X - 1, id.Y - 1);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            if (extendsRight && extendsTop)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X + 1, id.Y - 1);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            if (extendsLeft && extendsBottom)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X - 1, id.Y + 1);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }

            if (extendsRight && extendsBottom)
            {
                neighbourId = new CanonicalTileId(id.Z, id.X + 1, id.Y + 1);
                AddPolyTo(id, neighbourId, polygon);
                changedTiles.Add(neighbourId);
            }
        }

        return changedTiles;
    }

    private void AddPolyTo(CanonicalTileId originalId, CanonicalTileId neighbourId, Vector3[] polygon)
    {
        var dif = new Vector3(neighbourId.X - originalId.X, 0, neighbourId.Y - originalId.Y);
        var newPoly = new Vector3[polygon.Length];
        for (var i = 0; i < polygon.Length; i++)
        {
            newPoly[i] = new Vector3(polygon[i].x - dif.x, polygon[i].y, polygon[i].z + dif.z);
        }

        if (!_landmarksPerTile.ContainsKey(neighbourId))
            _landmarksPerTile.Add(neighbourId, new LandmarkData(neighbourId));

        _landmarksPerTile[neighbourId].Polygons.Add(newPoly);
        _landmarksPerTile[neighbourId].BoundingBoxes.Add(MapboxGeometryUtilities.BoundsOfVertices(newPoly));
        _landmarksPerTile[neighbourId].Sources.Add(originalId);
    }

    private IEnumerable<CanonicalTileId> AllChildrenOf14(CanonicalTileId tileId)
    {
        for (int i = 0; i < 4; i++)
        {
            var child = tileId.Quadrant(i);
            yield return child;

            for (int j = 0; j < 4; j++)
            {
                var grandchild = child.Quadrant(j);
                yield return grandchild;
            }
        }
    }

    private class BuildingData
    {
        public CanonicalTileId TileId;
        public GameObject GameObject;
        public MeshData MeshData;
    }

    private class LandmarkData
    {
        public CanonicalTileId TileId;
        public List<Bounds> BoundingBoxes;
        public List<Vector3[]> Polygons;
        public List<CanonicalTileId> Sources;

        public LandmarkData(CanonicalTileId id)
        {
            TileId = id;
            BoundingBoxes = new List<Bounds>();
            Polygons = new List<Vector3[]>();
            Sources = new List<CanonicalTileId>();
        }
    }
}
