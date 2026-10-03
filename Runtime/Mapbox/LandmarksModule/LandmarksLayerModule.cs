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

namespace Mapbox.LandmarksModule
{
	/// <summary>
	/// Fetches 3D landmark tiles from the Mapbox 3D Buildings tileset, builds them with glTFast,
	/// and hides the basic buildings they overlap.
	///
	/// <b>Preview.</b> This module ships as preview in v3.2.0: its API and behaviour may change in a
	/// minor release. Dynamic map scaling is not supported. See
	/// <c>Documentation~/LandmarksModuleKnownGaps.md</c> for everything outstanding.
	/// </summary>
	public class LandmarksLayerModule : ILayerModule
	{
		/// <summary>
		/// Zoom the landmarks tileset publishes at. Render tiles are mapped up to this level to find
		/// the tile that carries their landmarks, and the overlap connectors transform building
		/// geometry into this level's tile space to test it against landmark footprints.
		/// </summary>
		public const int LandmarkDataZoom = 14;

		/// <summary>
		/// How close, in z14 tile units, a landmark footprint has to come to a tile edge before the
		/// overlap connectors also copy it into the neighbouring z14 tile.
		///
		/// Crossing the edge is not the right test. Vector tiles carry a buffer of roughly 1/64 of the
		/// tile past each edge, and VectorLayerVisualizer converts geometry without clipping it, so a
		/// building straddling the edge between tiles A and B is drawn by B's copy of it slightly past
		/// B's edge, into A. A landmark lying wholly inside A but close to that edge overlaps the
		/// overhang, yet was only ever stored under A - and the connectors look buildings up under
		/// their own z14 parent only, so B's copy was tested against nothing and stayed visible through
		/// the landmark. Replicating within this margin puts the footprint in B too.
		///
		/// Twice the typical buffer, because being generous is cheap: a footprint copied into a
		/// neighbour it does not actually reach only costs a few extra bbox rejections there.
		/// </summary>
		public const float NeighbourReplicationMargin = 1f / 32f;

		/// <summary>
		/// How many times a tile may fail to generate before it is given up on. The first failure is
		/// worth a retry - a truncated download decodes as a malformed glTF - but a tile that is
		/// genuinely broken must stop being retried, or it re-downloads, re-fails and logs an error
		/// on every sweep for the rest of the session.
		/// </summary>
		private const int MaxGenerationAttempts = 2;

		private Source<BuildingData> _buildingSource;
		private UnityContext _unityContext;
		private IMapInformation _mapInformation;

		/// <summary>
		/// Landmark visuals per data tile. Presence of a key means the tile is resolved and needs
		/// no further generation; the value is the visuals it produced and the import that built
		/// them.
		///
		/// Values are never null - a resolved tile with no visuals holds an entry with an empty
		/// object collection. That is an invariant, not a coincidence: three of the four readers
		/// here iterate the value directly without a null check, so a null would NRE on the next
		/// reposition, cull, or dispose. An empty collection carries the same information
		/// ("resolved, nothing to show") and every reader handles it correctly for free.
		/// </summary>
		private Dictionary<CanonicalTileId, LandmarkTileVisuals> _visualCache;
		private GltfVisualizer _meshGenerationUnit;

		private HashSet<CanonicalTileId> _retainedBuildingTiles;

		// Tiles that failed to generate, and how often. Kept because nothing else remembers a
		// failure: a tile that is absent from the visual cache and not in work looks exactly like a
		// tile nobody has tried yet, so RetainTiles would start it again on every sweep.
		private Dictionary<CanonicalTileId, int> _failedAttempts;

		// Tiles that have exhausted MaxGenerationAttempts. Treated as resolved-with-nothing-to-show
		// so they stop being regenerated and stop holding LoadInstant back, without pretending a
		// visual exists for them.
		private HashSet<CanonicalTileId> _unbuildableTiles;

		// Tiles whose data was disposed while their glTF load was still running. The result arrives
		// afterwards with nothing left to attach it to, and the callback releases it on sight
		// instead of caching it - see ClearDisposedDataVisual.
		private HashSet<CanonicalTileId> _disposedWhileGenerating;

		// Constructed in Initialize from the tile-lifecycle source handed in there, disposed in
		// OnDestroy. Null when no source was supplied, so every use is guarded - landmarks then
		// simply stay at ground level, which is the pre-terrain behaviour.
		private LandmarkTerrainSnapper _terrainSnapper;

