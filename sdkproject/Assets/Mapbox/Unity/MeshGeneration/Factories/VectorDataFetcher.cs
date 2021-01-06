using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using Mapbox.Platform.Cache;
using Mapbox.Unity;
using Mapbox.Utils;
using UnityEngine;

public class VectorDataFetcher : DataFetcher
{
	public Action<UnityTile, Mapbox.VectorTile.VectorTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, VectorTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	public override void FetchData(DataFetcherParameters parameters)
	{
		var imageDataParameters = parameters as VectorDataFetcherParameters;
		if(imageDataParameters == null)
		{
			return;
		}

		FetchData(imageDataParameters.tilesetId, imageDataParameters.canonicalTileId, imageDataParameters.useOptimizedStyle, imageDataParameters.style, imageDataParameters.tile);
	}

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchData(string tilesetId, CanonicalTileId tileId, bool useOptimizedStyle, Style optimizedStyle, UnityTile unityTile = null)
	{
		//MemoryCacheCheck
		var dataItem = MapboxAccess.Instance.CacheManager.GetVectorItemFromMemory(tilesetId, tileId);
		if (dataItem != null)
		{
			if (dataItem.VectorTile != null)
			{
				DataRecieved(unityTile, dataItem.VectorTile);
				return;
			}
			else if (dataItem.Data != null)
			{
				Debug.Log("Memory cached vector item has raw data but not decompressed data, this shouldn't ever happen.");
				var decompressed = Compression.Decompress(dataItem.Data);
				var vectorTile = new Mapbox.VectorTile.VectorTile(decompressed);
				DataRecieved(unityTile, vectorTile);
				return;
			}
		}

		if (MapboxAccess.Instance.CacheManager.TileExistsInSqlite(tilesetId, tileId)) //not in memory, check sqlite cache
		{
			MapboxAccess.Instance.CacheManager.GetVectorItemFromSqlite(tilesetId, tileId, (cachedDataItem) =>
			{
				//sqlite stores raw binary data for now so we have to decompress it
				//two things should be done here;
				//(1) store serialized vector tile so we won't need to decompress again
				//(2) decompress async
				//so two lines below should cause a big big perf hit at the moment

				var decompressed = Compression.Decompress(cachedDataItem.Data);
				var vectorTile = new Mapbox.VectorTile.VectorTile(decompressed);
				DataRecieved(unityTile, vectorTile);

				//after returning what we already have
				//check if it's out of date, if so check server for update
				if (cachedDataItem.ExpirationDate < DateTime.Now)
				{
					CreateWebRequest(tilesetId, tileId, useOptimizedStyle, optimizedStyle, cachedDataItem.ETag, unityTile);
				}
			});

			return;
		}

		//not in cache so web request
		CreateWebRequest(tilesetId, tileId, useOptimizedStyle, optimizedStyle,String.Empty, unityTile);
	}

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	// public void FetchData2(DataFetcherParameters parameters)
	// {
	// 	var vectorDaraParameters = parameters as VectorDataFetcherParameters;
	// 	if(vectorDaraParameters == null)
	// 	{
	// 		return;
	// 	}
	// 	var vectorTile = (vectorDaraParameters.useOptimizedStyle) ? new VectorTile(vectorDaraParameters.style.Id, vectorDaraParameters.style.Modified) : new VectorTile();
	//
	// 	if (vectorDaraParameters.tile != null)
	// 	{
	// 		vectorDaraParameters.tile.AddTile(vectorTile);
	// 	}
	//
	// 	vectorTile.Initialize(_fileSource, vectorDaraParameters.tile.CanonicalTileId, vectorDaraParameters.tilesetId, () =>
	// 	{
	// 		if (vectorDaraParameters.tile.CanonicalTileId != vectorTile.Id)
	// 		{
	// 			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
	// 			return;
	// 		}
	// 		if (vectorTile.HasError)
	// 		{
	// 			FetchingError(vectorDaraParameters.tile, vectorTile, new TileErrorEventArgs(vectorDaraParameters.tile.CanonicalTileId, vectorTile.GetType(), vectorDaraParameters.tile, vectorTile.Exceptions));
	// 		}
	// 		else
	// 		{
	// 			DataRecieved(vectorDaraParameters.tile, vectorTile);
	// 		}
	//
	// 		if (vectorDaraParameters.tile != null)
	// 		{
	// 			vectorDaraParameters.tile.RemoveTile(vectorTile);
	// 		}
	// 	});
	// }

	private void CreateWebRequest(string tilesetId, CanonicalTileId tileId, bool useOptimizedStyle, Style optimizedStyle, string etag, UnityTile unityTile = null)
	{
		var vectorTile = (useOptimizedStyle) ? new VectorTile(tileId, tilesetId, optimizedStyle.Id, optimizedStyle.Modified) : new VectorTile(tileId, tilesetId);


		if (unityTile != null)
		{
			unityTile.AddTile(vectorTile);
		}

		EnqueueForFetching(new FetchInfo()
		{
			TileId = tileId,
			TilesetId = tilesetId,
			RasterTile = vectorTile,
			ETag = etag,
			Callback = () => { FetchingCallback(tileId, vectorTile, unityTile); }
		});
	}

	private void FetchingCallback(CanonicalTileId tileId, VectorTile vectorTile, UnityTile unityTile = null)
	{
		if (unityTile != null && unityTile.CanonicalTileId != vectorTile.Id)
		{
			//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
			return;
		}

		if (vectorTile.HasError)
		{
			FetchingError(unityTile, vectorTile, new TileErrorEventArgs(tileId, vectorTile.GetType(), unityTile, vectorTile.Exceptions));
		}
		else
		{
			MapboxAccess.Instance.CacheManager.AddVectorDataItem(
				vectorTile.TilesetId,
				vectorTile.Id,
				new VectorCacheItem()
				{
					ETag = vectorTile.ETag,
					Data = vectorTile.ByteData,
					VectorTile = vectorTile.Data,
					ExpirationDate = vectorTile.ExpirationDate
				},
				true);

			if (vectorTile.StatusCode != 304) //NOT MODIFIED
			{
				DataRecieved(unityTile, vectorTile.Data);
			}
		}

		// if (unityTile != null)
		// {
		// 	unityTile.RemoveTile(vectorTile);
		// }
	}
}

public class VectorDataFetcherParameters : DataFetcherParameters
{
	public bool useOptimizedStyle = false;
	public Style style = null;
}
