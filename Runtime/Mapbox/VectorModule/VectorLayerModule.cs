using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration;
using UnityEngine;
using Console = System.Console;

namespace Mapbox.VectorModule
{
	public class VectorLayerModule : ILayerModule
	{
		private Source<VectorData> _vectorSource;
		private MeshGenerationUnit _meshGenerationUnit;
		private VectorModuleSettings _vectorModuleSettings;
		private IMapInformation _mapInformation;
		
		private HashSet<CanonicalTileId> _retainedTiles;
		private HashSet<CanonicalTileId> _activeTiles;
		private HashSet<CanonicalTileId> _readyTiles;
		private HashSet<CanonicalTileId> _visibleTiles;
		
		public VectorLayerModule(IMapInformation mapInformation, Source<VectorData> source, MeshGenerationUnit meshGenerator, VectorModuleSettings vectorModuleSettings = null) : base()
		{
			_mapInformation = mapInformation;
			_vectorSource = source;
			_meshGenerationUnit = meshGenerator;
			_vectorModuleSettings = vectorModuleSettings ?? new VectorModuleSettings();
			_readyTiles = new HashSet<CanonicalTileId>();
			_vectorSource.CacheItemDisposed += ClearDisposedDataVisual;
			_retainedTiles = new HashSet<CanonicalTileId>();
			_activeTiles = new HashSet<CanonicalTileId>();
			_visibleTiles = new HashSet<CanonicalTileId>();
		}

		public virtual IEnumerator Initialize()
		{
			yield return _vectorSource.Initialize();
			yield return _meshGenerationUnit.Initialize();
		}

		public virtual void LoadTempTile(UnityMapTile tile)
		{
			
		}

		public virtual bool LoadInstant(UnityMapTile unityTile)
		{
			var targetId = GetTargetTileId(unityTile.CanonicalTileId);
			if (_readyTiles.Contains(targetId))
				return true;
			
			//Debug.Log(string.Format("Load Instant {0}, {1}, {2}" ,unityTile.CanonicalTileId, _vectorSource.CheckInstantData(unityTile.CanonicalTileId), _visualCache.ContainsKey(unityTile.CanonicalTileId)));
			if (!IsZinSupportedRange(targetId.Z)) return true;

			//this is wrong, it feels wrong
			//tile doesn't need data, only yhe visual object. why are we checking for data
			if (_vectorSource.GetInstantData(targetId, out var instantData) && 
			    unityTile.TerrainContainer.State == TileContainerState.Final)
			{
				if(!_meshGenerationUnit.IsInWork(targetId))
				{
					CreateVisual(targetId, instantData);
				}
			}

			return false;
		}

		public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles, Dictionary<UnwrappedTileId, UnityMapTile> activeTiles)
		{
			UpdateRetainedTiles(retainedTiles);
			UpdateActiveTileList(activeTiles);
			
			foreach (var tileId in _readyTiles)
			{
				var isRetained = _activeTiles.Contains(tileId) || _retainedTiles.Contains(tileId);
				UpdateVisibilityCallbacks(tileId,isRetained);
				_meshGenerationUnit.SetVisualActive(tileId, isRetained, _mapInformation);
			}

			_meshGenerationUnit.RetainTiles(_retainedTiles);
			var isReady = _vectorSource.RetainTiles(_retainedTiles);
			return isReady;
		}
		private void UpdateVisibilityCallbacks(CanonicalTileId tileId, bool isRetained)
		{
			if (isRetained && !_visibleTiles.Contains(tileId))
			{
				OnVectorMeshTurnVisible(tileId);
				_visibleTiles.Add(tileId);
				return;
			}
			if (!isRetained &&_visibleTiles.Contains(tileId))
			{
				OnVectorMeshTurnInvisible(tileId);
				_visibleTiles.Remove(tileId);
			}
		}

