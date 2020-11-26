using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.Utilities;
using UnityEngine;

public abstract class DataFetcher : ScriptableObject
{
	protected MapboxAccess _fileSource;
	//protected Queue<FetchInfo> _fetchingQueue;

	private Queue<CanonicalTileId> _tileOrder;
	private Dictionary<CanonicalTileId, FetchInfo> _tileFetchInfos;

	public IEnumerator UpdateTick()
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
					fi.RasterTile.Initialize(_fileSource, fi.TileId, fi.TilesetId, fi.Callback);
					yield return null;
				}
			}

			yield return null;
		}
	}

	public virtual void OnEnable()
	{
		_fileSource = MapboxAccess.Instance;
		_tileOrder = new Queue<CanonicalTileId>();
		_tileFetchInfos = new Dictionary<CanonicalTileId, FetchInfo>();
		//_fetchingQueue = new Queue<FetchInfo>();
		Runnable.Run(UpdateTick());
	}

	public abstract void FetchData(DataFetcherParameters parameters);

	protected void EnqueueForFetching(FetchInfo info)
	{
		//_fetchingQueue.Enqueue(info);
		if (!_tileFetchInfos.ContainsKey(info.TileId))
		{
			_tileOrder.Enqueue(info.TileId);
			_tileFetchInfos.Add(info.TileId, info);
		}
	}

	public virtual void CancelFetching(UnwrappedTileId tileUnwrappedTileId)
	{
		var canonical = tileUnwrappedTileId.Canonical;
		if (_tileFetchInfos.ContainsKey(canonical))
		{
			_tileFetchInfos.Remove(canonical);
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