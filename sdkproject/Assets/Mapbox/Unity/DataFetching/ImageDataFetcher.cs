using System;
using System.Linq;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using UnityEngine;

namespace Mapbox.Unity.DataFetching
{
	public class ImageDataFetcher : DataFetcher
	{
		public Action<RasterTile> TextureReceived = (s) => { };
		public Action<RasterTile, TileErrorEventArgs> FetchingError = (r, s) => { };

		public virtual TextureCacheItem FetchDataInstant(CanonicalTileId tileId, string tileset)
		{
			var response = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tileset, tileId);
			if (response != null)
			{
				MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(
					response.TilesetId,
					response.Tile.Id,
					response,
					true);
			}

			return response;
		}

		public virtual void FetchData(RasterTile tile, string tilesetId, CanonicalTileId tileId)
		{
			//MemoryCacheCheck
			//we do not check for tile expiration of memory cached items
			//we only do expiration check for item from file/sql
// 			var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tile.Id);
// 			if (textureItem != null)
// 			{
// 				tile.Logs.Add("found in memory");
// 				tile.SetTextureFromCache(textureItem.Texture2D);
// #if UNITY_EDITOR
// 				tile.FromCache = CacheType.MemoryCache;
// #endif
//
// 				if (tile.Texture2D == null)
// 				{
// 					Debug.Log("null texture from memory cache?");
// 				}
//
// 				//this is mostly to update the caching time
// 				MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(
// 					textureItem.TilesetId,
// 					textureItem.Tile.Id,
// 					textureItem,
// 					true);
// 				TextureReceived(unityTile, tile);
// 				return;
// 			}

			void TextureReadCallback(TextureCacheItem textureCacheItem)
			{
				tile.AddLog("callback TextureReadCallback");
				if (!tile.IsInUse())
				{
					tile.AddLog("TextureReadCallback dropped due to id mismatch");
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
					textureCacheItem.Tile = tile;
					tile.SetTextureFromCache(textureCacheItem.Texture2D);
#if UNITY_EDITOR
					tile.FromCache = CacheType.FileCache;
					textureCacheItem.From = tile.FromCache;
#endif

					tile.ETag = textureCacheItem.ETag;
					if (textureCacheItem.ExpirationDate.HasValue)
					{
						tile.ExpirationDate = textureCacheItem.ExpirationDate.Value;
					}

					if (tile.Texture2D == null)
					{
						Debug.Log("here");
					}

					TextureReceived(tile);

					//IMPORTANT file is read from file cache and it's not automatically
					//moved to memory cache. we have to do it here.
					MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(textureCacheItem.TilesetId, textureCacheItem.Tile.Id, textureCacheItem, true);
				}
				else
				{
					//this else part technically should rarely ever happen.
					//it means file exists check returned true but while the command is in queue
					//file is probably deleted so cannot find it anymore.
					EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty) {Callback = () =>
					{
						FetchingCallback(tileId, tile);
					}});
				}
			}

			void TextureInfoUpdatedCallback(TextureCacheItem textureCacheItem)
			{
				tile.AddLog("callback TextureInfoUpdatedCallback");
				if (!tile.IsInUse())
				{
					tile.AddLog("TextureInfoUpdatedCallback dropped due to id mismatch");
					//rasterTile.Clear();
					//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
					return;
				}

				if (textureCacheItem != null)
				{
					tile.ETag = textureCacheItem.ETag;
					if (textureCacheItem.ExpirationDate.HasValue)
					{
						tile.ExpirationDate = textureCacheItem.ExpirationDate.Value;
					}

					//after returning what we already have
					//check if it's out of date, if so check server for update
					if (textureCacheItem.ExpirationDate < DateTime.Now)
					{
						EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, textureCacheItem.ETag)
						{
							Callback = () =>
							{
								FetchingCallback(tileId, tile);
							}
						});
					}
				}
			}

			void FailureCallback()
			{
				tile.AddLog("callback FailureCallback");
				if (!tile.IsInUse())
				{
					tile.AddLog("FailureCallback dropped due to id mismatch");
					//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
					return;
				}

				EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, string.Empty) {Callback = () =>
				{
					FetchingCallback(tileId, tile);
				}});
			}

			void CancelledCallback()
			{
				tile.AddLog("callback CancelledCallback");
				FetchingError(tile, new TileErrorEventArgs(tileId, tile.GetType(), tile.Exceptions));
			}

			void FileNotFoundCallback()
			{
				EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
				{
					Callback = () => { FetchingCallback(tileId, tile); }
				});
			}

			//FileCacheCheck
			MapboxAccess.Instance.CacheManager.GetTextureItemFromFile(
				tilesetId,
				tile.Id,
				tileId,
				tile.IsTextureNonreadable,
				TextureReadCallback,
				TextureInfoUpdatedCallback,
				FailureCallback,
				CancelledCallback,
				FileNotFoundCallback);

		}

		protected virtual void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
		{
			if (unityTile != null && !unityTile.ContainsDataTile(rasterTile))
			{
				//rasterTile.Clear();
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				FetchingError( rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), rasterTile.Exceptions));
			}

			if (rasterTile.CurrentTileState == TileState.Canceled)
			{
				FetchingError(rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), rasterTile.Exceptions));
			}
			else
			if (rasterTile.HasError)
			{
				FetchingError(rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), rasterTile.Exceptions));
			}
			else
			{
				//304 means data was in file cache and sql
				//we fetched it from file/sql and had to update due to expiration date
				//so the file and the metadata is already there and server verified they
				//are all still good.
				//We just need to update the expiration date now and for current session
				//add it to memory cache
				if (rasterTile.StatusCode == 304)
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
						rasterTile.TilesetId,
						rasterTile.Id,
						rasterTile.ExpirationDate);
				}
				else
				{
					//IMPORTANT This is where we create a Texture2D
					rasterTile.AddLog("extracting texture ", rasterTile.Id);
					rasterTile.ExtractTextureFromRequest();

#if UNITY_EDITOR
					if (rasterTile.Texture2D != null)
					{
						rasterTile.Texture2D.name = string.Format("{0}_{1}", rasterTile.Id.ToString(), rasterTile.TilesetId);
					}
#endif

					var newTextureCacheItem = new TextureCacheItem()
					{
						Tile = rasterTile,
						TileId = rasterTile.Id,
						TilesetId = rasterTile.TilesetId,
						ETag = rasterTile.ETag,
						Data = rasterTile.Data,
						ExpirationDate = rasterTile.ExpirationDate,
						Texture2D = rasterTile.Texture2D
					};

#if UNITY_EDITOR
					newTextureCacheItem.From = rasterTile.FromCache;
#endif

					//IMPORTANT And this is where we pass it to cache
					//cache will be responsible for tracking it all the way
					//and destroying it when it's not used anymore
					MapboxAccess.Instance.CacheManager.AddTextureItem(
						rasterTile.TilesetId,
						rasterTile.Id,
						newTextureCacheItem,
						true);

					TextureReceived(rasterTile);
				}
			}
		}
	}

	public class ImageDataFetcherParameters : DataFetcherParameters
	{
		public bool useRetina = true;
	}
}