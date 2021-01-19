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
	protected MapboxAccess _fileSource;

	protected static Queue<int> _tileOrder;
	protected static Dictionary<int, FetchInfo> _tileFetchInfos;
	protected static Dictionary<int, Tile> _activeRequests;
	protected static int _activeRequestLimit = 10;

	protected DataFetcher()
	{
		_fileSource = MapboxAccess.Instance;
		if (_tileOrder == null)
		{
			_tileOrder = new Queue<int>();
			_tileFetchInfos = new Dictionary<int, FetchInfo>();
			_activeRequests = new Dictionary<int, Tile>();
			Runnable.Run(UpdateTick(_fileSource));
		}
	}

	public static IEnumerator UpdateTick(IFileSource fileSource)
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
					_activeRequests.Add(tileKey, fi.RasterTile);
					fi.RasterTile.Initialize(
						fileSource,
						fi.TileId,
						fi.TilesetId,
						() =>
						{
							_activeRequests.Remove(tileKey);
							fi.Callback();
						});
					yield return null;
				}
			}

			yield return null;
		}
	}

	public abstract void FetchData(DataFetcherParameters parameters);

	protected void EnqueueForFetching(FetchInfo info)
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
}