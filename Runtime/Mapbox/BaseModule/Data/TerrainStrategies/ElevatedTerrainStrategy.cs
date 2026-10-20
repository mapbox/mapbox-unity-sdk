using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain.Settings;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;
// Unity 6.3 replaced the int instance id in Physics.BakeMesh with the opaque EntityId
// handle and marked the int overload obsolete-as-error. Alias the id type so the same
// job code compiles on 2022.3 through 6.2 (int) and 6.3+ (EntityId).
#if UNITY_6000_3_OR_NEWER
using MeshBakeId = UnityEngine.EntityId;
#else
using MeshBakeId = System.Int32;
#endif

namespace Mapbox.ImageModule.Terrain.TerrainStrategies
{
	public class MeshDataArray
	{
		public Vector3[] Vertices;
		public Vector3[] Normals;
		public List<int[]> Triangles;
		public Vector2[] Uvs;

		public MeshDataArray()
		{
			Triangles = new List<int[]>();
		}
	}

	public class ElevatedTerrainStrategy : TerrainStrategy, IElevationBasedTerrainStrategy
	{
		[SerializeField]
		protected ElevationLayerProperties _elevationOptions = new ElevationLayerProperties();
		
		private MeshDataArray _baseMesh;
		private List<Vector3> _newVertexList;
		private List<Vector3> _newNormalList;
		private List<Vector2> _newUvList;

		private bool _useTileSkirts = false;
		private float _skirtSize = 1;

		private int _sideVertexCount;
		private int _requiredVertexCount;

		private TerrainColliderOptions _colliderOptions;

		// Collider geometry is written by a Burst job into persistent NativeArrays so the
		// data flows zero-copy into Mesh.SetVertices / SetIndices. Triangles are generated
		// once per sampleCount change (they don't depend on elevation), so only the vertex
		// buffer is written per tile build. The elevation mirror is a NativeArray copy of
		// the current data tile's managed ElevationValues — populated lazily when the
		// source reference changes, reused across the 16 render tiles that share one data
		// tile.
		private NativeArray<Vector3> _colliderVerticesNative;
		private NativeArray<int> _colliderTrianglesNative;
		private int _colliderNativeSampleCount = -1;
		private NativeArray<float> _elevationNative;
		// Keyed by the TerrainData reference rather than the float[] reference: ElevationArrayPool
		// recycles float[] buffers, so the same array reference can hold different data across
		// dispose+re-rent cycles. TerrainData instances aren't recycled the same way, so reference
		// equality on the data wrapper is a safe cache key.
		private TerrainData _elevationNativeMirror;

		// Shared flat render mesh used by every tile in shader mode. All render tiles have
		// byte-identical CPU vertex data in that mode (the per-tile _HeightTexture_ST
		// sub-region determines the actual surface); pointing every MeshFilter at this
		// single Mesh avoids per-tile Mesh allocation and upload.
		// Exposed for UnityMapTile.OnDestroy so its leak cleanup can identify which meshes
		// it must skip (this one is owned by the strategy) versus destroy (per-tile collider
		// or CPU-elevated render mesh).
		public const string SharedFlatMeshName = "TerrainSharedFlat";
		public const string TerrainColliderMeshName = "TerrainCollider";
		public const string TerrainCpuMeshName = "TerrainCpuMesh";
		private Mesh _sharedFlatMesh;

		// Tracks the (TerrainData, CanonicalTileId) last used to build each MeshCollider's
		// sharedMesh. Temp-tile → final-tile transitions can trigger RegisterTile with the
		// same data; skipping the rebuild avoids a redundant PhysX re-cook.
		private readonly Dictionary<MeshCollider, (TerrainData data, CanonicalTileId tileId)>
			_lastColliderBuild = new Dictionary<MeshCollider, (TerrainData, CanonicalTileId)>();

		// Sweep dead-collider entries from _lastColliderBuild once it exceeds this count.
		// Bounded by tile-pool peak in normal use; the sweep catches the case where
		// GameObjects actually destroy (scene teardown, runtime tile-pool resize).
		private const int LastColliderBuildSweepThreshold = 256;
		private readonly List<MeshCollider> _deadColliderKeysScratch = new List<MeshCollider>();

		// Tracks deferred-rebuild closures so OnDestroy can unsubscribe them. Without this
		// list, if a TerrainData is disposed before ElevationValuesUpdated fires, the
		// closure stays attached to the event and roots both tile + data via capture.
		// Keyed by (data, tile) so RegisterCollider's temp→final re-invocation can
		// de-dup against an already-pending entry.
		private readonly List<(TerrainData data, UnityMapTile tile, Action rebuild)> _pendingRebuilds =
			new List<(TerrainData, UnityMapTile, Action)>();
		
		public override int RequiredVertexCount
		{
			get
			{
				return _requiredVertexCount;
			}
		}

