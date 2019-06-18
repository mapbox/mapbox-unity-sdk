using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataFetcherParameters
{
	public CanonicalTileId canonicalTileId;
	public string tilesetId;
	public UnityTile tile;
}

public class ImageDataFetcherParameters : DataFetcherParameters
{
	public bool useRetina = true;
}

public class TerrainDataFetcherParameters : DataFetcherParameters
{
}

public class VectorDataFetcherParameters : DataFetcherParameters
{
	public bool useOptimizedStyle = false;
	public Style style = null;
}

public abstract class DataFetcher : ScriptableObject
{
	protected MapboxAccess _fileSource;

	public void OnEnable()
	{
		_fileSource = MapboxAccess.Instance;
	}

	public abstract void FetchData(DataFetcherParameters parameters);
}

public class TerrainDataFetcher : DataFetcher
{
	public Action<UnityTile, RawPngRasterTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, RawPngRasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public override void FetchData(DataFetcherParameters parameters)
	{
		var terrainDataParameters = parameters as TerrainDataFetcherParameters;
		if(terrainDataParameters == null)
		{
			return;
		}
		var pngRasterTile = new RawPngRasterTile();
		pngRasterTile.Initialize(_fileSource, terrainDataParameters.canonicalTileId, terrainDataParameters.tilesetId, () =>
		{
			if (terrainDataParameters.tile.CanonicalTileId != pngRasterTile.Id)
			{
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				return;
			}

			if (pngRasterTile.HasError)
			{
				FetchingError(terrainDataParameters.tile, pngRasterTile, new TileErrorEventArgs(terrainDataParameters.canonicalTileId, pngRasterTile.GetType(), null, pngRasterTile.Exceptions));
			}
			else
			{
				DataRecieved(terrainDataParameters.tile, pngRasterTile);
			}
		});
	}
}
