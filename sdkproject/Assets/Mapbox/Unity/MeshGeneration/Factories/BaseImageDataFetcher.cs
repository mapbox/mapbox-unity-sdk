using System;
using Mapbox.Map;
using Mapbox.Platform;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;

public class BaseImageDataFetcher : ImageDataFetcher
{
	public BaseImageDataFetcher(IFileSource fileSource) : base(fileSource)
	{

	}

	public void FetchData(RasterTile tile, string tilesetId, CanonicalTileId tileId, bool useRetina, UnityTile unityTile = null)
	{
		//MemoryCacheCheck
		var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tileId);
		if (textureItem != null)
		{
			var rasterTile = new RasterTile(tileId, tilesetId);
			rasterTile.SetTextureFromCache(textureItem.Texture2D);
#if UNITY_EDITOR
			rasterTile.FromCache = CacheType.MemoryCache;
#endif
			TextureReceived(unityTile, rasterTile);
			return;
		}

		//FileCacheCheck
		if (MapboxAccess.Instance.CacheManager.TextureFileExists(tilesetId, tileId)) //not in memory, check file cache
		{
			MapboxAccess.Instance.CacheManager.GetTextureItemFromFile(tilesetId, tileId, (textureCacheItem) =>
			{
				//even though we just checked file exists, system couldn't find&load it
				//this shouldn't happen frequently, only in some corner cases
				//one possibility might be file being pruned due to hitting cache limit
				//after that first check few lines above and actual loading (loading is scheduled and delayed so it's not in same frame)
				if (textureCacheItem != null)
				{
					var rasterTile = new RasterTile(tileId, tilesetId);
					rasterTile.SetTextureFromCache(textureCacheItem.Texture2D);
#if UNITY_EDITOR
					rasterTile.FromCache = CacheType.FileCache;
#endif
					rasterTile.ETag = textureCacheItem.ETag;
					rasterTile.ExpirationDate = textureCacheItem.ExpirationDate.Value;
					TextureReceived(unityTile, rasterTile);
					MapboxAccess.Instance.CacheManager.AddTextureItemToMemory(tilesetId, tileId, textureCacheItem, true);
					MapboxAccess.Instance.CacheManager.MarkFixed(tileId, tilesetId);

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
					EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, string.Empty)
					{
						Callback = () => { FetchingCallback(tileId, tile, unityTile); }
					});
				}
			});

			return;
		}

		//not in cache so web request
		EnqueueForFetching(new FetchInfo(tileId, tilesetId, tile, String.Empty)
		{
			Callback = () => { FetchingCallback(tileId, tile, unityTile); }
		});
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
		MapboxAccess.Instance.CacheManager.MarkFixed(rasterTile.Id, rasterTile.TilesetId);
	}
}