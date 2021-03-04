using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using UnityEngine;

public abstract class DataFetcher
{
	private const float _requestDelay = 0.2f;
	public int QueuedRequestCount => _localQueuedRequests.Count;

	private Dictionary<int, Tile> _localQueuedRequests = new Dictionary<int, Tile>();

	protected IFileSource _fileSource;
	protected static Queue<int> _tileOrder;
	protected static Dictionary<int, FetchInfo> _tileFetchInfos;
	protected static Dictionary<int, Tile> _globalActiveRequests;
	protected static int _activeRequestLimit = 10;

	protected DataFetcher(IFileSource fileSource)
	{
		_fileSource = fileSource;
		if (_tileOrder == null)
		{
			_tileOrder = new Queue<int>();
			_tileFetchInfos = new Dictionary<int, FetchInfo>();
			_globalActiveRequests = new Dictionary<int, Tile>();
			Runnable.Run(UpdateTick(_fileSource));
		}
	}

	protected DataFetcher()
	{
		_fileSource = MapboxAccess.Instance;
		if (_tileOrder == null)
		{
			_tileOrder = new Queue<int>();
			_tileFetchInfos = new Dictionary<int, FetchInfo>();
			_globalActiveRequests = new Dictionary<int, Tile>();
			Runnable.Run(UpdateTick(_fileSource));
		}
	}

	public static IEnumerator UpdateTick(IFileSource fileSource)
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
					fi.RasterTile.Initialize(
						fileSource,
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

	protected void EnqueueForFetching(FetchInfo info)
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

}

public class DataFetcherParameters
{
	public CanonicalTileId canonicalTileId;
	public string tilesetId;
	public UnityTile tile;
}

public class FetchInfo
{
	public CanonicalTileId TileId;
	public string TilesetId;
	public Action Callback;
	public Tile RasterTile;
	public string ETag;
	public float QueueTime;

	public FetchInfo(CanonicalTileId tileId, string tilesetId, Tile tile, string eTag = "", Action callback = null)
	{
		TileId = tileId;
		TilesetId = tilesetId;
		RasterTile = tile;
		ETag = eTag;
		callback = callback;
	}

}