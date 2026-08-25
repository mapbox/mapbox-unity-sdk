using System;
using System.Collections;
using System.Collections.Generic;
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
		private Dictionary<CanonicalTileId, IEnumerable<GameObject>> _visualCache;
		private GltfVisualizer _meshGenerationUnit;

		private HashSet<CanonicalTileId> _retainedBuildingTiles;

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

		public virtual IEnumerator Initialize()
		{
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
					yield return LoadAndProcessTileCoroutine(targetId);
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
		}

		public void OnDestroy()
		{
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
					if (visuals == null) continue;

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
				if (result.ResultType == TaskResultType.DataProcessingFailure ||
				    result.ResultType == TaskResultType.GameObjectFailure)
				{
					_buildingSource.InvalidateData(tileId);
					return;
				}

				if (result.ResultType == TaskResultType.GameObjectFailure || result.GeneratedObjects == null)
				{
					_visualCache.Add(tileId, null);
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
