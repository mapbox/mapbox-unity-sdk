using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using UnityEngine;

namespace Mapbox.LandmarkModule
{
	public class LandmarksLayerModule : ILayerModule
	{
		private Source<BuildingData> _buildingSource;
		private UnityContext _unityContext;
		private IMapInformation _mapInformation;
		/// <summary>
		/// Landmark visuals per data tile. Presence of a key means the tile is resolved and needs
		/// no further generation; the value is the visuals it produced.
		///
		/// Values are never null - a resolved tile with no visuals holds an empty collection. That
		/// is an invariant, not a coincidence: three of the four readers here iterate the value
		/// directly without a null check, so a null would NRE on the next reposition, cull, or
		/// dispose. An empty collection carries the same information ("resolved, nothing to show")
		/// and every reader handles it correctly for free.
		/// </summary>
		private Dictionary<CanonicalTileId, IEnumerable<GameObject>> _visualCache;
		private GltfVisualizer _meshGenerationUnit;

		private HashSet<CanonicalTileId> _retainedBuildingTiles;

		// Constructed in Initialize from the tile-lifecycle source handed in there, disposed in
		// OnDestroy. Null when no source was supplied, so every use is guarded - landmarks then
		// simply stay at ground level, which is the pre-terrain behaviour.
		private LandmarkTerrainSnapper _terrainSnapper;

		public LandmarksLayerModule(UnityContext unityContext, IMapInformation mapInformation, MapService mapService,
			string sourceId, Material buildingMaterial) : base()
		{
			_unityContext = unityContext;
			_mapInformation = mapInformation;
			_visualCache = new Dictionary<CanonicalTileId, IEnumerable<GameObject>>();
			_retainedBuildingTiles = new HashSet<CanonicalTileId>();
			_buildingSource = mapService.GetBuildingSource(new VectorSourceSettings() { TilesetId = sourceId });
			_buildingSource.CacheItemDisposed += ClearDisposedDataVisual;
			_meshGenerationUnit = new GltfVisualizer(_unityContext, _mapInformation, buildingMaterial);
			// _meshGenerationUnit.LandmarkMeshResults += (id, results) =>
			// {
			// 	LandmarkMeshResults(id, results);
			// };
			// _meshGenerationUnit.LandmarkFootprintCreated += (id, results) =>
			// {
			// 	LandmarkFootprintCreated(id, results);
			// };
		}

		public virtual IEnumerator Initialize(ITileLifecycleSource tileLifecycle)
		{
			// Before the first yield, so the snapper is watching tile loads while the rest of this
			// method waits on network work. Idempotent: a second snapper would double-subscribe
			// TileLoaded and leak the first one's coroutine.
			if (_terrainSnapper == null && tileLifecycle != null)
			{
				_terrainSnapper = new LandmarkTerrainSnapper(tileLifecycle, _mapInformation);
			}

			yield return _buildingSource.Initialize();
			yield return _meshGenerationUnit.Initialize();
		}

		public virtual void LoadTempTile(UnityMapTile tile)
		{
		}

		public virtual bool LoadInstant(UnityMapTile unityTile)
		{
			if (GetDataId(unityTile.CanonicalTileId, out var targetId))
			{
				return _visualCache.ContainsKey(targetId);
			}

			return true;
		}

		public IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
		{
			yield return null;
		}

		public IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles)
		{
			foreach (var tileId in tiles)
			{
				if (GetDataId(tileId, out var targetId))
				{
					// Skip tiles whose mesh is already generated or already being generated.
					// Without this, every call to LoadTileCoverToMemory (e.g. MapFlightController
					// re-checking readiness after each animated step) re-ran the full glTF mesh
					// generation for every already-finished landmark in the cover, not just new
					// ones - GenerateTask has no guard of its own and even calling it again would
					// have thrown on the second _visualCache.Add for the same key. Mirrors the
					// same check EnsureRetainedTilesReady already uses for the RetainTiles path.
					if (!_visualCache.ContainsKey(targetId) && !_meshGenerationUnit.IsInWork(targetId))
					{
						yield return LoadAndProcessTileCoroutine(targetId);
					}
				}
			}
		}

