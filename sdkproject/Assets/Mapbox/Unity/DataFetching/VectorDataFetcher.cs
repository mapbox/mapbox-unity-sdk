using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System;
using System.Collections.Generic;
using Mapbox.Platform;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.DataContainers;
using Mapbox.Unity.DataFetching;
using UnityEngine;

public class VectorDataFetcher : DataFetcher
{
	public Action<UnityTile, VectorTile> DataReceived = (t, s) => { };
	public Action<UnityTile, VectorTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	public virtual void FetchData(VectorTile tile, string tilesetId, CanonicalTileId tileId, UnityTile unityTile = null)
	{
		//tile.Logs.Add(string.Format("{0} checking memory", Time.frameCount));
		// MemoryCacheCheck
		// we do not check for tile expiration of memory cached items
		// we only do expiration check for item from file/sql
 		var vectorCacheItemFromMemory = MapboxAccess.Instance.CacheManager.GetVectorItemFromMemory(tilesetId, tileId);
 		if (vectorCacheItemFromMemory != null &&
            vectorCacheItemFromMemory.Tile.CurrentTileState == TileState.Loaded)
 		{
 			tile.SetVectorFromCache(vectorCacheItemFromMemory.Tile as VectorTile);

 #if UNITY_EDITOR
 			tile.FromCache = CacheType.MemoryCache;
 #endif

 			//this is mostly to update the caching time
 			MapboxAccess.Instance.CacheManager.AddVectorItemToMemory(
 				vectorCacheItemFromMemory.TilesetId,
 				vectorCacheItemFromMemory.TileId,
 				vectorCacheItemFromMemory,
 				true);
            FireDataReceived(unityTile, tile);
 			return;
 		}

        void FailureCallback()
        {
	        if (unityTile != null && !unityTile.ContainsDataTile(tile))
	        {
		        //this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
		        return;
	        }

	        EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, string.Empty) {Callback = () => { FetchingCallback(tileId, tile, unityTile); }});
        }

        void CancelledCallback()
        {
	        FetchingError(unityTile, tile, new TileErrorEventArgs(tileId, tile.GetType(), unityTile, tile.Exceptions));
        }

        void SuccessCallback(CacheItem vectorCacheItemFromSqlite)
        {
	        if (tile.CurrentTileState == TileState.Canceled) return;

	        if (unityTile != null && !unityTile.ContainsDataTile(tile))
	        {
		        //this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
		        return;
	        }

	        if (vectorCacheItemFromSqlite.ExpirationDate.HasValue)
	        {
		        vectorCacheItemFromSqlite.Tile.ExpirationDate = vectorCacheItemFromSqlite.ExpirationDate.Value;
	        }

	        FireDataReceived(unityTile, tile);
	        MapboxAccess.Instance.CacheManager.AddVectorItemToMemory(tile.TilesetId, tile.Id, vectorCacheItemFromSqlite, true);

	        if (vectorCacheItemFromSqlite.ExpirationDate < DateTime.Now)
	        {
		        EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, vectorCacheItemFromSqlite.ETag) {Callback = () => { FetchingCallback(tileId, tile, unityTile); }});
	        }
        }

        MapboxAccess.Instance.CacheManager.GetVectorItemFromSqlite(
	        tile,
	        tilesetId,
	        tileId,
	        SuccessCallback,
	        FailureCallback,
	        CancelledCallback);
	}

	protected virtual void FetchingCallback(CanonicalTileId tileId, VectorTile vectorTile, UnityTile unityTile = null)
	{
		if (unityTile != null && !unityTile.ContainsDataTile(vectorTile))
		{
			//rasterTile.Clear();
			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
			FetchingError(unityTile, vectorTile, new TileErrorEventArgs(tileId, vectorTile.GetType(), unityTile, vectorTile.Exceptions));
		}

		if (vectorTile.CurrentTileState == TileState.Canceled)
		{
			FetchingError(unityTile, vectorTile, new TileErrorEventArgs(tileId, vectorTile.GetType(), unityTile, vectorTile.Exceptions));
		}
		else if (vectorTile.HasError)
		{
			FetchingError(unityTile, vectorTile, new TileErrorEventArgs(tileId, vectorTile.GetType(), unityTile, vectorTile.Exceptions));
		}
		else
		{
			//304 means data was in file cache and sql
			//we fetched it from file/sql and had to update due to expiration date
			//so the file and the metadata is already there and server verified they
			//are all still good.
			//We just need to update the expiration date now and for current session
			//add it to memory cache
			if (vectorTile.StatusCode == 304)
			{
				//304 means expired data from file/sql
				//it has already been processed and added to memory
				//304 means server says everything is same (except expiration date of course)
				//no need to add to memory cache again
				//expiration date will be updated in next call
				// MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(
				// 	rasterTile.TilesetId,
				// 	rasterTile.Id,
				// 	newTextureCacheItem,
				// 	true);

				MapboxAccess.Instance.CacheManager.UpdateExpirationDate(
					vectorTile.TilesetId,
					vectorTile.Id,
					vectorTile.ExpirationDate);
			}
			else
			{
				var cacheItem = new CacheItem()
				{
					Tile = vectorTile,
					TileId = tileId,
					TilesetId = vectorTile.TilesetId,
					ETag = vectorTile.ETag,
					Data = vectorTile.ByteData,
					ExpirationDate = vectorTile.ExpirationDate
				};

#if UNITY_EDITOR
				cacheItem.From = vectorTile.FromCache;
#endif

				//IMPORTANT And this is where we pass it to cache
				//cache will be responsible for tracking it all the way
				//and destroying it when it's not used anymore
				FireDataReceived(unityTile, vectorTile);
				MapboxAccess.Instance.CacheManager.AddVectorDataItem(
					vectorTile.TilesetId,
					vectorTile.Id,
					cacheItem,
					true);

			}
		}
	}

	public void FireDataReceived(UnityTile unityTile, VectorTile tile)
	{
		if (tile.Data == null && tile.CurrentTileState == TileState.Processing)
		{
			//new data fetched and processing
			tile.DataProcessingFinished += (b) =>
			{
				DataReceived(unityTile, tile);
			};
		}
		else if (tile.Data != null && tile.CurrentTileState == TileState.Processing)
		{
			//this is updating
			tile.DataProcessingFinished += (b) =>
			{
				DataReceived(unityTile, tile);
			};
		}
		else if (tile.CurrentTileState == TileState.Loaded)
		{
			DataReceived(unityTile, tile);
		}
		else
		{
			Debug.Log("error with vector tile");
		}
	}
}

public class VectorDataFetcherParameters : DataFetcherParameters
{
	public bool useOptimizedStyle = false;
	public Style style = null;
}
