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
				_meshGenerationUnit.SetVisualActive(tileId, _activeTiles.Contains(tileId) || _retainedTiles.Contains(tileId), _mapInformation);
			}

			_meshGenerationUnit.RetainTiles(_retainedTiles);
			var isReady = _vectorSource.RetainTiles(_retainedTiles);
			return isReady;
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
				var isRetained = _retainedTiles.Contains(tileId);
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
			if (_vectorSource.GetInstantData(targetId, out var instantData))
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
		
		
		//COROUTINE METHODS only used in initialization so far
		#region coroutines
		public IEnumerator LoadAndProcessTileCoroutine(CanonicalTileId tile)
		{
			VectorData tileData = null;
			yield return _vectorSource.LoadTileCoroutine(tile, data => tileData = data);
			if (tileData == null)
			{
				Debug.LogWarning("Loaded tile data is null");
				yield break;
			}
			yield return CreateVisualCoroutine(tile, tileData);
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
				targetTileIds.Add(targetId);
			}

			//we calculate the targetTileIds first and then start the process because multiple tiles targeting same
			//vector parent tile will cause problems inside the LoadAndProcessTileCoroutine method
			yield return targetTileIds.Select(LoadAndProcessTileCoroutine).WaitForAll();
		}

		#endregion
		
		
		
		
		private bool IsZinSupportedRange(int targetZ)
		{
			return _vectorModuleSettings.DataSettings.ActiveZoomLevelRange.x <= targetZ && _vectorModuleSettings.DataSettings.ActiveZoomLevelRange.y >= targetZ && _vectorSource.IsZinSupportedRange(targetZ);
		}
		
		private void UpdateRetainedTiles(HashSet<CanonicalTileId> retainedTiles)
		{
			_retainedTiles.Clear();
			foreach (var tileId in retainedTiles)
			{
				var targetId = GetTargetTileId(tileId);
				if (IsZinSupportedRange(targetId.Z))
				{
					if(targetId.Z < _vectorModuleSettings.DataSettings.ActiveZoomLevelRange.x)
						continue;
					_retainedTiles.Add(targetId);
				}

				if (_vectorModuleSettings.LoadBackgroundData)
				{
					if (!_readyTiles.Contains(targetId))
					{
						for (int i = targetId.Z; i >= _vectorModuleSettings.DataSettings.ActiveZoomLevelRange.x; i--)
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
		}
		
		private CanonicalTileId GetTargetTileId(CanonicalTileId tileId)
		{
			var maxZoom = (int)_vectorModuleSettings.DataSettings.ActiveZoomLevelRange.y;
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
			if (!_meshGenerationUnit.IsInWork(vectorData.TileId))
			{
				_meshGenerationUnit.MeshGeneration(vectorData, (result =>
				{
					if (result != null && result.ResultType == TaskResultType.Success)
					{
						_readyTiles.Add(tileId);
						OnVectorMeshCreated(result.GeneratedObjects);
						_meshGenerationUnit.UpdateForView(tileId, _mapInformation);
					}
					else if (result.ResultType == TaskResultType.DataProcessingFailure)
					{
						_vectorSource.InvalidateData(vectorData.TileId);
						Debug.Log(result.ExceptionsAsString);
					}
					else if (result.ResultType == TaskResultType.Cancelled)
					{
						if (result.GeneratedObjects != null)
						{
							foreach (var gameObject in result.GeneratedObjects)
							{
								GameObject.Destroy(gameObject);
							}
						}
					}
					callback?.Invoke(result);;
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

		}

		public Action<IEnumerable<GameObject>> OnVectorMeshCreated = list => { };
		public Action<GameObject> OnVectorMeshDestroyed = go => { };
		public Action<GameObject> OnVectorMeshTurnVisible = go => { };
		public Action<GameObject> OnVectorMeshTurnInvisible = go => { };
	}
}
