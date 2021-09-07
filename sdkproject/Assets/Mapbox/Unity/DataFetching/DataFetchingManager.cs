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
		private const float _requestDelay = 0.2f;

		protected IFileSource _fileSource;
		protected Queue<int> _tileOrder;
		protected Dictionary<int, FetchInfo> _tileFetchInfos;
		private Dictionary<int, Tile> _localQueuedRequests = new Dictionary<int, Tile>();
		protected Dictionary<int, Tile> _globalActiveRequests;
		protected int _activeRequestLimit = 10;

		public DataFetchingManager(IFileSource fileSource)
		{
			_fileSource = fileSource;
			_tileOrder = new Queue<int>();
			_tileFetchInfos = new Dictionary<int, FetchInfo>();
			_globalActiveRequests = new Dictionary<int, Tile>();
			Runnable.Run(UpdateTick());
		}

		public virtual void EnqueueForFetching(FetchInfo info)
		{
			var key = info.TileId.GenerateKey(info.TilesetId);

			if (!_localQueuedRequests.ContainsKey(key))
			{
				info.Callback += () =>
				{
					_globalActiveRequests.Remove(key);
					_localQueuedRequests.Remove(key);
				};

				_tileOrder.Enqueue(key);
				_localQueuedRequests.Add(key, info.RasterTile);
				info.QueueTime = Time.time;
				_tileFetchInfos.Add(key, info);
			}
			else
			{
				//same requests is already in queue.
				//this probably means first one was supposed to be cancelled but for some reason has not.
				//ensure all data fetchers (including unorthodox ones like file data fetcher) handling
				//tile cancelling properly
				Debug.Log("tile request is already in queue. This most likely means first request was supposed to be cancelled but not.");
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

			if (_globalActiveRequests.ContainsKey(key))
			{
				_globalActiveRequests[key].Cancel();
				_globalActiveRequests.Remove(key);
			}
			_localQueuedRequests.Remove(key);
		}

		private IEnumerator UpdateTick()
		{
			while (true)
			{
				var fallbackCounter = 0;
				while (_tileOrder.Count > 0 && _globalActiveRequests.Count < _activeRequestLimit && fallbackCounter < _activeRequestLimit)
				{
					fallbackCounter++;
					var tileKey = _tileOrder.Peek(); //we just peek first as we might want to hold it until delay timer runs out
					if (!_tileFetchInfos.ContainsKey(tileKey))
					{
						_tileOrder.Dequeue(); //but we dequeue it if it's not in tileFetchInfos, which means it's cancelled
						continue;
					}

					if (QueueTimeHasMatured(_tileFetchInfos[tileKey].QueueTime, _requestDelay))
					{
						tileKey = _tileOrder.Dequeue();
						var fi = _tileFetchInfos[tileKey];
						_tileFetchInfos.Remove(tileKey);
						_globalActiveRequests.Add(tileKey, fi.RasterTile);
						//fi.RasterTile.Logs.Add(string.Format("{0} Before Initialize", Time.frameCount));
						fi.RasterTile.Initialize(
							_fileSource,
							fi.TileId,
							fi.TilesetId,
							fi.Callback);
						yield return null;
					}
				}

				yield return null;
			}
		}

		private static bool QueueTimeHasMatured(float queueTime, float maturationAge)
		{
			return Time.time - queueTime >= maturationAge;
		}
	}

	public class EditorDataFetchingManager : DataFetchingManager
	{
		public List<string> Logs = new List<string>();
		public EditorDataFetchingManager(IFileSource fileSource) : base(fileSource)
		{
		}

		public override void EnqueueForFetching(FetchInfo info)
		{
			//info.RasterTile.Logs.Add(string.Format("{0} Data Fetcher enqueued", Time.frameCount));
			base.EnqueueForFetching(info);
		}

		public override void CancelFetching(UnwrappedTileId tileUnwrappedTileId, string tilesetId)
		{
			// var key = tileUnwrappedTileId.Canonical.GenerateKey(tilesetId);
			// if (_tileFetchInfos.ContainsKey(key))
			// {
			// 	var info = _tileFetchInfos[key];
			// 	info.RasterTile.Logs.Add(string.Format("{0} Data Fetcher cancelled", Time.frameCount));
			// }
			base.CancelFetching(tileUnwrappedTileId, tilesetId);
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
			return _globalActiveRequests;
		}
	}
}