		public override void Initialize(ElevationLayerProperties elOptions)
		{
			if (elOptions != null)
			{
				_elevationOptions = elOptions;
			}

			_useTileSkirts = elOptions.sideWallOptions.isActive;
			 _sideVertexCount = _useTileSkirts
				? _elevationOptions.modificationOptions.sampleCount + 3
				: _elevationOptions.modificationOptions.sampleCount + 1;
			_skirtSize = elOptions.sideWallOptions.wallHeight;
			_colliderOptions = elOptions.colliderOptions;

			_newVertexList = new List<Vector3>(_requiredVertexCount);
			_newNormalList = new List<Vector3>(_requiredVertexCount);
			_newUvList = new List<Vector2>(_requiredVertexCount);

			_baseMesh = CreateBaseMesh(_elevationOptions.TileMeshSize, _sideVertexCount);
			_requiredVertexCount = _baseMesh.Vertices.Length;

			// Free the previous shared mesh on re-init — otherwise a second Initialize
			// orphans it. OnDestroy alone isn't enough since re-init can happen without a
			// preceding destroy (settings changes, editor reloads).
			if (_sharedFlatMesh != null)
			{
				UnityEngine.Object.Destroy(_sharedFlatMesh);
				_sharedFlatMesh = null;
			}

			// Build the shader-mode shared render mesh from the base data once. Every
			// shader-mode tile's MeshFilter will point at this single instance.
			_sharedFlatMesh = new Mesh { name = SharedFlatMeshName };
			_sharedFlatMesh.subMeshCount = 2;
			_sharedFlatMesh.vertices = _baseMesh.Vertices;
			_sharedFlatMesh.normals = _baseMesh.Normals;
			for (var i = 0; i < _baseMesh.Triangles.Count; i++)
			{
				_sharedFlatMesh.SetTriangles(_baseMesh.Triangles[i], i);
			}
			_sharedFlatMesh.uv = _baseMesh.Uvs;
			_sharedFlatMesh.UploadMeshData(markNoLongerReadable: false);
		}

		/// <summary>
		/// Cascaded from <c>TerrainLayerModule.OnDestroy</c>. Releases the shared render
		/// mesh and the persistent NativeArrays that back Burst collider builds; per-tile
		/// collider meshes are released from <c>UnityMapTile.OnDestroy</c>.
		/// </summary>
		public override void OnDestroy()
		{
			// Detach any pending-rebuild subscriptions before disposing — closures
			// would otherwise stay attached to TerrainData.ElevationValuesUpdated and
			// root captured (tile, data) indefinitely if the event never fires.
			for (int i = 0; i < _pendingRebuilds.Count; i++)
			{
				var entry = _pendingRebuilds[i];
				if (entry.data != null && entry.rebuild != null)
				{
					entry.data.ElevationValuesUpdated -= entry.rebuild;
				}
			}
			_pendingRebuilds.Clear();

			if (_sharedFlatMesh != null)
			{
				UnityEngine.Object.Destroy(_sharedFlatMesh);
				_sharedFlatMesh = null;
			}
			if (_colliderVerticesNative.IsCreated) _colliderVerticesNative.Dispose();
			if (_colliderTrianglesNative.IsCreated) _colliderTrianglesNative.Dispose();
			if (_elevationNative.IsCreated) _elevationNative.Dispose();
			_lastColliderBuild.Clear();
			_elevationNativeMirror = null;
		}