		// Set by OnDestroy. A glTF load already in flight cannot be cancelled, so its result still
		// arrives after teardown; this is what stops the callback re-populating structures OnDestroy
		// has just cleared and raising TileFinished for a tile that will never be disposed.
		private bool _destroyed;

		public LandmarksLayerModule(UnityContext unityContext, IMapInformation mapInformation, MapService mapService,
			string sourceId, Material buildingMaterial) : base()
		{
			_unityContext = unityContext;
			_mapInformation = mapInformation;
			_visualCache = new Dictionary<CanonicalTileId, LandmarkTileVisuals>();
			_retainedBuildingTiles = new HashSet<CanonicalTileId>();
			_failedAttempts = new Dictionary<CanonicalTileId, int>();
			_unbuildableTiles = new HashSet<CanonicalTileId>();
			_disposedWhileGenerating = new HashSet<CanonicalTileId>();
			_buildingSource = mapService.GetBuildingSource(new VectorSourceSettings() { TilesetId = sourceId });
			_buildingSource.CacheItemDisposed += ClearDisposedDataVisual;
			_meshGenerationUnit = new GltfVisualizer(_unityContext, _mapInformation, buildingMaterial);
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
				return IsResolved(targetId);
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
					// Skip tiles whose mesh is already generated, already being generated, or given
					// up on. Without this, every call to LoadTileCoverToMemory (e.g. MapFlightController
					// re-checking readiness after each animated step) re-ran the full glTF mesh
					// generation for every already-finished landmark in the cover, not just new
					// ones. GenerateTask re-checks resolution itself, since this check runs before
					// the fetch and the tile can resolve while it is in flight; this one just saves
					// the fetch. Mirrors the check EnsureRetainedTilesReady uses for RetainTiles.
					if (!IsResolved(targetId) && !_meshGenerationUnit.IsInWork(targetId))
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
				foreach (var gameObject in pair.Value.Objects)
				{
					// Destroyed-object check, not a null-reference one: reading .transform off a
					// destroyed GameObject throws, so anything that destroyed a landmark from
					// outside would otherwise take every future reposition down with it.
					if (gameObject == null) continue;
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
			// Set before anything is torn down. Loads still in flight resume after this and check
			// it; the visualizer has its own copy of the same flag for the same reason.
			_destroyed = true;

			_terrainSnapper?.Dispose();
			_terrainSnapper = null;

			// Subscribed in the constructor, so it has to come off here or a destroyed module keeps
			// being called back by a source that outlives it.
			_buildingSource.CacheItemDisposed -= ClearDisposedDataVisual;

			// Visuals for tiles never released through ClearDisposedDataVisual are still parented
			// under the generation root. GltfVisualizer.OnDestroy takes its own root with it, but
			// the imports that built these are held here, and only disposing them destroys the
			// meshes, materials and textures glTFast created for them.
			foreach (var pair in _visualCache)
			{
				pair.Value.Release();
			}
			_visualCache.Clear();
			_failedAttempts.Clear();
			_unbuildableTiles.Clear();
			_disposedWhileGenerating.Clear();

			// Owns the "gltf root" GameObject, which nothing else references - re-initializing a map
			// in one session used to leak one each time.
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
					foreach (var o in visual.Value.Objects)
					{
						if (o != null) o.SetActive(false);
					}
				}
			}
		}

		private void EnsureRetainedTilesReady()
		{
			foreach (var tileId in _retainedBuildingTiles)
			{
				if (_visualCache.TryGetValue(tileId, out var visuals))
				{
					// No null check needed: cached values are never null, and a resolved tile with
					// no visuals is an empty collection this loop simply skips.
					foreach (var visual in visuals.Objects)
					{
						if (visual != null) visual.SetActive(true);
					}
				}
				else if (!_unbuildableTiles.Contains(tileId) && !_meshGenerationUnit.IsInWork(tileId))
				{
					GenerateVisualsForTile(tileId);
				}
				//else: mesh generation is currently happening, or the tile has been given up on
			}
		}

		private IEnumerator CreateVisualCoroutine(CanonicalTileId tileId, BuildingData buildingData)
		{
			var task = GenerateTask(tileId, buildingData);
			while (!task.IsCompleted)
			{
				yield return null;
			}

			ReportIfFaulted(tileId, task);
		}

