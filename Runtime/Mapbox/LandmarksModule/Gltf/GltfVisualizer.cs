using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Logging;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarksModule;
using Mapbox.LandmarksModule.FeatureId;
using UnityEngine;
using Material = UnityEngine.Material;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Mapbox.LandmarkModule
{
    public class GltfVisualizer
    {
        // public Action<CanonicalTileId, List<Mesh>> LandmarkMeshResults = (id, results) => { };
        // public Action<CanonicalTileId, List<Vector3[]>> LandmarkFootprintCreated = (id, results) => { };
        
        private Material _baseMaterial;
        private Dictionary<CanonicalTileId, TaskWrapper> _activeTasks;
        private ObjectPool<GameObject> _objectPool;
        private Transform _gltfRoot;
        private IMapInformation _mapInformation;
        

        public bool IsInWork(CanonicalTileId tileId) { return _activeTasks.ContainsKey(tileId); }
		
        public GltfVisualizer(UnityContext unityContext, IMapInformation mapInformation, Material baseMaterial)
        {
            _mapInformation = mapInformation;
            _baseMaterial = baseMaterial;
            _activeTasks = new Dictionary<CanonicalTileId, TaskWrapper>();
            _objectPool = new ObjectPool<GameObject>(PoolObjectGenerator);
            var go = new GameObject("gltf root");
            go.transform.SetParent(unityContext.RuntimeGenerationRoot);
            _gltfRoot = go.transform;
        }
        
        public IEnumerator Initialize()
        {
            return _objectPool.InitializeItems(20);
        }
		
        public async Task Generate(BuildingData vectorData, Action<GltfGenerationTaskResult> callback)
        {
            if (_activeTasks.ContainsKey(vectorData.TileId))
            {
                // Generation is already running for this tile, started by an earlier call, and
                // that call reports the real outcome to its own callback. Previously this
                // returned without invoking the callback at all, so the second caller was told
                // nothing whatsoever and silently carried on as if it had never asked.
                // AlreadyInProgress at least says which of the two it is: nothing to start, and
                // nothing ready yet either.
                callback(new GltfGenerationTaskResult(TaskResultType.AlreadyInProgress));
                return;
            }

            if (vectorData.Data == null || vectorData.Data.Length == 0)
            {
                // An empty tile, not a failure - most tiles simply hold no landmarks. Reported
                // as NoData rather than Success so a caller can tell "there is nothing here" from
                // "here is a building", which Success plus an empty collection could not express.
                // The empty collection is kept rather than left null because consumers iterate
                // cached visuals without null-checking them.
                callback(new GltfGenerationTaskResult(TaskResultType.NoData, Enumerable.Empty<GameObject>()));
                return;
            }

            _activeTasks.Add(vectorData.TileId, null);

            var gltf = new MapboxGltfImport(null, null, new MapboxGltfMaterialGenerator(_baseMaterial), new ConsoleLogger());
            var buildingRoot = _objectPool.GetObject(); //new GameObject("building " + vectorData.TileId);

            FeatureIdColorTable featureIds = null;
            MapboxGltfInstantiator gameObjectInstantiator = null;
            var loaded = false;
            var success = false;

            try
            {
                loaded = await gltf.Load(vectorData.Data);
                if (loaded)
                {
                    // Feature IDs are decoded from the tile bytes we already hold, independently of
                    // glTFast's load-time buffers, which are released once Load completes.
                    featureIds = FeatureIdColorTable.Build(gltf.GetSourceRoot(), vectorData.Data);

                    gameObjectInstantiator =
                        new MapboxGltfInstantiator(gltf, _mapInformation, buildingRoot.transform, featureIds);

                    success = await gltf.InstantiateSceneAsync(gameObjectInstantiator, 0);
                }
            }
            catch (Exception e)
            {
                loaded = false;
                Debug.LogError($"Landmark tile {vectorData.TileId} failed to generate: {e}");
            }
            finally
            {
                // Decompressed bytes are held only until instantiation has consumed them.
                featureIds?.Dispose();

                // Always clear the in-work marker. Leaving it set would make IsInWork report true
                // for the rest of the session, so the tile would never be retried.
                _activeTasks.Remove(vectorData.TileId);
            }

            // Dispatched outside the try so a throwing callback cannot re-enter the failure paths.
            if (loaded && success)
            {
                //LandmarkMeshResults(vectorData.TileId, footPrints);
                callback(new GltfGenerationTaskResult(TaskResultType.Success, new List<GameObject>() {buildingRoot}, gameObjectInstantiator.FootprintResults));
                return;
            }

            // A non-null instantiator means instantiation was attempted and may have parented
            // partial geometry, so that root is not safe to pool. Destroy it, as the module does
            // when releasing tile visuals. An untouched root goes back to the pool instead, so
            // repeated load failures do not drain the pre-warmed pool.
            if (gameObjectInstantiator == null)
            {
                _objectPool.Put(buildingRoot);
            }
            else
            {
                GameObject.Destroy(buildingRoot);
            }

            callback(new GltfGenerationTaskResult(
                loaded ? TaskResultType.GameObjectFailure : TaskResultType.DataProcessingFailure,
                Enumerable.Empty<GameObject>()));
        }
        
        

        private GameObject PoolObjectGenerator()
        {
            var go = new GameObject("gltf visualizer");
            go.transform.SetParent(_gltfRoot);
            return go;
        }
        
        private class MapboxGltfInstantiator : GameObjectInstantiator
        {
            public List<Vector3[]> FootprintResults = new List<Vector3[]>();
            private MapboxGltfImport _gltf;
            private IMapInformation _mapInformation;
            private FeatureIdColorTable _featureIds;

            public MapboxGltfInstantiator(MapboxGltfImport gltf, IMapInformation mapInformation, Transform parent, FeatureIdColorTable featureIds = null, ICodeLogger logger = null, InstantiationSettings settings = null) : base(gltf, parent, logger, settings)
            {
                _gltf = gltf;
                _mapInformation = mapInformation;
                _featureIds = featureIds;
            }

            public override void AddPrimitive(uint nodeIndex, string meshName, MeshResult meshResult, uint[] joints = null, uint? rootJoint = null,
                float[] morphTargetWeights = null, int meshNumeration = 0)
            {
                base.AddPrimitive(nodeIndex, meshName, meshResult, joints, rootJoint, morphTargetWeights, meshNumeration);
                var node = _gltf.GetSourceRoot().nodes[nodeIndex];

                FlipYZ(node.matrix, meshResult);

                // Written after FlipYZ, which rewrites the mesh's vertex buffers.
                FeatureIdMeshWriter.Apply(
                    meshResult.mesh, meshResult.meshIndex, meshResult.primitiveIndices, _featureIds);

                // Footprints are collected here rather than in EndScene for three reasons:
                // AddPrimitive is called once per primitive, so a footprint node with more than
                // one primitive contributes all of them; it is called for every node with a mesh
                // at any depth, so a footprint nested below a root node is not missed; and it runs
                // after FlipYZ, so the vertices are already in tile-local space.
                if (IsFootprint(node))
                {
                    MapboxGeometryUtilities.ExtractBoundaryRings(meshResult.mesh, FootprintResults);
                }
            }

            public override void EndScene(uint[] rootNodeIndices)
            {
                // FootprintResults is filled during AddPrimitive, so nothing is gathered here and
                // the list must not be cleared. This pass only hides the footprint geometry and
                // normalises the transforms of the root nodes that remain visible.
                if (rootNodeIndices == null) return;

                var sourceRoot = _gltf.GetSourceRoot();
                foreach (var nodeIndex in rootNodeIndices)
                {
                    var node = sourceRoot.nodes[nodeIndex];
                    var go = m_Nodes[nodeIndex];
                    if (go == null) continue;

                    var isFootprint = IsFootprint(node);

                    // This activation is load-bearing, not just footprint hiding.
                    // GameObjectInstantiator.CreateNode creates root-level nodes inactive so a
                    // half-loaded scene never renders, and the base EndScene is what switches
                    // them on. Since this override does not call base, it has to do it here or
                    // the landmark never becomes visible at all.
                    go.SetActive(!isFootprint);
                    if (isFootprint) continue;

                    var tr = go.transform;
                    tr.localPosition = Vector3.zero;
                    tr.localScale = Vector3.one;
                    tr.localRotation = Quaternion.identity;

                    // Only the roots get their transform normalised — rewriting a child's
                    // transform would detach it from its parent's placement — but footprints
                    // nested anywhere below still need hiding.
                    HideNestedFootprints(node);
                }
            }

            /// <summary>
            /// Deactivates any footprint node below <paramref name="node"/>. Deactivating a
            /// footprint takes its whole subtree with it, so there is no need to descend further
            /// once one is found.
            /// </summary>
            private void HideNestedFootprints(GLTFast.Newtonsoft.Schema.Node node)
            {
                if (node.children == null) return;

                var sourceRoot = _gltf.GetSourceRoot();
                foreach (var childIndex in node.children)
                {
                    var child = sourceRoot.nodes[childIndex];
                    var childGo = m_Nodes[childIndex];

                    if (IsFootprint(child))
                    {
                        if (childGo != null) childGo.SetActive(false);
                        continue;
                    }

                    HideNestedFootprints(child);
                }
            }
            
            private void FlipYZ(float[] matrix, MeshResult meshResult)
            {
                var mesh = meshResult.mesh;
                var verts = mesh.vertices;
                var norms = mesh.normals;
                
                for (int i = 0; i < verts.Length; i ++)
                {
                    var vert = verts[i];
                    verts[i] = new Vector3(
                        (matrix[12] / 8192) + (-vert.x / 8192) * matrix[0], 
                        vert.z / (_mapInformation.Scale * (float)Conversions.LatitudeElevationCompensation(_mapInformation.LatitudeLongitude.Latitude)), 
                        (-matrix[13] / 8192) + (vert.y / 8192) * -matrix[5]);
                    norms[i] = new Vector3(-norms[i].x, norms[i].z, norms[i].y);
                }

                mesh.normals = norms;
                mesh.vertices = verts;
                mesh.RecalculateBounds();
            }
            
            private bool IsFootprint(GLTFast.Newtonsoft.Schema.Node node)
            {
                return node.extras.TryGetValue<string>("mapbox:footprint:id", out var value);
            }
        }
    }

    
}