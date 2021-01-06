using System;
using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;

namespace CustomImageLayerSample
{
	public class CustomImageDataFetcher : ImageDataFetcher
	{
		private string _urlFormat;

		public CustomImageDataFetcher(string format)
		{
			_urlFormat = format;
		}

		protected override void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useRetina, string etag, UnityTile unityTile = null)
		{
			var rasterTile = new CustomImageTile(tileId, tilesetId, _urlFormat);

			if (unityTile != null)
			{
				unityTile.AddTile(rasterTile);
			}

			EnqueueForFetching(new FetchInfo()
			{
				TileId = tileId,
				TilesetId = tilesetId,
				RasterTile = rasterTile,
				ETag = etag,
				Callback = () => { FetchingCallback(tileId, rasterTile, unityTile); }
			});
		}
	}

	public class CustomBaseImageDataFetcher : ImageDataFetcher
	{
		private string _urlFormat;

		public CustomBaseImageDataFetcher(string format)
		{
			_urlFormat = format;
		}

		public void FetchData(string tilesetId, CanonicalTileId tileId, bool useRetina, UnityTile unityTile = null)
		{
			//MemoryCacheCheck
			var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tileId);
			if (textureItem != null)
			{
				TextureReceived(
					unityTile,
					new RasterTile(tileId, tilesetId)
					{
						Texture2D = textureItem.Texture2D
					});
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
						TextureReceived(
							unityTile,
							new RasterTile(tileId, tilesetId)
							{
								Texture2D = textureCacheItem.Texture2D
							});
						MapboxAccess.Instance.CacheManager.MarkFixed(tileId, tilesetId);

						//after returning what we already have
						//check if it's out of date, if so check server for update
						if (textureCacheItem.ExpirationDate < DateTime.Now)
						{
							CreateWebRequest(tilesetId, tileId, useRetina, textureCacheItem.ETag, unityTile);
						}
					}
					else
					{
						CreateWebRequest(tilesetId, tileId, useRetina, String.Empty, unityTile);
					}
				});

				return;
			}

			//not in cache so web request
			CreateWebRequest(tilesetId, tileId, useRetina, String.Empty, unityTile);
		}

		protected override void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useRetina, string etag, UnityTile unityTile = null)
		{
			var rasterTile = new CustomImageTile(tileId, tilesetId, _urlFormat);

			if (unityTile != null)
			{
				unityTile.AddTile(rasterTile);
			}

			EnqueueForFetching(new FetchInfo()
			{
				TileId = tileId,
				TilesetId = tilesetId,
				RasterTile = rasterTile,
				ETag = etag,
				Callback = () => { FetchingCallback(tileId, rasterTile, unityTile); }
			});
		}

		protected override void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
		{
			base.FetchingCallback(tileId, rasterTile, unityTile);
			MapboxAccess.Instance.CacheManager.MarkFixed(rasterTile.Id, rasterTile.TilesetId);
		}
	}
}