using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System;
using Mapbox.Platform;
using Mapbox.Platform.Cache;
using Mapbox.Unity;

public class VectorDataFetcher : DataFetcher
{
	public Action<UnityTile, VectorTile> DataReceived = (t, s) => { };
	public Action<UnityTile, VectorTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	public VectorDataFetcher(IFileSource fileSource) : base(fileSource)
	{

	}

	public virtual void FetchData(VectorTile tile, string tilesetId, CanonicalTileId tileId, UnityTile unityTile = null)
	{
		//MemoryCacheCheck
		//we do not check for tile expiration of memory cached items
		//we only do expiration check for item from file/sql
// 		var textureItem = MapboxAccess.Instance.CacheManager.GetVectorItemFromMemory(tilesetId, tileId);
// 		if (textureItem != null)
// 		{
// 			tile.SetVectorFromCache(textureItem.Tile as VectorTile);
//
// #if UNITY_EDITOR
// 			tile.FromCache = CacheType.MemoryCache;
// #endif
//
// 			//this is mostly to update the caching time
// 			MapboxAccess.Instance.CacheManager.AddVectorItemToMemory(
// 				textureItem.TilesetId,
// 				textureItem.TileId,
// 				textureItem,
// 				true);
// 			DataReceived(unityTile, tile);
// 			return;
// 		}

		//FileCacheCheck
// 		if (MapboxAccess.Instance.CacheManager.TextureFileExists(tilesetId, tileId)) //not in memory, check file cache
// 		{
// 			MapboxAccess.Instance.CacheManager.GetTextureItemFromFile(tilesetId, tileId, (textureCacheItem) =>
// 			{
// 				if (unityTile != null && !unityTile.ContainsDataTile(tile))
// 				{
// 					//rasterTile.Clear();
// 					//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
// 					return;
// 				}
//
// 				//even though we just checked file exists, system couldn't find&load it
// 				//this shouldn't happen frequently, only in some corner cases
// 				//one possibility might be file being pruned due to hitting cache limit
// 				//after that first check few lines above and actual loading (loading is scheduled and delayed so it's not in same frame)
// 				if (textureCacheItem != null)
// 				{
// 					textureCacheItem.Tile = tile;
// 					//TODO FIX THIS
// 					//tile.SetTextureFromCache(textureCacheItem.Texture2D);
// #if UNITY_EDITOR
// 					tile.FromCache = CacheType.FileCache;
// 					textureCacheItem.From = tile.FromCache;
// #endif
//
// 					tile.ETag = textureCacheItem.ETag;
// 					if (textureCacheItem.ExpirationDate.HasValue)
// 					{
// 						tile.ExpirationDate = textureCacheItem.ExpirationDate.Value;
// 					}
//
// 					DataReceived(unityTile, tile);
//
// 					//IMPORTANT file is read from file cache and it's not automatically
// 					//moved to memory cache. we have to do it here.
// 					MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(
// 						textureCacheItem.TilesetId,
// 						textureCacheItem.TileId,
// 						textureCacheItem,
// 						true);
//
// 					//after returning what we already have
// 					//check if it's out of date, if so check server for update
// 					if (textureCacheItem.ExpirationDate < DateTime.Now)
// 					{
// 						EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, textureCacheItem.ETag)
// 						{
// 							Callback = () => { FetchingCallback(tileId, tile, unityTile); }
// 						});
// 					}
// 				}
// 				else
// 				{
// 					//this else part technically should rarely ever happen.
// 					//it means file exists check returned true but while the command is in queue
// 					//file is probably deleted so cannot find it anymore.
// 					EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
// 					{
// 						Callback = () => { FetchingCallback(tileId, tile, unityTile); }
// 					});
// 				}
// 			});
//
// 			return;
// 		}

		//not in cache so web request
		EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
		{
			Callback = () => { FetchingCallback(tileId, tile, unityTile); }
		});
	}

	protected virtual void FetchingCallback(CanonicalTileId tileId, VectorTile vectorTile, UnityTile unityTile = null)
	{
		if (unityTile != null && !unityTile.ContainsDataTile(vectorTile))
		{
			//rasterTile.Clear();
			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
			return;
		}

		if (vectorTile.HasError)
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
				//IMPORTANT This is where we create a Texture2D
				//rasterTile.ExtractTextureFromRequest();

				var cacheItem = new VectorCacheItem()
				{
					Tile = vectorTile,
					TileId = tileId,
					TilesetId = vectorTile.TilesetId,
					ETag = vectorTile.ETag,
					Data = vectorTile.ByteData,
					ExpirationDate = vectorTile.ExpirationDate,
					VectorTile = vectorTile.Data
				};

#if UNITY_EDITOR
				cacheItem.From = vectorTile.FromCache;
#endif

				//IMPORTANT And this is where we pass it to cache
				//cache will be responsible for tracking it all the way
				//and destroying it when it's not used anymore
				MapboxAccess.Instance.CacheManager.AddVectorDataItem(
					vectorTile.TilesetId,
					vectorTile.Id,
					cacheItem,
					true);

				DataReceived(unityTile, vectorTile);
			}
		}
	}

	// //
	// public void FetchData(DataFetcherParameters parameters)
	// {
	// 	var imageDataParameters = parameters as VectorDataFetcherParameters;
	// 	if(imageDataParameters == null)
	// 	{
	// 		return;
	// 	}
	//
	// 	FetchData(imageDataParameters.tilesetId, imageDataParameters.canonicalTileId, imageDataParameters.useOptimizedStyle, imageDataParameters.style, imageDataParameters.tile);
	// }
	//
	// //tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	// public void FetchData(string tilesetId, CanonicalTileId tileId, bool useOptimizedStyle, Style optimizedStyle, UnityTile unityTile = null)
	// {
	// 	//MemoryCacheCheck
	// 	var dataItem = MapboxAccess.Instance.CacheManager.GetVectorItemFromMemory(tilesetId, tileId);
	// 	if (dataItem != null)
	// 	{
	// 		if ((dataItem.Tile as VectorTile).VectorResults != null)
	// 		{
	// 			DataReceived(unityTile, dataItem.Tile as VectorTile);
	// 			return;
	// 		}
	// 		// else if (dataItem.Data != null)
	// 		// {
	// 		// 	Debug.Log("Memory cached vector item has raw data but not decompressed data, this shouldn't ever happen.");
	// 		// 	var decompressed = Compression.Decompress(dataItem.Data);
	// 		// 	var vectorTile = new Mapbox.VectorTile.VectorTile(decompressed);
	// 		// 	DataRecieved(unityTile, vectorTile);
	// 		// 	return;
	// 		// }
	// 	}
	//
	// 	//not in cache so web request
	// 	CreateWebRequest(tilesetId, tileId, useOptimizedStyle, optimizedStyle,String.Empty, unityTile);
	// }
	//
	// private void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useOptimizedStyle, Style optimizedStyle, string etag, UnityTile unityTile = null)
	// {
	// 	var vectorTile = (useOptimizedStyle) ? new VectorTile(tileId, tilesetId, optimizedStyle.Id, optimizedStyle.Modified) : new VectorTile(tileId, tilesetId);
	//
	//
	// 	if (unityTile != null)
	// 	{
	// 		unityTile.AddTile(vectorTile);
	// 	}
	//
	// 	EnqueueForFetching(new FetchInfo(tileId, tilesetId, vectorTile, etag)
	// 	{
	// 		Callback = () => { FetchingCallback2(tileId, vectorTile, unityTile); }
	// 	});
	// }
	//
	// private void FetchingCallback2(CanonicalTileId tileId, VectorTile vectorTile, UnityTile unityTile = null)
	// {
	// 	if (unityTile != null && unityTile.CanonicalTileId != vectorTile.Id)
	// 	{
	// 		//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
	// 		return;
	// 	}
	//
	// 	if (vectorTile.HasError)
	// 	{
	// 		FetchingError(unityTile, vectorTile, new TileErrorEventArgs(tileId, vectorTile.GetType(), unityTile, vectorTile.Exceptions));
	// 	}
	// 	else
	// 	{
	// 		MapboxAccess.Instance.CacheManager.AddVectorDataItem(
	// 			vectorTile.TilesetId,
	// 			vectorTile.Id,
	// 			new VectorCacheItem()
	// 			{
	// 				Tile = vectorTile,
	// 				TileId = vectorTile.Id,
	// 				TilesetId = vectorTile.TilesetId,
	// 				ETag = vectorTile.ETag,
	// 				Data = vectorTile.ByteData,
	// 				VectorTile = vectorTile.Data,
	// 				ExpirationDate = vectorTile.ExpirationDate
	// 			},
	// 			true);
	//
	// 		if (vectorTile.StatusCode != 304) //NOT MODIFIED
	// 		{
	// 			DataReceived(unityTile, vectorTile);
	// 		}
	// 	}
	//
	// 	// if (unityTile != null)
	// 	// {
	// 	// 	unityTile.RemoveTile(vectorTile);
	// 	// }
	// }
}

public class VectorDataFetcherParameters : DataFetcherParameters
{
	public bool useOptimizedStyle = false;
	public Style style = null;
}
