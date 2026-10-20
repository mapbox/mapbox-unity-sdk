using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mapbox.BaseModule.Unity
{
	public class UnityMapTile : MonoBehaviour
	{
		private Action<UnityMapTile> OnElevationValuesUpdated = (t) => { };
		public Action<UnwrappedTileId> OnDataDisposed = (t) => { };
		public UnwrappedTileId UnwrappedTileId { get; private set; }
		public CanonicalTileId CanonicalTileId { get; private set; }
		public float TileScale { get; private set; }

		//change this with list T : containers?
		public UnityTileTerrainContainer TerrainContainer;
		public UnityTileImageContainer ImageContainer;

		private static readonly int TileScalePropertyId = Shader.PropertyToID("_TileScale");
		
		private MeshRenderer _meshRenderer;
		public MeshRenderer MeshRenderer => _meshRenderer;

		public Material Material;
		private MeshFilter _meshFilter;
		public MeshFilter MeshFilter => _meshFilter;
		public List<UnityMapTile> Children;

		// Per-tile state (height texture, scale-offset, elevation multiplier, etc.) is
		// pushed via this MaterialPropertyBlock instead of a per-tile Material instance.
		// One Material is shared across the entire tile pool (set in TileCreator); the
		// win is avoiding a Material clone per renderer (reading Renderer.material clones,
		// SetPropertyBlock does not). This pattern is NOT SRP-Batcher-compatible — any
		// SetPropertyBlock on a renderer excludes it from the SRP Batcher. It is also not
		// GPU-instanced today (materials have m_EnableInstancingVariants:0 and the shader
		// has no UNITY_INSTANCING_BUFFER block); see project_terrain_instancing for what
		// a real batching rework would entail.
		private MaterialPropertyBlock _propertyBlock;
		public MaterialPropertyBlock PropertyBlock
		{
			get
			{
				if (_propertyBlock == null)
				{
					_propertyBlock = new MaterialPropertyBlock();
				}
				return _propertyBlock;
			}
		}

		/// <summary>
		/// Pushes the cached <see cref="PropertyBlock"/> onto the renderer. Call once
		/// after a logical batch of property mutations (the block can hold many properties
		/// at once, so multiple <c>Set*</c> calls between two <c>Apply</c> calls coalesce).
		/// </summary>
		public void ApplyPropertyBlock()
		{
			if (_meshRenderer != null && _propertyBlock != null)
			{
				_meshRenderer.SetPropertyBlock(_propertyBlock);
			}
		}

		public int MeshVertexCount = 0;

		//public bool IsTemporary = false;

		public LoadingState LoadingState;

		public void Awake()
		{
			ImageContainer = new UnityTileImageContainer(this, DataDisposed);
			TerrainContainer = new UnityTileTerrainContainer(this, ElevationUpdatedCallback, DataDisposed);
			
			_meshRenderer = gameObject.AddComponent<MeshRenderer>();
			_meshFilter = gameObject.AddComponent<MeshFilter>();
			_meshFilter.sharedMesh = new Mesh();
		}

		public void Initialize(UnwrappedTileId tileId, float scale)
		{
			TileScale = 1 / scale;
			UnwrappedTileId = tileId;
			CanonicalTileId = tileId.Canonical;
#if UNITY_EDITOR
			gameObject.name = tileId.ToString();
#endif

			PropertyBlock.SetFloat(TileScalePropertyId, TileScale);
			ApplyPropertyBlock();
		}
		
		public void ElevationUpdatedCallback()
		{
			if (_meshRenderer != null)
			{
				var centerHeight = (TerrainContainer.TerrainData.MaxElevation + TerrainContainer.TerrainData.MinElevation) / 2 * TileScale;
				var boxHeight = (TerrainContainer.TerrainData.MaxElevation - TerrainContainer.TerrainData.MinElevation)  * TileScale;
				// Renderer.localBounds overrides the Mesh's bounds for culling without
				// cloning the Mesh. Using MeshFilter.mesh.bounds would silently clone the
				// sharedMesh, which defeats per-tile Mesh sharing.
				_meshRenderer.localBounds = new Bounds(new Vector3(.5f, centerHeight, -.5f), new Vector3(1, boxHeight, 1));
			}
			OnElevationValuesUpdated(this);
		}

		// Hard cap used for fallback mesh bounds. Earth's highest point is ~8849m; 10000m
		// gives a safe margin. Oversize bounds only widen the frustum test; they don't add
		// any draw cost since there is no geometry outside the real elevation range.
		private const float FallbackMaxElevationMeters = 10000f;

		/// <summary>
		/// Applies a conservative mesh bounds that covers the full plausible terrain height
		/// range so shader-displaced vertices stay inside the mesh's frustum culling volume
		/// before real <c>MinElevation</c>/<c>MaxElevation</c> are known (or permanently,
		/// when CPU extraction is disabled). A later call to
		/// <see cref="ElevationUpdatedCallback"/> tightens the bounds if CPU elevation
		/// eventually arrives.
		/// </summary>
		public void SetFallbackMeshBounds()
		{
			if (_meshRenderer == null)
			{
				return;
			}
			var boxHeight = FallbackMaxElevationMeters * TileScale;
			var centerHeight = boxHeight * 0.5f;
			_meshRenderer.localBounds = new Bounds(new Vector3(.5f, centerHeight, -.5f), new Vector3(1, boxHeight, 1));
		}
		
		public void Recycle()
		{
			gameObject.SetActive(false);
			ImageContainer.GetAndClearImageData();
			TerrainContainer.GetAndClearTerrainData();
		}

		private void DataDisposed()
		{
			OnDataDisposed(this.UnwrappedTileId);
		}
		
		private void OnDestroy()
		{
			ImageContainer.OnDestroy();
			TerrainContainer.OnDestroy();

			// Unity does not auto-free runtime-created Meshes when the GameObject is
			// destroyed. Release the render mesh allocated in Awake and any dedicated
			// collider mesh allocated by ElevatedTerrainStrategy. Skip the shader-mode
			// shared flat mesh — it is owned by the strategy and disposed via
			// TerrainLayerModule.OnDestroy. The identity checks avoid double-freeing
			// an aliased render/collider mesh (FlatTerrainStrategy). Colliders may live
			// on a dedicated-layer child GameObject; scan children too. Names are pulled
			// from the strategy's constants so renames stay in sync.
			var renderMesh = _meshFilter != null ? _meshFilter.sharedMesh : null;
			var colliders = GetComponentsInChildren<MeshCollider>(includeInactive: true);
			foreach (var mc in colliders)
			{
				if (mc.sharedMesh != null &&
				    mc.sharedMesh.name == ElevatedTerrainStrategy.TerrainColliderMeshName &&
				    mc.sharedMesh != renderMesh)
				{
					Destroy(mc.sharedMesh);
				}
			}
			// Destroy the render mesh if it's a per-tile instance. Skip the strategy-owned
			// shared flat mesh (those are destroyed centrally in the strategy's OnDestroy).
			// Both the unnamed Awake mesh and named per-tile CPU meshes get freed here.
			if (renderMesh != null && renderMesh.name != ElevatedTerrainStrategy.SharedFlatMeshName)
			{
				Destroy(renderMesh);
			}
		}
	}

	public enum LoadingState
	{
		None,
		Temporary,
		Finished,
		Filler
	}
}