		public override void RegisterTile(UnityMapTile tile, bool createElevatedMesh)
		{
			if (_elevationOptions.unityLayerOptions.addToLayer && tile.gameObject.layer != _elevationOptions.unityLayerOptions.layerId)
			{
				tile.gameObject.layer = _elevationOptions.unityLayerOptions.layerId;
			}

			if (!createElevatedMesh)
			{
				// Shader mode: point the tile at the shared flat mesh. Byte-identical
				// vertex data across tiles means a single Mesh covers the whole pool; the
				// shader picks the right sub-region via per-tile _HeightTexture_ST.
				if (tile.MeshFilter.sharedMesh != _sharedFlatMesh)
				{
					var previous = tile.MeshFilter.sharedMesh;
					tile.MeshFilter.sharedMesh = _sharedFlatMesh;
					// The mesh UnityMapTile.Awake allocated is now orphaned; destroy it
					// unless something else (not us) already put the shared mesh here.
					if (previous != null && previous != _sharedFlatMesh && previous.name != SharedFlatMeshName)
					{
						UnityEngine.Object.Destroy(previous);
					}
				}
				tile.MeshVertexCount = _sharedFlatMesh.vertexCount;
			}
			else if (tile.MeshVertexCount != RequiredVertexCount)
			{
				// CPU mode: each tile needs its own unique vertex buffer since the Y
				// displacement is per-tile. Reset the tile's Awake-allocated mesh from the
				// base template — but never Clear() _sharedFlatMesh, which would wipe the
				// mesh used by every shader-mode tile. Allocate a fresh per-tile mesh if
				// this tile's MeshFilter is still pointing at the shared instance (happens
				// when a runtime config change flips the tile shader→CPU and the new
				// RequiredVertexCount differs from _sharedFlatMesh.vertexCount).
				var sharedMesh = tile.MeshFilter.sharedMesh;
				if (sharedMesh == null || sharedMesh == _sharedFlatMesh)
				{
					sharedMesh = new Mesh { name = TerrainCpuMeshName };
					tile.MeshFilter.sharedMesh = sharedMesh;
				}
				else
				{
					// Re-tag any pre-existing per-tile mesh (e.g. the Awake mesh) so
					// UnityMapTile.OnDestroy correctly destroys it on tile teardown.
					sharedMesh.name = TerrainCpuMeshName;
				}
				sharedMesh.Clear();
				var newMesh = _baseMesh;
				sharedMesh.subMeshCount = 2;
				sharedMesh.vertices = newMesh.Vertices;
				sharedMesh.normals = newMesh.Normals;
				for (var i = 0; i < newMesh.Triangles.Count; i++)
				{
					var triangle = newMesh.Triangles[i];
					sharedMesh.SetTriangles(triangle, i);
				}
				sharedMesh.uv = newMesh.Uvs;
				tile.MeshVertexCount = newMesh.Vertices.Length;
				tile.ElevationUpdatedCallback();
			}

			if (createElevatedMesh)
			{
				// Same-vertex-count shader→CPU transition: the else-if branch above didn't
				// fire, but MeshFilter.sharedMesh might still be _sharedFlatMesh. Allocate
				// a per-tile owned mesh in that case so CreateElevatedMesh's writes don't
				// hit the shared one.
				if (tile.MeshFilter.sharedMesh == null || tile.MeshFilter.sharedMesh == _sharedFlatMesh)
				{
					var perTile = new Mesh { name = TerrainCpuMeshName };
					perTile.subMeshCount = 2;
					perTile.vertices = _baseMesh.Vertices;
					perTile.normals = _baseMesh.Normals;
					for (var i = 0; i < _baseMesh.Triangles.Count; i++)
					{
						perTile.SetTriangles(_baseMesh.Triangles[i], i);
					}
					perTile.uv = _baseMesh.Uvs;
					tile.MeshFilter.sharedMesh = perTile;
				}
				CreateElevatedMesh(tile);
			}

			if (_colliderOptions != null && _colliderOptions.addCollider)
			{
				RegisterCollider(tile);
			}
		}

		/// <summary>
		/// Ensures <paramref name="tile"/> has a <see cref="MeshCollider"/> backed by a
		/// CPU-elevated mesh. Builds immediately when elevation data is already decoded,
		/// otherwise defers until <see cref="TerrainData.ElevationValuesUpdated"/> fires
		/// (async GPU-readback path). Short-circuits when the tile's existing collider was
		/// already built from the same data + tileId (temp→final transitions trigger
		/// RegisterTile multiple times without the underlying data changing).
		/// </summary>
		private void RegisterCollider(UnityMapTile tile)
		{
			var data = tile.TerrainContainer != null ? tile.TerrainContainer.TerrainData : null;
			if (data == null)
			{
				return;
			}

			var existing = FindExistingCollider(tile);
			if (existing != null &&
			    _lastColliderBuild.TryGetValue(existing, out var last) &&
			    ReferenceEquals(last.data, data) &&
			    last.tileId.Equals(tile.CanonicalTileId))
			{
				return;
			}

			if (data.IsElevationDataReady)
			{
				BuildAndAssignCollider(tile);
			}
			else
			{
				// De-dup: temp→final transitions re-call RegisterTile with the same
				// (tile, data). Without this check we'd add a second closure + a second
				// async bake, doubling work for every transition.
				for (int i = 0; i < _pendingRebuilds.Count; i++)
				{
					if (ReferenceEquals(_pendingRebuilds[i].data, data) &&
					    ReferenceEquals(_pendingRebuilds[i].tile, tile))
					{
						return;
					}
				}

				// Defer until values arrive. The callback self-unsubscribes on fire and
				// no-ops if the tile has since been recycled onto different data, so a late
				// async readback does not stomp a freshly-reassigned tile. We also track
				// the subscription in _pendingRebuilds so OnDestroy can detach pending
				// closures whose data is disposed before the event fires, AND attach a
				// dispose-callback so eviction self-detaches (TerrainData.Dispose doesn't
				// raise ElevationValuesUpdated, so without this hook the closure would
				// stay attached until the next visualizer teardown — pendings grow
				// O(failed-decodes) under bursty zoom).
				Action rebuild = null;
				Action onDispose = null;
				(TerrainData data, UnityMapTile tile, Action rebuild) entry = (data, tile, null);
				rebuild = () =>
				{
					data.ElevationValuesUpdated -= rebuild;
					data.RemoveDisposeCallback(onDispose);
					_pendingRebuilds.Remove(entry);
					if (tile == null || tile.TerrainContainer == null || tile.TerrainContainer.TerrainData != data)
					{
						return;
					}
					BuildAndAssignCollider(tile);
				};
				onDispose = () =>
				{
					data.ElevationValuesUpdated -= rebuild;
					data.RemoveDisposeCallback(onDispose);
					_pendingRebuilds.Remove(entry);
				};
				entry.rebuild = rebuild;
				_pendingRebuilds.Add(entry);
				data.ElevationValuesUpdated += rebuild;
				data.AddDisposeCallback(onDispose);
			}
		}

