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
	protected Queue<FetchInfo> _fetchingQueue;

	public IEnumerator UpdateTick()
	{
		while (true)
		{
			if (_fetchingQueue.Count > 0)
			{
				var fi = _fetchingQueue.Dequeue();
				fi.RasterTile.Initialize(_fileSource, fi.TileId, fi.TilesetId, fi.Callback);
			}

			yield return null;
		}
	}

	public virtual void OnEnable()
	{
		_fileSource = MapboxAccess.Instance;
		_fetchingQueue = new Queue<FetchInfo>();
		Runnable.Run(UpdateTick());
	}

	public abstract void FetchData(DataFetcherParameters parameters);
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
}