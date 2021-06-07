using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity.Utilities;
using UnityEngine;

namespace Mapbox.Unity.DataFetching
{
	public class DataFetchingManager
	{
		protected IFileSource _fileSource;
		protected Queue<int> _tileOrder;
		protected Dictionary<int, FetchInfo> _tileFetchInfos;
		protected Dictionary<int, Tile> _activeRequests;
		protected int _activeRequestLimit = 10;

		public DataFetchingManager(IFileSource fileSource)
		{
			_fileSource = fileSource;
			_tileOrder = new Queue<int>();
			_tileFetchInfos = new Dictionary<int, FetchInfo>();
			_activeRequests = new Dictionary<int, Tile>();
			Runnable.Run(UpdateTick());
		}

		public virtual void EnqueueForFetching(FetchInfo info, int priority = 0)
		{
			var key = info.TileId.GenerateKey(info.TilesetId);
			if (!_tileFetchInfos.ContainsKey(key))
			{
				_tileOrder.Enqueue(key);
				_tileFetchInfos.Add(key, info);
			}
		}

		public virtual void CancelFetching(UnwrappedTileId tileUnwrappedTileId, string tilesetId)
		{
			var canonical = tileUnwrappedTileId.Canonical;
			var key = canonical.GenerateKey(tilesetId);
			if (_tileFetchInfos.ContainsKey(key))
			{
				_tileFetchInfos.Remove(key);
			}

			if (_activeRequests.ContainsKey(key))
			{
				_activeRequests[key].Cancel();
				_activeRequests.Remove(key);
			}
		}

		private IEnumerator UpdateTick()
		{
			while (true)
			{
				while (_tileOrder.Count > 0 && _activeRequests.Count < _activeRequestLimit)
				{
					var tileKey = _tileOrder.Dequeue();
					if (_tileFetchInfos.ContainsKey(tileKey))
					{
						var fi = _tileFetchInfos[tileKey];
						_tileFetchInfos.Remove(tileKey);
						if (_activeRequests.ContainsKey(tileKey))
						{
							//skip this info, a clone of it is already running
						}
						else
						{
							_activeRequests.Add(tileKey, fi.RasterTile);
							fi.RasterTile.Initialize(
								_fileSource,
								fi.TileId,
								fi.TilesetId,
								() =>
								{
									_activeRequests.Remove(tileKey);
									fi.Callback();
								});
						}

						yield return null;
					}
				}

				yield return null;
			}
		}
	}

	public class EditorDataFetchingManager : DataFetchingManager
	{
		public EditorDataFetchingManager(IFileSource fileSource) : base(fileSource)
		{
		}

		public Queue<int> GetTileOrderQueue()
		{
			return _tileOrder;
		}

		public Dictionary<int, FetchInfo> GetFetchInfoQueue()
		{
			return _tileFetchInfos;
		}

		public int GetActiveRequestLimit()
		{
			return _activeRequestLimit;
		}

		public Dictionary<int, Tile> GetActiveRequests()
		{
			return _activeRequests;
		}
	}
}