		// Our generated grid has no duplicate verts, no degenerate triangles, and doesn't
		// need welding, so we tell PhysX to skip those cook stages. Keeps
		// CookForFasterSimulation on for faster runtime queries against the static terrain.
		private const MeshColliderCookingOptions TerrainColliderCookingOptions =
			MeshColliderCookingOptions.CookForFasterSimulation |
			MeshColliderCookingOptions.UseFastMidphase;

		// Name of the dedicated child GameObject that holds the MeshCollider when
		// useDedicatedColliderLayer is enabled. Keyed by name so we can locate + reuse it
		// across pool cycles without maintaining a per-tile dictionary. Distinct from
		// TerrainColliderMeshName so the GO name and the Mesh name don't collide
		// (they were both "TerrainCollider" — same string used for two different purposes).
		private const string ColliderChildName = "TerrainColliderChild";

		// Removes _lastColliderBuild entries whose MeshCollider key has been destroyed
		// (GameObject teardown, scene unload). Unity's overloaded == returns true for
		// destroyed-vs-null, but Dictionary<K,V> hashes by C# reference equality and
		// would otherwise keep dead proxies indefinitely.
		private void SweepDeadColliderEntries()
		{
			_deadColliderKeysScratch.Clear();
			foreach (var kv in _lastColliderBuild)
			{
				if (kv.Key == null)
				{
					_deadColliderKeysScratch.Add(kv.Key);
				}
			}
			for (int i = 0; i < _deadColliderKeysScratch.Count; i++)
			{
				_lastColliderBuild.Remove(_deadColliderKeysScratch[i]);
			}
			_deadColliderKeysScratch.Clear();
		}

		/// <summary>
		/// Looks up the existing <see cref="MeshCollider"/> for this tile without creating
		/// one, mirroring the location rule in <see cref="GetOrCreateCollider"/>. Used by
		/// the rebuild-short-circuit check.
		/// </summary>
		private MeshCollider FindExistingCollider(UnityMapTile tile)
		{
			if (_colliderOptions.useDedicatedColliderLayer)
			{
				var childTransform = tile.transform.Find(ColliderChildName);
				return childTransform != null ? childTransform.GetComponent<MeshCollider>() : null;
			}
			return tile.GetComponent<MeshCollider>();
		}

		/// <summary>
		/// Returns the <see cref="MeshCollider"/> the collider mesh should be assigned to.
		/// When <see cref="TerrainColliderOptions.useDedicatedColliderLayer"/> is enabled
		/// the collider lives on a child GameObject so it can sit on its own Unity Layer
		/// independently of the tile's render layer; otherwise it's attached to the tile
		/// itself. Applies our tuned <see cref="TerrainColliderCookingOptions"/> on first
		/// creation.
		/// </summary>
		private MeshCollider GetOrCreateCollider(UnityMapTile tile)
		{
			MeshCollider collider;
			if (_colliderOptions.useDedicatedColliderLayer)
			{
				var childTransform = tile.transform.Find(ColliderChildName);
				GameObject childGo;
				if (childTransform == null)
				{
					childGo = new GameObject(ColliderChildName);
					childGo.transform.SetParent(tile.transform, worldPositionStays: false);
				}
				else
				{
					childGo = childTransform.gameObject;
				}
				childGo.layer = _colliderOptions.colliderLayerId;

				collider = childGo.GetComponent<MeshCollider>();
				if (collider == null)
				{
					collider = childGo.AddComponent<MeshCollider>();
				}
			}
			else
			{
				collider = tile.GetComponent<MeshCollider>();
				if (collider == null)
				{
					collider = tile.gameObject.AddComponent<MeshCollider>();
				}
			}
			// Reassign every call: if the collider's cookingOptions diverges from what
			// BakeColliderJob uses, PhysX discards the async-cooked cache and re-cooks
			// synchronously on sharedMesh assignment — silently defeating the async path.
			collider.cookingOptions = TerrainColliderCookingOptions;
			return collider;
		}

