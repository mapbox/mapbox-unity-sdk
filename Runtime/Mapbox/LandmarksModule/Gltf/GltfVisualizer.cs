using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Logging;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.LandmarksModule.FeatureId;
using UnityEngine;
using Material = UnityEngine.Material;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Mapbox.LandmarksModule
{
    internal class GltfVisualizer
    {
        private Material _baseMaterial;

        /// <summary>
        /// In-flight loads, each with the handle that cancels it. glTFast threads a
        /// <see cref="CancellationToken"/> through both <c>Load</c> and
        /// <c>InstantiateSceneAsync</c> and throws <see cref="OperationCanceledException"/> out of
        /// them, so a tile the map has stopped wanting can be abandoned part-way instead of running
        /// to completion and being thrown away on arrival.
        /// </summary>
        private Dictionary<CanonicalTileId, CancellationTokenSource> _activeLoads;
        private Transform _gltfRoot;
        private IMapInformation _mapInformation;

        // Set by OnDestroy and checked after every await. Generate cannot be cancelled, so a map
        // torn down mid-load still resumes here afterwards, and everything it would touch - the
        // gltf root, the tile roots parented under it - has already been destroyed. Without this
        // the continuation parents to a destroyed transform, hands the module visuals it has no
        // way to release, and raises TileFinished for a tile that will never raise TileDisposed,
        // leaving the vector and component connectors suppressing buildings forever.
        private bool _destroyed;

        public bool IsInWork(CanonicalTileId tileId) { return _activeLoads.ContainsKey(tileId); }

        /// <summary>
        /// Abandons the load for one tile, if one is running. The call returns immediately; the
        /// load unwinds on its own and reports <see cref="TaskResultType.Cancelled"/>, having
        /// released the import and the GameObjects it had built so far.
        ///
        /// Safe to call for a tile that is not loading, and safe to call twice.
        /// </summary>
        public void Cancel(CanonicalTileId tileId)
        {
            if (_activeLoads.TryGetValue(tileId, out var cancellation))
            {
                cancellation.Cancel();
            }
        }

        public GltfVisualizer(UnityContext unityContext, IMapInformation mapInformation, Material baseMaterial)
        {
            _mapInformation = mapInformation;
            _baseMaterial = baseMaterial;
            _activeLoads = new Dictionary<CanonicalTileId, CancellationTokenSource>();

            // A null base material is not reported here. LandmarksLayerModuleScript already logs it
            // against the offending component, which is the only thing that can point the Inspector
            // at the field, and repeating it would put two errors in the console for one mistake.
            // What this class does about it is refuse to build: every landmark material is a clone
            // of this one, so Generate reports every tile as empty rather than throwing its way
            // through the whole cover.

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

        /// <summary>
        /// Kept as a coroutine because <c>ILayerModule.Initialize</c> yields on it and may grow
        /// asynchronous setup later. There is nothing to pre-warm: tile roots used to come from a
        /// pool of 20, but the success path hands each root to the module, which destroys it on
        /// release rather than returning it, so the pool recycled nothing and only deferred the
        /// first twenty allocations.
        /// </summary>
        public IEnumerator Initialize()
        {
            yield break;
        }

        /// <summary>
        /// Loads and instantiates one landmark tile.
        ///
        /// Note there is no cancellation. When RetainTiles drops a tile the vector and pbf sources
        /// cancel their in-flight fetches, but a glTF load already under way here runs to
        /// completion and reports a tile the map no longer wants - the module then caches a visual
        /// for an un-retained tile until the next sweep releases it. Wasted work rather than a
        /// correctness problem, and most visible during fast zoom sweeps where the cover churns.
        ///
        /// On success the import that built the visuals is handed to the caller through the result
        /// and becomes the caller's to dispose; on every other path it is disposed here. See
        /// <see cref="GltfGenerationTaskResult.Import"/> for why it cannot just be disposed at the
        /// end of this method.
        /// </summary>
        public async Task Generate(BuildingData vectorData, Action<GltfGenerationTaskResult> callback)
        {
            var tileId = vectorData.TileId;

            if (_activeLoads.ContainsKey(tileId))
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

            if (_baseMaterial == null)
            {
                // Reported the same way as an empty tile, and for the same reason: the tile has to
                // come back resolved or the module regenerates it on every sweep forever. The
                // constructor already said why once; a failure result here would instead invalidate
                // the data and re-download every landmark tile in the cover, indefinitely, for a
                // condition no amount of retrying can fix.
                callback(new GltfGenerationTaskResult(TaskResultType.NoData, Enumerable.Empty<GameObject>()));
                return;
            }

            var cancellation = new CancellationTokenSource();
            _activeLoads.Add(tileId, cancellation);
            var token = cancellation.Token;

            var gltf = new MapboxGltfImport(null, null, new MapboxGltfMaterialGenerator(_baseMaterial), new ConsoleLogger());
            var buildingRoot = new GameObject("gltf visualizer");
            buildingRoot.transform.SetParent(_gltfRoot, worldPositionStays: false);

            FeatureIdColorTable featureIds = null;
            MapboxGltfInstantiator gameObjectInstantiator = null;
            var loaded = false;
            var success = false;
            var canceled = false;

            try
            {
                loaded = await gltf.Load(vectorData.Data, cancellationToken: token);

                // Everything below writes into the scene, and the map can be torn down while the
                // load is in flight. Instantiating into a destroyed hierarchy is how a root ends up
                // parented to nothing and a tile the module can no longer reach ends up on screen.
                if (loaded && !_destroyed)
                {
                    // Feature IDs are decoded from the tile bytes we already hold, independently of
                    // glTFast's load-time buffers, which are released once Load completes.
                    featureIds = FeatureIdColorTable.Build(gltf.GetSourceRoot(), vectorData.Data);

                    gameObjectInstantiator =
                        new MapboxGltfInstantiator(gltf, _mapInformation, buildingRoot.transform, featureIds);

                    success = await gltf.InstantiateSceneAsync(gameObjectInstantiator, 0, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Not an error and not logged: the map stopped wanting this tile while it was
                // loading, which is the whole point of the token. Reported as Cancelled so the
                // caller records no failure against the tile and simply regenerates it if the
                // tile comes back into the cover.
                canceled = true;
            }
            catch (Exception e)
            {
                loaded = false;
                Debug.LogError($"Landmark tile {tileId} failed to generate: {e}");
            }
            finally
            {
                // Decompressed bytes are held only until instantiation has consumed them.
                featureIds?.Dispose();

                // Always clear the in-work marker. Leaving it set would make IsInWork report true
                // for the rest of the session, so the tile would never be retried.
                if (_activeLoads.TryGetValue(tileId, out var registered) && registered == cancellation)
                {
                    _activeLoads.Remove(tileId);
                }
                cancellation.Dispose();
            }

            // Dispatched outside the try so a throwing callback cannot re-enter the failure paths.
            if (loaded && success && !canceled && !_destroyed)
            {
                // Pairs each building with its footprint now that every node has been visited. The
                // two are siblings, so neither ordering can be assumed during AddPrimitive.
                gameObjectInstantiator.ResolveFootprints();

                callback(new GltfGenerationTaskResult(
                    TaskResultType.Success,
                    new List<GameObject>() {buildingRoot},
                    gameObjectInstantiator.FootprintResults,
                    gameObjectInstantiator.AnchorResults,
                    gltf));
                return;
            }

            // Failed, or the map went away mid-load. Either way nothing downstream is going to take
            // ownership of what this call built, so it is all released here. Disposing the import
            // is the part that is easy to miss: it is the only thing that destroys the meshes,
            // materials and textures glTFast created, and Unity never reclaims those on its own -
            // destroying the GameObjects below does not touch them.
            gltf.Dispose();
            if (buildingRoot != null) GameObject.Destroy(buildingRoot);

            // No caller left to tell. OnDestroy has already cleared the structures this result
            // would be recorded in, so reporting it would only re-populate them.
            if (_destroyed) return;

            callback(new GltfGenerationTaskResult(
                canceled
                    ? TaskResultType.Cancelled
                    : loaded ? TaskResultType.GameObjectFailure : TaskResultType.DataProcessingFailure,
                Enumerable.Empty<GameObject>()));
        }
        
        

        /// <summary>
        /// Releases the "gltf root" container and marks every in-flight load as unwanted. Without
        /// it, re-initializing a map in the same session left a gltf root behind each time, since
        /// nothing else holds a reference to one.
        /// </summary>
        public void OnDestroy()
        {
            // Set first. Loads already awaiting will resume after this returns, and this is what
            // tells them the scene they were building into is gone.
            _destroyed = true;

            // Abandon every load still running. They resume after this and unwind through the
            // cancellation path, which releases its own import and root - the destroyed flag alone
            // would let them finish the work first and only then discard it.
            foreach (var pair in _activeLoads)
            {
                pair.Value.Cancel();
            }
            _activeLoads.Clear();

            // Anything still parented here belongs to tiles the module has not released; destroying
            // the root takes them with it, which is what we want on teardown. The imports that
            // built them belong to the module, which disposes them alongside this call.
            if (_gltfRoot != null) GameObject.Destroy(_gltfRoot.gameObject);
            _gltfRoot = null;

        }

        private class MapboxGltfInstantiator : GameObjectInstantiator
        {
            /// <summary>
            /// Grid the tile's geometry is quantized to, the same extent the vector tile spec uses.
            /// Dividing by it puts a coordinate into the tile's own 0-1 space.
            ///
            /// Named because two separate places divide by it and they have to agree:
            /// <see cref="FlipYZ"/> for the vertices and <see cref="CaptureAnchor"/> for the point
            /// elevation is sampled at. If those two ever disagreed, a building's geometry and the
            /// ground height it is snapped to would be taken from different places in the tile.
            /// </summary>
            private const float TileExtent = 8192f;

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
                var x01 = matrix[12] / TileExtent;
                var z01 = -matrix[13] / TileExtent;

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
                // Base first. It does three things this override used to replicate only one of:
                // it hands the scene instance its MaterialsVariantsControl, it raises
                // EndSceneCompleted for glTFast's own import add-ons, and it activates every root
                // node - which is load-bearing, since CreateNode deliberately creates root nodes
                // inactive so a half-loaded scene never renders. The pass below then switches the
                // footprint roots back off, which is why it has to run after this and not before.
                base.EndScene(rootNodeIndices);

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
                    // TryGetValue, not the indexer: glTFast only records a node here if it
                    // materialised a GameObject for it, and it is free not to.
                    if (!m_Nodes.TryGetValue(nodeIndex, out var go) || go == null) continue;

                    // Base has just switched every root on. Footprints are geometry the tile
                    // publishes for collision tests, not something to draw, so they go back off.
                    if (IsFootprint(node))
                    {
                        go.SetActive(false);
                        continue;
                    }

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

                    if (IsFootprint(child))
                    {
                        // Same reason as EndScene: a node glTFast chose not to materialise has no
                        // entry here, and there is nothing to hide for it either.
                        if (m_Nodes.TryGetValue(childIndex, out var childGo) && childGo != null)
                        {
                            childGo.SetActive(false);
                        }
                        continue;
                    }

                    HideNestedFootprints(child);
                }
            }
            
            /// <summary>
            /// Rewrites a primitive's vertices from glTF node space into tile-local space, and
            /// flips its normals to match.
            ///
            /// Two assumptions are baked in and neither is enforced by the format. The node must
            /// carry a <c>matrix</c> rather than TRS, which is why the guard below exists - glTF
            /// allows either, and a TRS node would otherwise null-reference here and blank the
            /// whole tile through the catch in Generate. And the node's mesh must not be shared
            /// with another node: glTFast hands the same Unity mesh to every node referencing one
            /// glTF mesh, and this bakes the calling node's own translation into that shared vertex
            /// buffer, so the second node would win and both buildings would render in the wrong
            /// place. The landmarks tileset emits one mesh per node, so neither happens today.
            /// </summary>
            private void FlipYZ(float[] matrix, MeshResult meshResult)
            {
                var mesh = meshResult.mesh;

                // matrix[13] is the highest index read below, so anything shorter than 14 cannot be
                // used. A node with no matrix at all is the TRS case described above.
                if (matrix == null || matrix.Length < 14)
                {
                    Debug.LogWarning(
                        $"Landmark node '{mesh.name}' carries no glTF node matrix, so it cannot be " +
                        "placed in tile space and is left where glTFast put it. The landmarks " +
                        "tileset is expected to publish a matrix on every node.");
                    return;
                }

                var verts = mesh.vertices;
                var norms = mesh.normals;

                // A primitive with positions but no NORMAL attribute gives an empty normals array,
                // which the loop below would index straight past the end of.
                var hasNormals = norms != null && norms.Length == verts.Length;

                for (int i = 0; i < verts.Length; i ++)
                {
                    var vert = verts[i];
                    verts[i] = new Vector3(
                        (matrix[12] / TileExtent) + (-vert.x / TileExtent) * matrix[0],
                        vert.z / (_mapInformation.Scale * (float)Conversions.LatitudeElevationCompensation(_mapInformation.LatitudeLongitude.Latitude)), 
                        (-matrix[13] / TileExtent) + (vert.y / TileExtent) * -matrix[5]);
                    if (hasNormals) norms[i] = new Vector3(-norms[i].x, norms[i].z, norms[i].y);
                }

                if (hasNormals) mesh.normals = norms;
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