		public IEnumerator LoadAndProcessTileCoroutine(CanonicalTileId tile)
		{
			BuildingData buildingData = null;
			yield return _buildingSource.LoadTileCoroutine(tile, data => buildingData = data);
			if (buildingData != null)
			{
				yield return CreateVisualCoroutine(tile, buildingData);
			}
		}

		public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
		{
			UpdateRetainedList(retainedTiles);

			var isReady = _buildingSource.RetainTiles(_retainedBuildingTiles);

			EnsureRetainedTilesReady();

			ClearModelsUnusedTiles();

			return isReady;
		}

		public virtual void UpdatePositioning(IMapInformation information)
		{
			foreach (var pair in _visualCache)
			{
				foreach (var gameObject in pair.Value)
				{
					PositionBuilding(pair.Key, gameObject.transform);
				}
			}

			// PositionBuilding only places tile roots, and their Y is always 0. Each building's own
			// height above that root is derived from map scale, so it has to be recomputed here or
			// every landmark would sit at the elevation of whatever scale was current when its
			// terrain was first sampled - visibly wrong after one zoom step under
			// DynamicScalingMapInformation, where Scale tracks zoom.
			_terrainSnapper?.ReapplyForViewChange();
		}

		public void OnDestroy()
		{
			_terrainSnapper?.Dispose();
			_terrainSnapper = null;

			// Subscribed in the constructor, so it has to come off here or a destroyed module keeps
			// being called back by a source that outlives it.
			_buildingSource.CacheItemDisposed -= ClearDisposedDataVisual;

			// Visuals for tiles never released through ClearDisposedDataVisual are still parented
			// under the generation root. GltfVisualizer.OnDestroy takes its own root with it, but
			// these are the ones tracked here, so clearing them keeps the two views consistent.
			foreach (var pair in _visualCache)
			{
				foreach (var visual in pair.Value)
				{
					if (visual != null) GameObject.Destroy(visual);
				}
			}
			_visualCache.Clear();

			// Owns a GameObject and a pre-warmed pool of 20, neither of which anything else
			// references - re-initializing a map in one session used to leak a full set each time.
			_meshGenerationUnit.OnDestroy();
			_buildingSource.OnDestroy();
		}



		private void UpdateRetainedList(HashSet<CanonicalTileId> retainedTiles)
		{
			_retainedBuildingTiles.Clear();
			foreach (var tileId in retainedTiles)
			{
				if (GetDataId(tileId, out var targetId))
				{
					_retainedBuildingTiles.Add(targetId);
				}
			}
		}

		private void ClearModelsUnusedTiles()
		{
			foreach (var visual in _visualCache)
			{
				if (!_retainedBuildingTiles.Contains(visual.Key))
				{
					foreach (var o in visual.Value)
					{
						o.SetActive(false);
					}
				}
			}
		}

		private void EnsureRetainedTilesReady()
		{
			foreach (var tileId in _retainedBuildingTiles)
			{
				if (!_visualCache.ContainsKey(tileId) && !_meshGenerationUnit.IsInWork(tileId))
				{
					GenerateVisualsForTile(tileId);
				}
				else if (_visualCache.TryGetValue(tileId, out var visuals))
				{
					// No null check needed: cached values are never null, and a resolved tile with
					// no visuals is an empty collection this loop simply skips.
					foreach (var visual in visuals)
					{
						visual.SetActive(true);
					}
					//state, output is in the visual cache or mesh generation is currently happening
				}
			}
		}

		private IEnumerator CreateVisualCoroutine(CanonicalTileId tileId, BuildingData buildingData)
		{
			var task = GenerateTask(tileId, buildingData);
			while (task.Status < TaskStatus.RanToCompletion)
			{
				yield return null;
			}
		}

		private void GenerateVisualsForTile(CanonicalTileId tileId)
		{
			if (_buildingSource.GetInstantData(tileId, out var vectorData))
			{
				GenerateTask(tileId, vectorData);
			}
			else
			{
				//state, data isn't available yet
			}
		}