		private void GenerateVisualsForTile(CanonicalTileId tileId)
		{
			if (_buildingSource.GetInstantData(tileId, out var vectorData))
			{
				// Fire and forget, but not unobserved. The result callback runs outside Generate's
				// own try/catch by design, so anything it throws is captured into this Task and
				// would otherwise be lost entirely - there is no await and nobody reads Exception.
				GenerateTask(tileId, vectorData).ContinueWith(
					t => ReportIfFaulted(tileId, t),
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
			}
			else
			{
				//state, data isn't available yet
			}
		}

		private static void ReportIfFaulted(CanonicalTileId tileId, Task task)
		{
			if (!task.IsFaulted) return;
			Debug.LogError($"Landmark generation for tile {tileId} threw: {task.Exception}");
		}

		private Task GenerateTask(CanonicalTileId tileId, BuildingData vectorData)
		{
			// Callers check this before they start, but the cover path checks it before fetching
			// the data and only calls in here once the fetch completes, frames later. In that gap
			// RetainTiles can see the same data arrive and resolve the tile itself - synchronously,
			// for an empty tile - so by the time the cover path gets here the cache already holds
			// the key and the Add below would throw. Checked here, at the last moment, so no
			// caller can race it.
			if (IsResolved(tileId))
			{
				return Task.CompletedTask;
			}

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

				// The module was destroyed, or this tile's data was evicted, while the load ran.
				// Either way there is nothing left to attach the result to and nothing that will
				// ever release it, so it is released here instead of being cached. Caching it was
				// how landmark GameObjects ended up surviving their own tile's disposal, invisible
				// and unreachable, with the snapper still walking their transforms every frame.
				if (_destroyed || _disposedWhileGenerating.Remove(tileId))
				{
					new LandmarkTileVisuals(result.GeneratedObjects, result.Import).Release();
					return;
				}

				// Abandoned on purpose, so it is not a failure and must not be counted as one.
				// The tile is simply left unresolved: if it comes back into the cover it will be
				// generated again, and if it does not, nothing was owed for it.
				if (result.ResultType == TaskResultType.Cancelled)
				{
					return;
				}

				if (result.ResultType == TaskResultType.DataProcessingFailure ||
				    result.ResultType == TaskResultType.GameObjectFailure)
				{
					RecordGenerationFailure(tileId, result.ResultType);
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
					_visualCache.Add(tileId, new LandmarkTileVisuals(null, result.Import));
					return;
				}

				foreach (var gameObject in result.GeneratedObjects)
				{
					if (gameObject == null) continue;

					var tr = gameObject.transform;
					tr.gameObject.SetActive(true);
					tr.gameObject.name = tileId.ToString();
					PositionBuilding(tileId, tr);
				}

				// Announced per building, walking out from the anchors rather than down from the
				// tile root, because the anchor is the only thing that knows a mesh's landmark id -
				// the tile publishes it on the building node and nothing downstream can recover it
				// from the geometry. A building with several primitives, or with mesh nodes nested
				// below its root, contributes all of them and they all carry the same id.
				//
				// This inherits the module's existing assumption that every building is a scene
				// root node carrying its own primitive, since that is what produces an anchor: a
				// root node with no primitive of its own would have no anchor, and its meshes would
				// go unannounced here. The same node would already be unsnappable to terrain and
				// mispositioned by FlipYZ, so it is one assumption, not a new one.
				//
				// No active check: GetComponentsInChildren skips inactive objects by default, and
				// footprints - the only geometry that must not be announced - were deactivated in
				// EndScene before this runs.
				foreach (var anchor in result.Anchors)
				{
					if (anchor.Transform == null) continue;

					foreach (var meshObject in anchor.Transform.GetComponentsInChildren<MeshFilter>())
					{
						BuildingLoaded(meshObject.gameObject, anchor.LandmarkId);
					}
				}

				// The import comes with the visuals and is disposed with them. It is the only thing
				// that destroys the meshes, materials and textures glTFast built for this tile, and
				// Unity reclaims none of those on its own.
				_visualCache.Add(tileId, new LandmarkTileVisuals(result.GeneratedObjects, result.Import));
				_failedAttempts.Remove(tileId);

				// After the roots are positioned, since snapping writes each building's local Y
				// relative to a root that PositionBuilding has already placed.
				_terrainSnapper?.Register(tileId, result.Anchors);

				TileFinished(tileId, result.FootprintResults);
			});
		}

		/// <summary>
		/// Counts a failed generation and, once a tile has used up its attempts, stops trying.
		///
		/// The retry itself has to invalidate the cached bytes, since a glTF that will not parse is
		/// most often a truncated download - but invalidating drops the tile from memory and disk,
		/// so the next sweep re-downloads it. That is the right thing to do once and the wrong thing
		/// to do forever: a tile that is genuinely malformed would otherwise re-download, re-fail and
		/// log an error on every sweep for the rest of the session.
		///
		/// A GameObject failure is counted but never invalidated. The bytes parsed; re-fetching them
		/// cannot change the outcome.
		/// </summary>
		private void RecordGenerationFailure(CanonicalTileId tileId, TaskResultType resultType)
		{
			_failedAttempts.TryGetValue(tileId, out var attempts);
			attempts++;
			_failedAttempts[tileId] = attempts;

			if (attempts >= MaxGenerationAttempts)
			{
				_unbuildableTiles.Add(tileId);
				Debug.LogError(
					$"Giving up on landmark tile {tileId} after {attempts} attempts ({resultType}). " +
					"It will be retried if its data is re-fetched.");
				return;
			}

			if (resultType == TaskResultType.DataProcessingFailure)
			{
				_buildingSource.InvalidateData(tileId);
			}
		}

		/// <summary>
		/// Whether this tile needs no further generation - either it produced visuals, or it failed
		/// often enough that it never will. Both answers are "stop asking"; only one of them has
		/// something to show.
		/// </summary>
		private bool IsResolved(CanonicalTileId tileId)
		{
			return _visualCache.ContainsKey(tileId) || _unbuildableTiles.Contains(tileId);
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

			// The data is gone, so whatever made this tile unbuildable is gone with it. A re-fetch
			// deserves a fresh set of attempts.
			_failedAttempts.Remove(tileId);
			_unbuildableTiles.Remove(tileId);

			if (_visualCache.TryGetValue(tileId, out var visuals))
			{
				visuals.Release();
				_visualCache.Remove(tileId);
				TileDisposed(tileId);
			}
			else if (_meshGenerationUnit.IsInWork(tileId))
			{
				// Disposed mid-load. Cancel it - the bytes it is decoding have just been evicted, so
				// finishing would build a tile with nowhere to go. The load still reports back,
				// which is what the marker below is for: the result lands after this call, the
				// cache has no entry to find it by, and CacheItemDisposed will not fire again.
				_meshGenerationUnit.Cancel(tileId);
				_disposedWhileGenerating.Add(tileId);
			}
		}

		private bool GetDataId(CanonicalTileId tileId, out CanonicalTileId targetId)
		{
			if (tileId.Z >= LandmarkDataZoom)
			{
				targetId = tileId.ParentAt(LandmarkDataZoom);
				return true;
			}
			else
			{
				targetId = tileId;
				return false;
			}
		}

		/// <summary>
		/// One resolved tile's visuals, together with the glTF import that produced them.
		///
		/// The two are kept in one place because they have to be released in one step, in one order:
		/// destroying the GameObjects first, then disposing the import. Disposing the import is what
		/// destroys the meshes, materials and textures glTFast created, and Unity never reclaims
		/// those on its own - so dropping the import leaks the whole tile's geometry and PBR texture
		/// set for the rest of the session. Disposing it before the GameObjects are gone would
		/// instead pull the meshes out from under renderers that are still using them.
		/// </summary>
		private class LandmarkTileVisuals
		{
			public readonly IEnumerable<GameObject> Objects;

			private readonly MapboxGltfImport _import;

			public LandmarkTileVisuals(IEnumerable<GameObject> objects, MapboxGltfImport import)
			{
				Objects = objects ?? Enumerable.Empty<GameObject>();
				_import = import;
			}

			/// <summary>
			/// Destroys the visuals, then disposes the import. In that order, and never the other
			/// way round: Dispose destroys the meshes the renderers being torn down are still
			/// using.
			/// </summary>
			public void Release()
			{
				foreach (var o in Objects)
				{
					if (o == null) continue;
					GameObject.Destroy(o);
				}

				_import?.Dispose();
			}
		}

		public Action<CanonicalTileId, List<Vector3[]>> TileFinished = (id, list) =>  { };
		public Action<CanonicalTileId> TileDisposed = (id) => { };
		/// <summary>
		/// Raised once for every landmark mesh brought on screen, with the GameObject carrying the
		/// mesh and the id the tile publishes for the landmark it belongs to.
		///
		/// The id is the only way to ask "which landmark is this" from a raycast hit: the footprint
		/// and building nodes of a landmark publish it under different keys, it is captured during
		/// instantiation, and nothing about the resulting geometry records it.
		///
		/// There is no matching per-building unloaded event. Visuals are released a whole tile at a
		/// time, which <see cref="TileDisposed"/> already reports, and a subscriber holding a
		/// landmark GameObject can compare it against null like any other destroyed Unity object.
		/// </summary>
		public Action<GameObject, string> BuildingLoaded = (go, landmarkId) => { };
	}
}