		private void UpdateActiveTileList(Dictionary<UnwrappedTileId, UnityMapTile> activeTiles)
		{
			_activeTiles.Clear();
			foreach (var mapTile in activeTiles)
			{
				_activeTiles.Add(GetTargetTileId(mapTile.Key.Canonical));
			}
		}

		public virtual void UpdatePositioning(IMapInformation information)
		{
			foreach (var tileId in _readyTiles)
			{
				var isRetained = _activeTiles.Contains(tileId) || _retainedTiles.Contains(tileId);
				if (isRetained)
				{
					_meshGenerationUnit.UpdateForView(tileId, information);
				}
			}
		}
		
		public virtual void OnDestroy()
		{
			_meshGenerationUnit.OnDestroy();
		}

		public void ReloadTile(CanonicalTileId tile)
		{
			var targetId = GetTargetTileId(tile);
			if (_readyTiles.Contains(targetId) && _vectorSource.GetInstantData(targetId, out var instantData))
			{
				ClearDisposedDataVisual(targetId);
				CreateVisual(targetId, instantData);
			}
		}

		public IEnumerable<CanonicalTileId> GetReadyTiles()
		{
			return _readyTiles;
		}

		public bool TryGetLayerVisualizer(string name, out IVectorLayerVisualizer visualizer)
		{
			return _meshGenerationUnit.TryGetLayerVisualizer(name, out visualizer);
		}
		
		public IEnumerable<CanonicalTileId> GetDataId(IEnumerable<CanonicalTileId> tileIdList)
		{
			return tileIdList.Select(GetTargetTileId).Distinct();
		}
		
		//COROUTINE METHODS only used in initialization so far
		#region coroutines
		public IEnumerator LoadAndProcessTileCoroutine(CanonicalTileId tile)
		{
			VectorData tileData = null;
			yield return _vectorSource.LoadTileCoroutine(tile, data => tileData = data);
			if (tileData != null)
			{
				yield return CreateVisualCoroutine(tile, tileData);
			}
		}
		
		public virtual IEnumerator LoadTileData(CanonicalTileId tileId, Action<MapboxTileData> callback = null)
		{
			yield return _vectorSource.LoadTileCoroutine(tileId, callback);
		}

		public virtual IEnumerator ProcessTileData(CanonicalTileId tileId)
		{
			if (_vectorSource.GetInstantData(tileId, out var data))
			{
				yield return CreateVisualCoroutine(tileId, data);
			}
		}

		public virtual IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
		{
			//this section loaded all data first and started processing once they are all loaded
			//commented this out and replaced it with the section below
			//new version loads and processes the data per tile so process doesn't wait for all tiles to load
			//performance difference is almost non-existent, second version felt more correct
			//---
			// List<VectorData> loadedTiles = null;
			// yield return _vectorSource.LoadTilesCoroutine(GetTargetTileId(tiles), (result) => { loadedTiles = result; });
			// var visualGenerations = loadedTiles.Select(x => CreateVisualCoroutine(x.TileId, x));
			// yield return visualGenerations.WaitForAll();
			//---

			var targetTileIds = new HashSet<CanonicalTileId>();
			foreach (var tile in tiles)
			{
				var targetId = GetTargetTileId(tile);
				if (IsZinSupportedRange(targetId.Z))
				{
					targetTileIds.Add(targetId);
				}
			}

			//we calculate the targetTileIds first and then start the process because multiple tiles targeting same
			//vector parent tile will cause problems inside the LoadAndProcessTileCoroutine method
			yield return targetTileIds.Select(LoadAndProcessTileCoroutine).WaitForAll();
		}