		private Task GenerateTask(CanonicalTileId tileId, BuildingData vectorData)
		{
			return _meshGenerationUnit.Generate(vectorData, result =>
			{
				// Another call owns the in-flight generation and will report its outcome to its
				// own callback, so there is nothing to record here. Returning early is not just
				// tidiness: this result carries no objects, so without it the null check below
				// would cache a null visual for a tile that is mid-generation, and the owning
				// call's own _visualCache.Add would then throw on the duplicate key.
				if (result.ResultType == TaskResultType.AlreadyInProgress)
				{
					return;
				}

				if (result.ResultType == TaskResultType.DataProcessingFailure ||
				    result.ResultType == TaskResultType.GameObjectFailure)
				{
					_buildingSource.InvalidateData(tileId);
					return;
				}

				// Defensive: no current producer reports a result with null objects that reaches
				// here, but if one ever does, the tile still has to be recorded as resolved or it
				// would be regenerated forever. Cached as an empty collection rather than null so
				// the readers that iterate this value without a null check stay correct - see the
				// invariant on _visualCache. The old code stored null here, which is what made
				// UpdatePositioning, ClearModelsUnusedTiles and ClearDisposedDataVisual able to NRE.
				//
				// The GameObjectFailure test the condition used to carry was dead: the guard above
				// returns on that value before ever reaching this line.
				if (result.GeneratedObjects == null)
				{
					_visualCache.Add(tileId, Enumerable.Empty<GameObject>());
					return;
				}

				foreach (var gameObject in result.GeneratedObjects)
				{
					var tr = gameObject.transform;
					if (tr == null) continue;

					tr.gameObject.SetActive(true);
					tr.gameObject.name = tileId.ToString();
					PositionBuilding(tileId, tr);

					foreach (var meshObject in gameObject.GetComponentsInChildren<MeshFilter>())
					{
						if (meshObject.gameObject.activeSelf)
						{
							BuildingLoaded(meshObject.gameObject);
						}
					}
				}

				_visualCache.Add(tileId, result.GeneratedObjects);

				// After the roots are positioned, since snapping writes each building's local Y
				// relative to a root that PositionBuilding has already placed.
				_terrainSnapper?.Register(tileId, result.Anchors);

				TileFinished(tileId, result.FootprintResults);
			});
		}

		private void PositionBuilding(CanonicalTileId tileId, Transform tr)
		{
			_mapInformation.PositionObjectFor(tileId, out var pos, out var scale);
			tr.position = pos;
			//landmark x/z are in 0-1 space. heights though, are in real world meters. so we don't scale tile Y here.
			//tr.localScale = new Vector3(scale.x, 1 / (_mapInformation.Scale * (float)Conversions.LatitudeElevationCompensation(_mapInformation.LatitudeLongitude.Latitude)), scale.y);
			tr.localScale = new Vector3(scale.x, 1, scale.y);
		}



		private void ClearDisposedDataVisual(CanonicalTileId tileId)
		{
			// Unconditionally, and before the cache check: the snapper holds transforms that are
			// about to be destroyed, and a tile can have anchors registered even on a path where
			// the visual cache entry has already gone.
			_terrainSnapper?.Unregister(tileId);

			if (_visualCache.TryGetValue(tileId, out var gameObjects))
			{
				foreach (var o in gameObjects)
				{
					BuildingUnloaded(o);
					GameObject.Destroy(o);
				}

				_visualCache.Remove(tileId);
				TileDisposed(tileId);
			}
		}

		private bool GetDataId(CanonicalTileId tileId, out CanonicalTileId targetId)
		{
			if (tileId.Z >= 14)
			{
				targetId = tileId.ParentAt(14);
				return true;
			}
			else
			{
				targetId = tileId;
				return false;
			}
		}

		public Action<CanonicalTileId, List<Vector3[]>> TileFinished = (id, list) =>  { };
		public Action<CanonicalTileId> TileDisposed = (id) => { };
		public Action<CanonicalTileId, List<Vector3[]>> LandmarkFootprintCreated = (id, polygons) => { };
		public Action<CanonicalTileId, List<Mesh>> LandmarkMeshResults = (id, results) => { };
		public Action<GameObject> BuildingLoaded = (s) => { };
		public Action<GameObject> BuildingUnloaded = (s) => { };
	}
}
