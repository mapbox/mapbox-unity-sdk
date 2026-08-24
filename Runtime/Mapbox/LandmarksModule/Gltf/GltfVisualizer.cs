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
        // In-flight tile ids. A HashSet rather than the Dictionary<CanonicalTileId, TaskWrapper>
        // this used to be: the dictionary only ever stored null, so it read as though a task were
        // being kept for cancellation when nothing here can cancel a glTF load. If landmark
        // generation ever does become cancellable, that wants a real handle and an un-retain hook -
        // see the note on Generate.
        private HashSet<CanonicalTileId> _activeTasks;
        private ObjectPool<GameObject> _objectPool;
        private Transform _gltfRoot;
        private IMapInformation _mapInformation;
        

        public bool IsInWork(CanonicalTileId tileId) { return _activeTasks.Contains(tileId); }
		
        public GltfVisualizer(UnityContext unityContext, IMapInformation mapInformation, Material baseMaterial)
        {
            _mapInformation = mapInformation;
            _baseMaterial = baseMaterial;
            _activeTasks = new HashSet<CanonicalTileId>();
            _objectPool = new ObjectPool<GameObject>(PoolObjectGenerator);
            var go = new GameObject("gltf root");
            // worldPositionStays: false, matching VectorLayerVisualizer. A new GameObject sits at
            // world origin with identity rotation, so the default (true) makes Unity write
            // localPosition = inverse(parent) * 0 and counter-rotate localRotation to preserve that
            // world pose - every landmark then ends up offset and counter-rotated relative to the
            // base tiles under a translated or rotated MapRoot, such as one anchored to an AR rig.
            go.transform.SetParent(unityContext.RuntimeGenerationRoot, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            _gltfRoot = go.transform;
        }
        
        public IEnumerator Initialize()
        {
            return _objectPool.InitializeItems(20);
        }
		
        /// <summary>
        /// Loads and instantiates one landmark tile.
        ///
        /// Note there is no cancellation. When RetainTiles drops a tile the vector and pbf sources
        /// cancel their in-flight fetches, but a glTF load already under way here runs to
        /// completion and reports a tile the map no longer wants - the module then caches a visual
        /// for an un-retained tile until the next sweep releases it. Wasted work rather than a
        /// correctness problem, and most visible during fast zoom sweeps where the cover churns.
        /// </summary>
        public async Task Generate(BuildingData vectorData, Action<GltfGenerationTaskResult> callback)
        {
            if (_activeTasks.Contains(vectorData.TileId))
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

            _activeTasks.Add(vectorData.TileId);

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
                // Pairs each building with its footprint now that every node has been visited. The
                // two are siblings, so neither ordering can be assumed during AddPrimitive.
                gameObjectInstantiator.ResolveFootprints();

                //LandmarkMeshResults(vectorData.TileId, footPrints);
                callback(new GltfGenerationTaskResult(
                    TaskResultType.Success,
                    new List<GameObject>() {buildingRoot},
                    gameObjectInstantiator.FootprintResults,
                    gameObjectInstantiator.AnchorResults));
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
        
        

        /// <summary>
        /// Releases everything this visualizer owns: the pooled GameObjects, the pool's own queue,
        /// and the "gltf root" container. Without it, re-initializing a map in the same session left
        /// behind a gltf root and a full pre-warmed pool each time, since nothing else has a
        /// reference to either.
        /// </summary>
        public void OnDestroy()
        {
            foreach (var pooled in _objectPool.GetQueue())
            {
                if (pooled != null) GameObject.Destroy(pooled);
            }
            _objectPool.Clear();

            // Anything still parented here belongs to tiles the module has not released; destroying
            // the root takes them with it, which is what we want on teardown.
            if (_gltfRoot != null) GameObject.Destroy(_gltfRoot.gameObject);
            _gltfRoot = null;

            _activeTasks.Clear();
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

            /// <summary>
            /// One entry per building node, for terrain snapping. Footprint outlines are attached in
            /// <see cref="ResolveFootprints"/> once the whole scene has been walked, since a
            /// building node and its footprint node are siblings and either may be visited first.
            /// </summary>
            public readonly List<LandmarkAnchor> AnchorResults = new List<LandmarkAnchor>();

            // Candidate anchors by node index, narrowed to actual root nodes in ResolveFootprints.
            // AddPrimitive fires for nodes at any depth, but the matrix translation it reads is
            // relative to the node's parent, so it only means "position within the tile" for a root.
            private readonly Dictionary<uint, LandmarkAnchor> _anchorsByNode =
                new Dictionary<uint, LandmarkAnchor>();

            // Root node indices, captured in EndScene - the only place glTFast reports them.
            private readonly HashSet<uint> _rootNodes = new HashSet<uint>();

            // Footprint rings by landmark id, kept only to pair them with building nodes at the end
            // of instantiation. Rings also go into FootprintResults for the existing consumers, but
            // that list is flat and unkeyed, so it cannot answer "which rings belong to this
            // building" - a footprint with a hole contributes several.
            private readonly Dictionary<string, List<Vector3[]>> _footprintsById =
                new Dictionary<string, List<Vector3[]>>();

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
                    // Record where this call's rings start so they can also be filed by id, without
                    // ExtractBoundaryRings needing to know anything about the pairing.
                    var firstRing = FootprintResults.Count;
                    MapboxGeometryUtilities.ExtractBoundaryRings(meshResult.mesh, FootprintResults);
                    IndexFootprintRings(node, firstRing);
                }
                else
                {
                    CaptureAnchor(node, nodeIndex);
                }
            }

            /// <summary>
            /// Files the rings just appended to <see cref="FootprintResults"/> under the landmark id
            /// they belong to, so a building node can find them at the end of instantiation.
            /// </summary>
            private void IndexFootprintRings(GLTFast.Newtonsoft.Schema.Node node, int firstRing)
            {
                if (FootprintResults.Count <= firstRing) return;

                var id = GetLandmarkId(node);
                if (string.IsNullOrEmpty(id)) return;

                if (!_footprintsById.TryGetValue(id, out var rings))
                {
                    rings = new List<Vector3[]>();
                    _footprintsById[id] = rings;
                }

                for (var i = firstRing; i < FootprintResults.Count; i++)
                {
                    rings.Add(FootprintResults[i]);
                }
            }

            /// <summary>
            /// Records the ground point of a building node.
            ///
            /// Read from the node matrix rather than the tile's own <c>extras["anchor"]</c>, which
            /// holds the identical pair of numbers: the matrix translation is what FlipYZ already
            /// relies on, so it is guaranteed to be present and correctly typed, whereas the extras
            /// value would need JSON coercion and can legitimately be absent.
            /// </summary>
            private void CaptureAnchor(GLTFast.Newtonsoft.Schema.Node node, uint nodeIndex)
            {
                // AddPrimitive fires once per primitive, so a multi-primitive building node arrives
                // here several times. One anchor per node, not per primitive.
                if (_anchorsByNode.ContainsKey(nodeIndex)) return;

                var matrix = node.matrix;
                if (matrix == null || matrix.Length < 14) return;

                if (!m_Nodes.TryGetValue(nodeIndex, out var go) || go == null) return;

                // Same expressions FlipYZ uses for the vertex origin: x eastward in 0..1, z from 0
                // at the north edge to -1 at the south. GroundUv stores the elevation-sampling form,
                // so z becomes 1 + z.
                var x01 = matrix[12] / 8192f;
                var z01 = -matrix[13] / 8192f;

                _anchorsByNode[nodeIndex] = new LandmarkAnchor
                {
                    Transform = go.transform,
                    GroundUv = new Vector2(x01, 1f + z01),
                    LandmarkId = GetLandmarkId(node)
                };
            }

            /// <summary>
            /// Attaches each building's footprint outline to its anchor. Called once instantiation
            /// has finished, because the two nodes of a landmark are siblings with no ordering
            /// guarantee - the building may well be visited before its footprint exists.
            /// </summary>
            public void ResolveFootprints()
            {
                AnchorResults.Clear();

                foreach (var pair in _anchorsByNode)
                {
                    // Root nodes only. A nested mesh node's matrix translation is relative to its
                    // parent, so treating it as a tile-space position would place the building
                    // somewhere arbitrary. (FlipYZ has the same limitation for the vertices it
                    // rewrites, so such a tile would already render wrong - this just avoids
                    // compounding it with a bogus elevation lookup.)
                    if (!_rootNodes.Contains(pair.Key)) continue;

                    var anchor = pair.Value;
                    if (!string.IsNullOrEmpty(anchor.LandmarkId) &&
                        _footprintsById.TryGetValue(anchor.LandmarkId, out var rings))
                    {
                        anchor.FootprintUvs = ToGroundUvs(rings);
                    }

                    AnchorResults.Add(anchor);
                }
            }

            /// <summary>
            /// Flattens footprint rings into elevation-sampling UVs, dropping the Y component -
            /// footprint meshes are planar at ground level, so it carries no height information.
            /// </summary>
            private static Vector2[] ToGroundUvs(List<Vector3[]> rings)
            {
                var total = 0;
                for (var r = 0; r < rings.Count; r++)
                {
                    if (rings[r] != null) total += rings[r].Length;
                }

                if (total == 0) return null;

                var result = new Vector2[total];
                var w = 0;
                for (var r = 0; r < rings.Count; r++)
                {
                    var ring = rings[r];
                    if (ring == null) continue;
                    for (var v = 0; v < ring.Length; v++)
                    {
                        result[w++] = new Vector2(ring[v].x, 1f + ring[v].z);
                    }
                }

                return result;
            }

            /// <summary>
            /// The landmark id shared by a building node and its footprint node. They publish it
            /// under different keys, which is the only reason this needs two lookups.
            /// </summary>
            private static string GetLandmarkId(GLTFast.Newtonsoft.Schema.Node node)
            {
                if (node.extras == null) return null;
                if (node.extras.TryGetValue<string>("mapbox:footprint:id", out var footprintId)) return footprintId;
                if (node.extras.TryGetValue<string>("id", out var id)) return id;
                return null;
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
                    // Recorded for anchor filtering; this is the only point glTFast says which
                    // nodes are roots.
                    _rootNodes.Add(nodeIndex);

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
                // extras is optional in glTF, so a node without any would have thrown here.
                if (node.extras == null) return false;
                return node.extras.TryGetValue<string>("mapbox:footprint:id", out var value);
            }
        }
    }

    
}