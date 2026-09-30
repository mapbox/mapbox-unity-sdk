using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarksModule.Unity;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.ComponentSystem.Data;
using UnityEngine;

namespace Mapbox.LandmarksModule
{
public class LandmarkToBuildingComponentConnector : MonoBehaviour
{
    [Tooltip("Map whose initialization gates the wiring below. Required - leave it unassigned and this connector does nothing.")]
    public MapBehaviourCore Map;

    [Tooltip("Landmarks module supplying the footprints to test against. Required.")]
    public LandmarksLayerModuleScript LandmarksLayerModuleScript;

    [Tooltip("Building visualizer whose meshes get their overlapping vertices collapsed. Required.")]
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

        if (Map == null || LandmarksLayerModuleScript == null || BuildingLayerVisualizerObject == null)
        {
            Debug.LogWarning($"{nameof(LandmarkToBuildingComponentConnector)} needs Map, Landmarks Layer Module Script and Building Layer Visualizer Object assigned. Landmark/building overlap suppression is disabled.", this);
            enabled = false;
            return;
        }

        // Named method rather than a lambda so OnDestroy can detach it. As a closure it could not
        // be unsubscribed, so destroying this component before the map initialized left the body to
        // run against a dead component.
        Map.Initialized += OnMapInitialized;
    }

    private void OnMapInitialized(MapboxMap mapboxMap)
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
    }

    private void OnDestroy()
    {
        if (Map != null) Map.Initialized -= OnMapInitialized;

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
        var buildingAtZ14 = tileId.ParentAt(LandmarksLayerModule.LandmarkDataZoom);

        // Check against landmarks in the parent z14 tile.
        // Neighbor tile coverage is handled by landmark-side polygon replication in AddLandmarkData —
        // polygons reaching within NeighbourReplicationMargin of a tile boundary are copied into the
        // neighboring z14 tiles on that side, so a single lookup here is sufficient even for a
        // building whose buffer overhang pokes into the next tile.
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

            // Walk the buildings and ask which z14 tile each belongs to, rather than enumerating
            // the z14 tile's descendants and probing for them. The descendant enumeration only
            // reached z15 and z16, so a building rendered at z17 or deeper - or at z14 itself - was
            // never revisited when its landmark arrived later, and stayed visible through the
            // landmark. Extending the enumeration is the wrong shape anyway: it grows 4^depth (256
            // ids at z18, 1024 at z19), whereas this is bounded by the buildings actually loaded,
            // and needs no assumption about render zoom at all.
            foreach (var pair in _buildingsPerTile)
            {
                if (!IsUnderLandmarkTile(pair.Key, landmarkData.TileId)) continue;
                HandleCollision(pair.Value, landmarkData);
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
        var hidAnything = false;

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
                    hidAnything = true;
                    break;
                }
            }
        }

        // Only re-upload when something actually moved. This runs for every (building,
        // landmark tile) pair, and the overwhelmingly common outcome is that nothing intersects -
        // pushing the whole vertex buffer back to the mesh in that case re-uploaded every retained
        // building on every landmark tile completion for no change at all.
        if (!hidAnything) return;

        // The visualizer may have pooled or destroyed this building between the mesh being
        // registered and this landmark tile finishing; OnBuildingMeshDestroyed only unhooks the
        // exact tile id, so the entry can outlive the GameObject.
        if (buildingData.GameObject == null) return;

        var meshFilter = buildingData.GameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null) return;

        // sharedMesh, not mesh. Reading MeshFilter.mesh makes Unity duplicate sharedMesh, hand the
        // filter the copy and leak it - nothing destroys it - while the pooled mesh the visualizer
        // owns is orphaned until the entity is recycled and reassigns it. That is one leaked Mesh per
        // building that overlaps a landmark, and the old null check leaked one just by asking.
        // Writing sharedMesh is safe here because each pooled VectorEntity gets its own
        // `new Mesh()` from VectorEntityGenerator, so it is not shared between buildings.
        meshFilter.sharedMesh.SetVertices(meshData.Vertices);
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
        const float margin = LandmarksLayerModule.NeighbourReplicationMargin;

        var changedTiles = new HashSet<CanonicalTileId>();
        foreach (var polygon in polygons)
        {
            var bbox = MapboxGeometryUtilities.BoundsOfVertices(polygon);

            if (!_landmarksPerTile.ContainsKey(id)) _landmarksPerTile.Add(id, new LandmarkData(id));
            _landmarksPerTile[id].BoundingBoxes.Add(bbox);
            _landmarksPerTile[id].Polygons.Add(polygon);
            _landmarksPerTile[id].Sources.Add(id);
            changedTiles.Add(id);

            // Within the margin of an edge, not only past it: a neighbour's buildings overhang into
            // this tile by the vector tile buffer, and OnBuildingMeshCreated only looks a building up
            // under its own z14 parent. See NeighbourReplicationMargin.
            var extendsLeft = bbox.min.x < margin;
            var extendsRight = bbox.max.x > 1 - margin;
            var extendsTop = bbox.max.z > -margin;
            var extendsBottom = bbox.min.z < -1 + margin;

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

            // Diagonal neighbors — polygon reaches both an X and a Z boundary
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

    /// <summary>
    /// Whether a building tile sits inside the given z14 landmark tile, at any render zoom.
    /// </summary>
    private static bool IsUnderLandmarkTile(CanonicalTileId buildingTileId, CanonicalTileId landmarkTileId)
    {
        if (buildingTileId.Z < landmarkTileId.Z) return false;

        return buildingTileId.ParentAt(landmarkTileId.Z).Equals(landmarkTileId);
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
}
