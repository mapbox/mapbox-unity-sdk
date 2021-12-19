using System;
using Mapbox.Map;
using Mapbox.Platform.Cache;
using Mapbox.Unity.MeshGeneration.Data;

namespace Mapbox.Unity.DataFetching
{
	public class BaseImageDataFetcher : ImageDataFetcher
	{
		public void FetchData(RasterTile tile, string tilesetId, CanonicalTileId tileId, bool useRetina, UnityTile unityTile = null)
		{
			//MemoryCacheCheck
			var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tileId);
			if (textureItem != null)
			{
				var rasterTile = new RasterTile(tileId, tilesetId, tile.IsTextureNonreadable);
				rasterTile.SetTextureFromCache(textureItem.Texture2D);
#if UNITY_EDITOR
				rasterTile.FromCache = CacheType.MemoryCache;
#endif
				MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(
					textureItem.TilesetId,
					textureItem.TileId,
					textureItem,
					true);
				TextureReceived(unityTile, rasterTile);
				return;
			}

			void TextureReadCallback(TextureCacheItem textureCacheItem)
			{
				//even though we just checked file exists, system couldn't find&load it
				//this shouldn't happen frequently, only in some corner cases
				//one possibility might be file being pruned due to hitting cache limit
				//after that first check few lines above and actual loading (loading is scheduled and delayed so it's not in same frame)
				if (textureCacheItem != null)
				{
					textureCacheItem.Tile = tile;
					var rasterTile = new RasterTile(tileId, tilesetId, tile.IsTextureNonreadable);
					rasterTile.SetTextureFromCache(textureCacheItem.Texture2D);
#if UNITY_EDITOR
					rasterTile.FromCache = CacheType.FileCache;
					textureCacheItem.From = rasterTile.FromCache;
#endif
					tile.ETag = textureCacheItem.ETag;
					if (textureCacheItem.ExpirationDate.HasValue)
					{
						tile.ExpirationDate = textureCacheItem.ExpirationDate.Value;
					}
					TextureReceived(unityTile, rasterTile);
					MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(tilesetId, tileId, textureCacheItem, true);
					MapboxAccess.Instance.CacheManager.MarkFallback(tileId, tilesetId);
				}
				else
				{
					EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, string.Empty) {Callback = () => { FetchingCallback(tileId, tile, unityTile); }});
				}
			}

			void TextureInfoUpdatedCallback(TextureCacheItem textureCacheItem)
			{
				if (unityTile != null && !unityTile.ContainsDataTile(tile))
				{
					//rasterTile.Clear();
					//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
					return;
				}

				if (textureCacheItem != null)
				{
					//after returning what we already have
					//check if it's out of date, if so check server for update
					if (textureCacheItem.ExpirationDate < DateTime.Now)
					{
						EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, textureCacheItem.ETag) {Callback = () => { FetchingCallback(tileId, tile, unityTile); }});
					}
				}
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

			void FileNotFoundCallback()
			{
				EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
				{
					Callback = () => { FetchingCallback(tileId, tile, unityTile); }
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

			//not in cache so web request

		}

		protected override void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
		{
			base.FetchingCallback(tileId, rasterTile, unityTile);
#if UNITY_EDITOR
			if (rasterTile.Texture2D != null)
			{
				rasterTile.Texture2D.name += "_fallbackImage";
			}
#endif
			
			MapboxAccess.Instance.CacheManager.MarkFallback(rasterTile.Id, rasterTile.TilesetId);
		}
	}
}