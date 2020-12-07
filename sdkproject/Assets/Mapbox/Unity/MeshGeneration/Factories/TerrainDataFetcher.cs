using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using System.Collections.Generic;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using UnityEngine;

public class TerrainDataFetcher : DataFetcher
{
	public Action<UnityTile, Texture2D> TextureReceived = (t, s) => { };
	public Action<UnityTile, RasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	public override void FetchData(DataFetcherParameters parameters)
	{
		var terrainDataParameters = parameters as TerrainDataFetcherParameters;
		if(terrainDataParameters == null)
		{
			return;
		}

		FetchData(terrainDataParameters.tilesetId, terrainDataParameters.canonicalTileId, false, terrainDataParameters.tile);
	}

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchData(string tilesetId, CanonicalTileId tileId, bool useRetina, UnityTile unityTile = null)
	{
		//MemoryCacheCheck
		var textureItem = MapboxAccess.Instance.CacheManager.GetTextureItemFromMemory(tilesetId, tileId);
		if (textureItem != null)
		{
			TextureReceived(unityTile, textureItem.Texture2D);
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
					TextureReceived(unityTile, textureCacheItem.Texture2D);

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

	private void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useRetina, string etag, UnityTile unityTile = null)
	{
		RasterTile rasterTile;// = GetTileObject(tilesetId);

		if (tilesetId.StartsWith("mapbox://", StringComparison.Ordinal))
		{
			rasterTile = new DemTile();
		}
		else
		{
			rasterTile = new RawPngRasterTile();
		}

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

	private void FetchingCallback(CanonicalTileId tileId, RasterTile rasterTile, UnityTile unityTile = null)
	{
		if (unityTile != null && unityTile.CanonicalTileId != rasterTile.Id)
		{
			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
			return;
		}

		if (rasterTile.HasError)
		{
			FetchingError(unityTile, rasterTile, new TileErrorEventArgs(tileId, rasterTile.GetType(), unityTile, rasterTile.Exceptions));
		}
		else
		{
			MapboxAccess.Instance.CacheManager.AddTextureItem(
				rasterTile.TilesetId,
				rasterTile.Id,
				new TextureCacheItem()
				{
					ETag = rasterTile.ETag,
					Data = rasterTile.Data,
					ExpirationDate = rasterTile.ExpirationDate,
					Texture2D = rasterTile.Texture2D
				},
				true);

			if (rasterTile.StatusCode != 304) //NOT MODIFIED
			{
				TextureReceived(unityTile, rasterTile.Texture2D);
			}
		}

		if (unityTile != null)
		{
			unityTile.RemoveTile(rasterTile);
		}
	}
}

public class TerrainDataFetcherParameters : DataFetcherParameters
{
}