		/// <summary>
		/// Builds a dedicated CPU-elevated collider mesh for <paramref name="tile"/> and
		/// assigns it to the tile's <see cref="MeshCollider"/>. Grid resolution mirrors the
		/// render mesh so physics stays visually aligned with the terrain surface.
		/// </summary>
		private void BuildAndAssignCollider(UnityMapTile tile)
		{
			var meshCollider = GetOrCreateCollider(tile);

			// Unity auto-populates sharedMesh on a newly added MeshCollider from the
			// GameObject's MeshFilter.sharedMesh. We must NOT write into that mesh — it is
			// the render mesh. Detect and allocate a dedicated collider Mesh instead.
			var mesh = meshCollider.sharedMesh;
			if (mesh == null || mesh == tile.MeshFilter.sharedMesh)
			{
				mesh = new Mesh { name = TerrainColliderMeshName };
				mesh.MarkDynamic();
			}

			// Grid resolution matches the render mesh's INTERIOR vertex grid so the
			// collision surface aligns with the visible terrain over the tile area. In
			// skirt mode the render mesh adds 2 extra verts per axis (xrat = -0.01,
			// 1.01) just outside the tile boundary; the collider intentionally omits
			// these — there's no real geometry at the skirt and physics queries are not
			// expected outside the tile. If the user picks a coarser SimplificationFactor,
			// the collider follows.
			var sampleCount = _elevationOptions.modificationOptions.sampleCount;
			var side = sampleCount + 1;
			var size = _elevationOptions.TileMeshSize;
			var scale = tile.TileScale;

			// Elevation mirror is populated once per data tile and shared across the 16
			// render tiles that sample the same data. Triangles are regenerated only when
			// sampleCount changes — see EnsureColliderBuffers.
			var container = tile.TerrainContainer;
			var terrainData = container.TerrainData;
			var elevationValues = terrainData.ElevationValues;
			var dataWidth = (int)Mathf.Sqrt(elevationValues.Length);
			var scaleOffset = container.TerrainTextureScaleOffset;

			EnsureColliderBuffers(sampleCount);
			EnsureElevationNative(terrainData);

			new BuildColliderVerticesJob
			{
				ElevationValues = _elevationNative,
				DataWidth = dataWidth,
				SampleCount = sampleCount,
				Side = side,
				Size = size,
				Scale = scale,
				SectionWidth = dataWidth * scaleOffset.x - 1f,
				PaddingX = dataWidth * scaleOffset.z,
				PaddingY = dataWidth * scaleOffset.w,
				Vertices = _colliderVerticesNative
			}.Run();

			mesh.Clear();
			mesh.SetVertices(_colliderVerticesNative);
			// SetIndices is the canonical NativeArray-taking Mesh index API; the
			// NativeArray overload of SetTriangles isn't present on every Unity 2022.3
			// point release. MeshTopology.Triangles produces equivalent triangle-list
			// geometry.
			mesh.SetIndices(_colliderTrianglesNative, MeshTopology.Triangles, 0, calculateBounds: false);
			mesh.RecalculateBounds();

			// Record what we just built so RegisterCollider can short-circuit on
			// redundant subsequent RegisterTile calls (temp→final transitions).
			// Sweep dead Unity Object keys opportunistically when the dict grows past
			// the soft cap so destroyed-collider proxies don't linger across long
			// sessions (Unity's overloaded == doesn't reach Dictionary's reference
			// equality, so dead keys would otherwise stay queryable).
			if (_lastColliderBuild.Count > LastColliderBuildSweepThreshold)
			{
				SweepDeadColliderEntries();
			}
			_lastColliderBuild[meshCollider] = (tile.TerrainContainer.TerrainData, tile.CanonicalTileId);

			if (_colliderOptions.asyncBakeCollider)
			{
				// Move the PhysX cook to a worker thread. The assignment (and thus any
				// implicit recook) happens one frame later once BakeMesh has populated the
				// native cooked-data cache for this mesh id. PhysX reuses that cache when
				// sharedMesh is assigned, so the main-thread step is near-free.
				var handle = new BakeColliderJob
				{
#if UNITY_6000_3_OR_NEWER
					MeshId = mesh.GetEntityId(),
#else
					MeshId = mesh.GetInstanceID(),
#endif
					CookingOptions = TerrainColliderCookingOptions
				}.Schedule();
				Runnable.Instance.StartCoroutine(CompleteBakeAndAssign(meshCollider, mesh, handle));
			}
			else
			{
				// Null-then-reassign forces PhysX to re-cook collision data; a same-reference
				// reassignment is a no-op.
				meshCollider.sharedMesh = null;
				meshCollider.sharedMesh = mesh;
			}
		}

