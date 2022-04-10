using System;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.DataFetching
{
	public abstract class DataFetcher
	{
		public virtual void FetchData(DataFetcherParameters parameters)
		{
		}

		protected void EnqueueForFetching(FetchInfo info)
		{
			MapboxAccess.Instance.DataManager.EnqueueForFetching(info);
		}

		public virtual void CancelFetching(UnwrappedTileId tileUnwrappedTileId, string tilesetId)
		{
			//MapboxAccess.Instance.DataManager.CancelFetching(tileUnwrappedTileId, tilesetId);
		}

		public void CancelFetching(Tile tile, string tilesetId)
		{
			tile.AddLog(Time.frameCount + " CancelFetching");
			MapboxAccess.Instance.DataManager.CancelFetching(tile, tilesetId);
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
		public string TilesetId;
		public Action Callback;
		public Tile RasterTile;
		public string ETag;
		public float QueueTime;

		public FetchInfo(CanonicalTileId tileId, string tilesetId, Tile tile, string eTag = "", Action callback = null)
		{
			TilesetId = tilesetId;
			RasterTile = tile;
			ETag = eTag;
			Callback = callback;
		}

	}
}