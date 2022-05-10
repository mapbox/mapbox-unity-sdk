using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.CustomLayer
{
	public class VectorFactoryManager
	{
		public Action<UnityTile, Mapbox.Map.VectorTile> DataReceived = (t, s) => { };
		public Action<UnityTile, Mapbox.Map.VectorTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

		protected VectorLayerProperties _properties;
		protected VectorDataFetcher _fetcher;
		protected Dictionary<UnityTile, Mapbox.Map.VectorTile> _tileTracker = new Dictionary<UnityTile, Mapbox.Map.VectorTile>();

		public VectorFactoryManager(VectorLayerProperties properties)
		{
			_properties = properties;
			_fetcher = new VectorDataFetcher();
			_fetcher.DataReceived += OnFetcherDataRecieved;
			_fetcher.FetchingError += OnFetcherError;
		}

		public void RegisterTile(UnityTile tile)
		{
			if (string.IsNullOrEmpty(_properties.sourceOptions.Id) || _properties.sourceOptions.isActive == false)
			{
				return;
			}

			if (_tileTracker.ContainsKey(tile))
			{
				return;
			}
			
			var dataTile = CreateDataTile(tile.CanonicalTileId, _properties.sourceOptions.Id);
			_tileTracker.Add(tile, dataTile);
			if (tile != null)
			{
				tile.AddTile(dataTile);
				dataTile.AddUser(tile.CanonicalTileId);
			}

			_fetcher.FetchData(dataTile, _properties.sourceOptions.Id, tile.CanonicalTileId, tile);
		}

		public void UnregisterTile(UnityTile tile)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				//Debug.Log(string.Format("{0} - {1}",tile.CanonicalTileId, "remove Vector"));
				var dataTile = _tileTracker[tile];
				dataTile.Cancel();
				_fetcher.CancelFetching(dataTile, _properties.sourceOptions.Id);
				tile.RemoveTile(dataTile);
				_tileTracker[tile].RemoveUser(tile.CanonicalTileId);
				_tileTracker.Remove(tile);
				MapboxAccess.Instance.CacheManager.TileDisposed(tile, _properties.sourceOptions.Id);
				MapboxAccess.Instance.TaskManager.CancelTile(tile.CanonicalTileId);
			}
		}

		public void StopTile(UnityTile tile)
		{
			if (_tileTracker.ContainsKey(tile))
			{
				//Debug.Log(string.Format("{0} - {1}",tile.CanonicalTileId, "remove Vector"));
				var dataTile = _tileTracker[tile];
				dataTile.Cancel();
				tile.DataTileStopped(dataTile);
				_fetcher.CancelFetching(dataTile, _properties.sourceOptions.Id);
				MapboxAccess.Instance.TaskManager.CancelTile(tile.CanonicalTileId);
				//_tileTracker[tile].RemoveUser(tile.CanonicalTileId);
				//_tileTracker.Remove(tile);
				//MapboxAccess.Instance.CacheManager.TileDisposed(tile, _properties.sourceOptions.Id);
			}
		}

		private void OnFetcherDataRecieved(UnityTile unityTile, Mapbox.Map.VectorTile vectorTile)
		{
			vectorTile.AddLog("manager OnFetcherDataRecieved");
			DataReceived(unityTile, vectorTile);
		}

		protected virtual Mapbox.Map.VectorTile CreateDataTile(CanonicalTileId canonicalTileId, string tilesetId)
		{
			var vectorTile = (_properties.useOptimizedStyle)
				? new Mapbox.Map.VectorTile(canonicalTileId, tilesetId, _properties.optimizedStyle.Id, _properties.optimizedStyle.Modified)
				: new Mapbox.Map.VectorTile(canonicalTileId, tilesetId);
#if UNITY_EDITOR
			vectorTile.IsMapboxTile = true;
#endif
			return vectorTile;
		}

		private void OnFetcherError(UnityTile unityTile, Mapbox.Map.VectorTile dataTile, TileErrorEventArgs errorEventArgs)
		{
			FetchingError(unityTile, dataTile, errorEventArgs);
		}
	}
}