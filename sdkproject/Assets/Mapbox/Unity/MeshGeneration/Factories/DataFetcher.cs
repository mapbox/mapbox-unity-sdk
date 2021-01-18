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
	//protected Queue<FetchInfo> _fetchingQueue;

	protected static Queue<int> _tileOrder;
	protected static Dictionary<int, FetchInfo> _tileFetchInfos;

	protected DataFetcher()
	{
		_fileSource = MapboxAccess.Instance;
		if (_tileOrder == null)
		{
			_tileOrder = new Queue<int>();
			_tileFetchInfos = new Dictionary<int, FetchInfo>();
			//_fetchingQueue = new Queue<FetchInfo>();
			Runnable.Run(UpdateTick(_fileSource));
		}
	}

	public static IEnumerator UpdateTick(IFileSource fileSource)
	{
		while (true)
		{
			while (_tileOrder.Count > 0)
			{
				var tileId = _tileOrder.Dequeue();
				if (_tileFetchInfos.ContainsKey(tileId))
				{
					var fi = _tileFetchInfos[tileId];
					_tileFetchInfos.Remove(tileId);
					fi.RasterTile.Initialize(fileSource, fi.TileId, fi.TilesetId, fi.Callback);
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
		var key = tileUnwrappedTileId.Canonical.GenerateKey(tilesetId);
		if (_tileFetchInfos.ContainsKey(key))
		{
			_tileFetchInfos.Remove(key);
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