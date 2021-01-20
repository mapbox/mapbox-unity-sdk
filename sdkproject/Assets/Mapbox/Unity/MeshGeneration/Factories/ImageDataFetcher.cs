using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Platform;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageDataFetcher : DataFetcher
{
	public Action<UnityTile, RasterTile> TextureReceived = (t, s) => { };
	public Action<UnityTile, RasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	public ImageDataFetcher(IFileSource fileSource) : base(fileSource)
	{

	}

	public virtual void FetchData(RasterTile tile, string tilesetId, CanonicalTileId tileId, bool useRetina, UnityTile unityTile = null)
	{
		//MemoryCacheCheck
		var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tileId);
		if (textureItem != null)
		{
			tile.SetTextureFromCache(textureItem.Texture2D);
#if UNITY_EDITOR
			tile.FromCache = CacheType.MemoryCache;
#endif
			TextureReceived(unityTile, tile);
			return;
		}

		//FileCacheCheck
		if (MapboxAccess.Instance.CacheManager.TextureFileExists(tilesetId, tileId)) //not in memory, check file cache
		{
			MapboxAccess.Instance.CacheManager.GetTextureItemFromFile(tilesetId, tileId, (textureCacheItem) =>
			{
				if (unityTile != null && !unityTile.ContainsDataTile(tile))
				{
					//rasterTile.Clear();
					//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
					return;
				}

				//even though we just checked file exists, system couldn't find&load it
				//this shouldn't happen frequently, only in some corner cases
				//one possibility might be file being pruned due to hitting cache limit
				//after that first check few lines above and actual loading (loading is scheduled and delayed so it's not in same frame)
				if (textureCacheItem != null)
				{
					tile.SetTextureFromCache(textureCacheItem.Texture2D);
#if UNITY_EDITOR
					tile.FromCache = CacheType.FileCache;
#endif
					//do we need these for live products or are they only for debugging?
					tile.ETag = textureCacheItem.ETag;
					if (textureCacheItem.ExpirationDate.HasValue)
					{
						tile.ExpirationDate = textureCacheItem.ExpirationDate.Value;
					}

					TextureReceived(unityTile, tile);

					//after returning what we already have
					//check if it's out of date, if so check server for update
					if (textureCacheItem.ExpirationDate < DateTime.Now)
					{
						EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, textureCacheItem.ETag)
						{
							Callback = () => { FetchingCallback(tileId, tile, unityTile); }
						});
					}
				}
				else
				{
					EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
					{
						Callback = () => { FetchingCallback(tileId, tile, unityTile); }
					});
				}
			});

			return;
		}

		//not in cache so web request
		//CreateWebRequest(tilesetId, tileId, useRetina, String.Empty, unityTile);
		EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
		{
			Callback = () => { FetchingCallback(tileId, tile, unityTile); }
		});
	}

	protected virtual void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
	{
		if (unityTile != null && !unityTile.ContainsDataTile(rasterTile))
		{
			//rasterTile.Clear();
			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
			return;
		}

		if (rasterTile.HasError)
		{
			FetchingError(unityTile, rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), unityTile, rasterTile.Exceptions));
		}
		else
		{

			rasterTile.ExtractTextureFromRequest();

#if UNITY_EDITOR
			if (rasterTile.Texture2D != null)
			{
				rasterTile.Texture2D.name = string.Format("{0}_{1}", tileId.ToString(), rasterTile.TilesetId);
			}
#endif
			MapboxAccess.Instance.CacheManager.AddTextureItem(
				rasterTile.TilesetId,
				rasterTile.Id,
				new TextureCacheItem()
				{
					TileId = tileId,
					TilesetId = rasterTile.TilesetId,
					From = rasterTile.FromCache,
					ETag = rasterTile.ETag,
					Data = rasterTile.Data,
					ExpirationDate = rasterTile.ExpirationDate,
					Texture2D = rasterTile.Texture2D
				},
				true);

			if (rasterTile.StatusCode != 304) //NOT MODIFIED
			{
				TextureReceived(unityTile, rasterTile);
			}
		}
	}
}

public class ImageDataFetcherParameters : DataFetcherParameters
{
	public bool useRetina = true;
}