		public IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles)
		{
			var targetTiles = tiles.Where(x => IsZinSupportedRange(x.Z)).Select(GetTargetTileId).Distinct();
			return targetTiles.Select(x => LoadAndProcessTileCoroutine(x));
		}
		
		#endregion
		
		
		
		
		private bool IsZinSupportedRange(int targetZ)
		{
			return _vectorModuleSettings.RejectTilesOutsideZoom.x <= targetZ && _vectorModuleSettings.RejectTilesOutsideZoom.y >= targetZ && _vectorSource.IsZinSupportedRange(targetZ);
		}
		
		private void UpdateRetainedTiles(HashSet<CanonicalTileId> retainedTiles)
		{
			_retainedTiles.Clear();
			foreach (var tileId in retainedTiles)
			{
				var targetId = GetTargetTileId(tileId);
				if (IsZinSupportedRange(targetId.Z))
				{
					if(targetId.Z < _vectorModuleSettings.RejectTilesOutsideZoom.x)
						continue;
					_retainedTiles.Add(targetId);
				}
				
				if (!_readyTiles.Contains(targetId))
				{
					for (int i = targetId.Z; i >= _vectorModuleSettings.RejectTilesOutsideZoom.x; i--)
					{
						targetId = targetId.Parent;
						if (_readyTiles.Contains(targetId))
						{
							_retainedTiles.Add(targetId);
							break;
						}
					}
				}
			}
		}
		
		private CanonicalTileId GetTargetTileId(CanonicalTileId tileId)
		{
			var maxZoom = _vectorModuleSettings.DataSettings.ClampDataLevelToMax;
			if (tileId.Z >= maxZoom)
			{
				return tileId.Z > maxZoom
					? tileId.ParentAt(maxZoom)
					: tileId;
			}
			else
			{
				return tileId;
			}
		}
		
		private IEnumerable<CanonicalTileId> GetTargetTileId(IEnumerable<CanonicalTileId> tileIdList)
		{
			return tileIdList.Select(GetTargetTileId).ToList();
		}
		
		private void CreateVisual(CanonicalTileId tileId, VectorData vectorData, Action<MeshGenerationTaskResult> callback = null)
		{
			if (_readyTiles.Contains(tileId))
			{
				callback?.Invoke(new MeshGenerationTaskResult(TaskResultType.Success));
			}
			else if (!_meshGenerationUnit.IsInWork(vectorData.TileId))
			{
				_meshGenerationUnit.MeshGeneration(vectorData, (result =>
				{
					if (result != null)
					{
						switch (result.ResultType)
						{
							case TaskResultType.Success:
								_readyTiles.Add(tileId);
								_meshGenerationUnit.UpdateForView(tileId, _mapInformation);
								UpdateVisibilityCallbacks(tileId, true);
								OnVectorMeshCreated(vectorData.TileId, result.GeneratedObjects);
								break;
							case TaskResultType.DataProcessingFailure:
								_vectorSource.InvalidateData(vectorData.TileId);
								Debug.Log(result.ExceptionsAsString);
								break;
							case TaskResultType.Cancelled:
							{
								if (result.GeneratedObjects != null)
								{
									foreach (var gameObject in result.GeneratedObjects)
									{
										GameObject.Destroy(gameObject);
									}
								}

								break;
							}
						}
					}
					callback?.Invoke(result);
				}));
			}
		}

		private IEnumerator CreateVisualCoroutine(CanonicalTileId tileId, VectorData vectorData, Action<MeshGenerationTaskResult> callback = null)
		{
			var isMeshGenDone = false;
			CreateVisual(tileId, vectorData, (result) =>
			{
				isMeshGenDone = true;
				callback?.Invoke(result);
			});
			while (!isMeshGenDone)
			{
				yield return null;
			}
		}

		private void ClearDisposedDataVisual(CanonicalTileId tileId)
		{
			_readyTiles.Remove(tileId);
			_meshGenerationUnit.ClearDisposedDataVisual(tileId);
			OnVectorMeshDestroyed(tileId);
		}

		public Action<CanonicalTileId, IEnumerable<GameObject>> OnVectorMeshCreated = (_, _) => {};
		public Action<CanonicalTileId> OnVectorMeshDestroyed = _ => {};
		public Action<CanonicalTileId> OnVectorMeshTurnVisible = _ => {};
		public Action<CanonicalTileId> OnVectorMeshTurnInvisible = _ => {};
	}
}