		/// <summary>
		/// (Re)allocates the persistent collider NativeArrays when the strategy's
		/// sampleCount changes, and generates the triangle index list once — triangles are
		/// a pure function of <paramref name="sampleCount"/> and never need rebuilding per
		/// tile.
		/// </summary>
		private void EnsureColliderBuffers(int sampleCount)
		{
			if (_colliderNativeSampleCount == sampleCount && _colliderVerticesNative.IsCreated && _colliderTrianglesNative.IsCreated)
			{
				return;
			}
			if (_colliderVerticesNative.IsCreated) _colliderVerticesNative.Dispose();
			if (_colliderTrianglesNative.IsCreated) _colliderTrianglesNative.Dispose();

			var side = sampleCount + 1;
			_colliderVerticesNative = new NativeArray<Vector3>(side * side, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			_colliderTrianglesNative = new NativeArray<int>(sampleCount * sampleCount * 6, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

			int ti = 0;
			for (int y = 0; y < sampleCount; y++)
			{
				for (int x = 0; x < sampleCount; x++)
				{
					int vertA = y * side + x;
					int vertB = vertA + side + 1;
					int vertC = vertA + side;
					_colliderTrianglesNative[ti++] = vertA;
					_colliderTrianglesNative[ti++] = vertC;
					_colliderTrianglesNative[ti++] = vertB;

					vertA = y * side + x;
					vertB = vertA + 1;
					vertC = vertA + side + 1;
					_colliderTrianglesNative[ti++] = vertA;
					_colliderTrianglesNative[ti++] = vertC;
					_colliderTrianglesNative[ti++] = vertB;
				}
			}
			_colliderNativeSampleCount = sampleCount;
		}

		/// <summary>
		/// Mirrors the current data tile's managed <c>ElevationValues</c> into the
		/// strategy's persistent <see cref="_elevationNative"/>. Reuses the existing
		/// NativeArray when the source <see cref="TerrainData"/> reference is unchanged,
		/// so the 16 render tiles that share a data tile pay the ~256 KB copy only once.
		/// </summary>
		private void EnsureElevationNative(TerrainData terrainData)
		{
			var source = terrainData.ElevationValues;
			if (ReferenceEquals(terrainData, _elevationNativeMirror) && _elevationNative.IsCreated && _elevationNative.Length == source.Length)
			{
				return;
			}
			if (_elevationNative.IsCreated && _elevationNative.Length != source.Length)
			{
				_elevationNative.Dispose();
			}
			if (!_elevationNative.IsCreated)
			{
				_elevationNative = new NativeArray<float>(source.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			}
			_elevationNative.CopyFrom(source);
			_elevationNativeMirror = terrainData;
		}

		/// <summary>
		/// Burst-compiled vertex fill for the collider mesh. Bilinearly samples the
		/// mirrored elevation NativeArray and writes world-local vertex positions. Grid
		/// topology (triangles) is produced once in <see cref="EnsureColliderBuffers"/>,
		/// not in this job.
		/// </summary>
		[BurstCompile]
		private struct BuildColliderVerticesJob : IJob
		{
			[ReadOnly] public NativeArray<float> ElevationValues;
			public int DataWidth;
			public int SampleCount;
			public int Side;
			public float Size;
			public float Scale;
			public float SectionWidth;
			public float PaddingX;
			public float PaddingY;
			public NativeArray<Vector3> Vertices;

			public void Execute()
			{
				var invSampleCount = 1f / SampleCount;
				var maxIndex = DataWidth - 1;

				for (int y = 0; y < Side; y++)
				{
					var yrat = y * invSampleCount;
					var sampleYf = PaddingY + yrat * SectionWidth;
					if (sampleYf < 0f) sampleYf = 0f; else if (sampleYf > maxIndex) sampleYf = maxIndex;
					var y0 = (int)sampleYf;
					var y1 = y0 + 1; if (y1 > maxIndex) y1 = maxIndex;
					var fy = sampleYf - y0;
					var row0 = y0 * DataWidth;
					var row1 = y1 * DataWidth;
					var yy = (1f - yrat) * Size;

					for (int x = 0; x < Side; x++)
					{
						var xrat = x * invSampleCount;
						var sampleXf = PaddingX + xrat * SectionWidth;
						if (sampleXf < 0f) sampleXf = 0f; else if (sampleXf > maxIndex) sampleXf = maxIndex;
						var x0 = (int)sampleXf;
						var x1 = x0 + 1; if (x1 > maxIndex) x1 = maxIndex;
						var fx = sampleXf - x0;

						var h00 = ElevationValues[row0 + x0];
						var h10 = ElevationValues[row0 + x1];
						var h01 = ElevationValues[row1 + x0];
						var h11 = ElevationValues[row1 + x1];
						var h0 = h00 + (h10 - h00) * fx;
						var h1 = h01 + (h11 - h01) * fx;
						var sample = h0 + (h1 - h0) * fy;

						Vertices[y * Side + x] = new Vector3(xrat * Size, sample * Scale, -yy);
					}
				}
			}
		}

		/// <summary>
		/// IJob wrapper around <c>Physics.BakeMesh</c> so the PhysX cook can run on a
		/// worker thread. The cooking options must match what the MeshCollider is
		/// configured with, otherwise PhysX discards the cached data and re-cooks on
		/// assignment.
		/// Intentionally NOT [BurstCompile]: the entire body is a single P/Invoke into
		/// PhysX, so Burst-compiling adds AOT cost with no measurable benefit.
		/// </summary>
		private struct BakeColliderJob : IJob
		{
			public MeshBakeId MeshId;
			public MeshColliderCookingOptions CookingOptions;

			public void Execute()
			{
				Physics.BakeMesh(MeshId, false, CookingOptions);
			}
		}

		/// <summary>
		/// Waits for an async <see cref="BakeColliderJob"/> to complete, then assigns the
		/// pre-cooked mesh to its <see cref="MeshCollider"/>. Handles: (a) the tile or
		/// collider got destroyed during the bake — we free the orphaned mesh so it doesn't
		/// leak; (b) a previous <c>TerrainCollider</c> mesh was attached to this collider —
		/// we destroy it once we've swapped in the new one, since each async build
		/// allocates a fresh Mesh.
		/// </summary>
		private static IEnumerator CompleteBakeAndAssign(MeshCollider meshCollider, Mesh mesh, JobHandle handle)
		{
			while (!handle.IsCompleted)
			{
				yield return null;
			}
			handle.Complete();

			// Either side can be destroyed while we waited: UnityMapTile.OnDestroy can take
			// the meshCollider with the GameObject, and a parallel bake completion that
			// landed first can have Destroy()'d our mesh as its "previous" (line below).
			if (meshCollider == null || mesh == null)
			{
				if (mesh != null)
				{
					UnityEngine.Object.Destroy(mesh);
				}
				yield break;
			}

			var previous = meshCollider.sharedMesh;
			meshCollider.sharedMesh = null;
			meshCollider.sharedMesh = mesh;
			if (previous != null && previous != mesh && previous.name == TerrainColliderMeshName)
			{
				UnityEngine.Object.Destroy(previous);
			}
		}

		private void CreateElevatedMesh(UnityMapTile tile)
		{
			// Use sharedMesh (not .mesh) — the .mesh getter implicitly clones the assigned
			// shared mesh on first access and orphans the original. We pre-ensured a
			// uniquely-owned per-tile mesh in RegisterTile above.
			var mesh = tile.MeshFilter.sharedMesh;
			var vertices = mesh.vertices;
			// Per-axis vertex count is cached from Initialize. Was previously derived via
			// Mathf.Sqrt(mesh.vertexCount), which is exact for our square grid but wasteful
			// and gave the local an off-axis name (it's _sideVertexCount, not sampleCount).
			var sampleCount = _sideVertexCount;
			for (int i = 0; i < vertices.Length; i++)
			{
				var x = i % sampleCount;
				var y = i / sampleCount;
				var dx = (float)x / (sampleCount - 2);
				var dy = (float)y / (sampleCount - 2);
				var elevation = 0f;
				if (!_useTileSkirts)
				{
					elevation = tile.TerrainContainer.QueryHeightData(dx, dy) * tile.TileScale;
				}
				else
				{
					elevation = (x == 0 || y == 0 || x == sampleCount - 1 || y == sampleCount - 1)
						? -_skirtSize
						: tile.TerrainContainer.QueryHeightData(dx, dy) * tile.TileScale;
				}

				vertices[i].Set(vertices[i].x, elevation, vertices[i].z);
			}
			mesh.SetVertices(vertices);
			// Actual displaced bounds are tighter than the min/max-padded fallback bounds
			// set by UnityMapTile.ElevationUpdatedCallback. Mesh normals/tangents are not
			// recalculated: the terrain shader derives the surface normal from the height
			// texture directly in the fragment stage, so the mesh's vertex normals are
			// never read in either mode.
			mesh.RecalculateBounds();
		}

		#region mesh gen
		private MeshDataArray CreateBaseMesh(float tileSize, int sampleCount)
		{
			return
				_useTileSkirts
					? CreateBaseMeshSkirts(tileSize, sampleCount)
					: CreateBaseMeshWithoutSkirts(tileSize, sampleCount);
		}

		private MeshDataArray CreateBaseMeshWithoutSkirts(float size, int sampleCount)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			var _newTriangleList = new List<int>();

			//012
			//345
			//678
			for (float y = 0; y < sampleCount; y++)
			{
				var yrat = y / (sampleCount - 1);
				for (float x = 0; x < sampleCount; x++)
				{
					var xrat = x / (sampleCount - 1);

					var xx = Mathf.LerpUnclamped(0, size, xrat);
					//lerp x/y swapped here because of the texture space conversion (y to -y)
					var yy = Mathf.LerpUnclamped(size, 0, yrat);

					var elevation = 0;

					_newVertexList.Add(new Vector3(
						xx,
						elevation,
						-1 * yy));
					_newNormalList.Add(Constants.Math.Vector3Up);
					_newUvList.Add(new Vector2(x * 1f / (sampleCount - 1), (y * 1f / (sampleCount - 1))));
				}
			}

			int vertA, vertB, vertC;

			for (int y = 0; y < sampleCount - 1; y++)
			{
				for (int x = 0; x < sampleCount - 1; x++)
				{
					vertA = (y * sampleCount) + x;
					vertB = (y * sampleCount) + x + sampleCount + 1;
					vertC = (y * sampleCount) + x + sampleCount;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertC);
					_newTriangleList.Add(vertB);

					vertA = (y * sampleCount) + x;
					vertB = (y * sampleCount) + x + 1;
					vertC = (y * sampleCount) + x + sampleCount + 1;
					_newTriangleList.Add(vertA);
					_newTriangleList.Add(vertC);
					_newTriangleList.Add(vertB);
				}
			}

			var mesh = new MeshDataArray();
			mesh.Vertices = _newVertexList.ToArray();
			mesh.Normals = _newNormalList.ToArray();
			mesh.Uvs = _newUvList.ToArray();
			mesh.Triangles.Add(_newTriangleList.ToArray());
			return mesh;
		}

		/// <summary>
		/// Fraction of tile size that the outer skirt ring extends past the tile boundary.
		/// Kept constant so coarse meshes (e.g. <c>sampleCount=2</c> with
		/// <c>SimplificationFactor=64</c>) don't balloon the skirt half-way into neighboring
		/// tiles. 1% is narrow enough to be invisible at typical zoom levels and wide enough
		/// to hide seam artifacts from float-precision mismatches.
		/// </summary>
		private const float SkirtOuterOffsetFraction = 0.01f;

		/// <summary>
		/// Maps a skirt-loop index (<c>-1 .. sideVertexCount-2</c>) to the [0,1] UV range
		/// used for interior vertices, with the outer ring pinned to a fixed offset outside
		/// the tile rather than one grid step. Without this, a 3x3 grid puts the skirt half
		/// a tile outside the edge and overlaps neighboring tiles.
		/// </summary>
		/// <param name="index">Loop index. <c>-1</c> is the left/top outer skirt row; <c>sideVertexCount-2</c> is the right/bottom outer skirt row.</param>
		/// <param name="interiorSteps">Number of grid segments along one tile side (<c>sampleCount</c>).</param>
		/// <param name="sideVertexCount">Total verts per tile axis including skirts.</param>
		private static float RatioForSkirtIndex(int index, int interiorSteps, int sideVertexCount)
		{
			if (index == -1)
			{
				return -SkirtOuterOffsetFraction;
			}
			if (index == sideVertexCount - 2)
			{
				return 1f + SkirtOuterOffsetFraction;
			}
			return (float)index / interiorSteps;
		}

		private MeshDataArray CreateBaseMeshSkirts(float size, int sideVertexCount)
		{
			//TODO use arrays instead of lists
			_newVertexList.Clear();
			_newNormalList.Clear();
			_newUvList.Clear();
			var _newTriangleList = new List<int>();
			var interiorSteps = sideVertexCount - 3;

			//012
			//345
			//678
			for (int y = -1; y < sideVertexCount - 1; y++)
			{
				var yrat = RatioForSkirtIndex(y, interiorSteps, sideVertexCount);
				for (int x = -1; x < sideVertexCount - 1; x++)
				{
					var xrat = RatioForSkirtIndex(x, interiorSteps, sideVertexCount);

					var xx = Mathf.LerpUnclamped(0, size, xrat);
					//lerp x/y swapped here because of the texture space conversion (y to -y)
					var yy = Mathf.LerpUnclamped(size, 0, yrat);

					var elevation = x < 0 || y < 0 || x == sideVertexCount-2 || y == sideVertexCount-2 ? -_skirtSize : 0;

					_newVertexList.Add(new Vector3(
						xx,
						elevation,
						-1 * yy));
					_newNormalList.Add(Constants.Math.Vector3Up);
					_newUvList.Add(new Vector2(xrat, yrat));
					//_newUvList.Add(new Vector2((1f/514) + (xrat * 512)/514, 1 - ((1f/514) + (yrat * 512)/514)));
				}
			}

			int vertA, vertB, vertC;

			var topQuadTris = new List<int>();
			for (int y = 0; y < sideVertexCount - 1; y++)
			{
				for (int x = 0; x < sideVertexCount - 1; x++)
				{
					vertA = (y * sideVertexCount) + x;
					vertB = (y * sideVertexCount) + x + sideVertexCount + 1;
					vertC = (y * sideVertexCount) + x + sideVertexCount;

					if (x == 0 || y == 0 || x == sideVertexCount - 2 || y == sideVertexCount - 2)
					{
						_newTriangleList.Add(vertA);
						_newTriangleList.Add(vertC);
						_newTriangleList.Add(vertB);
					}
					else
					{
						topQuadTris.Add(vertA);
						topQuadTris.Add(vertC);
						topQuadTris.Add(vertB);
					}

					vertA = (y * sideVertexCount) + x;
					vertB = (y * sideVertexCount) + x + 1;
					vertC = (y * sideVertexCount) + x + sideVertexCount + 1;
					
					if (x == 0 || y == 0 || x == sideVertexCount - 2 || y == sideVertexCount - 2)
					{
						_newTriangleList.Add(vertA);
						_newTriangleList.Add(vertC);
						_newTriangleList.Add(vertB);
					}
					else
					{
						topQuadTris.Add(vertA);
						topQuadTris.Add(vertC);
						topQuadTris.Add(vertB);
					}
				}
			}

			var mesh = new MeshDataArray();
			mesh.Vertices = _newVertexList.ToArray();
			mesh.Normals = _newNormalList.ToArray();
			mesh.Uvs = _newUvList.ToArray();
			topQuadTris.AddRange(_newTriangleList);
			mesh.Triangles.Add(topQuadTris.ToArray());
			return mesh;
		}
		#endregion
	}
}
