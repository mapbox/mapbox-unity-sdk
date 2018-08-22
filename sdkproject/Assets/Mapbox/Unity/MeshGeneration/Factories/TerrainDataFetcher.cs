using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataFetcher : ScriptableObject
{
	protected MapboxAccess _fileSource;

	public void OnEnable()
	{
		_fileSource = MapboxAccess.Instance;
	}
}

public class TerrainDataFetcher : DataFetcher
{
	public Action<UnityTile, RawPngRasterTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, RawPngRasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchTerrain(CanonicalTileId canonicalTileId, string mapid, UnityTile tile = null)
	{
		var pngRasterTile = new RawPngRasterTile();

		pngRasterTile.Initialize(_fileSource, canonicalTileId, mapid, () =>
		{
			if (tile.CanonicalTileId != pngRasterTile.Id)
			{
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				return;
			}

			if (pngRasterTile.HasError)
			{
				FetchingError(tile, pngRasterTile, new TileErrorEventArgs(canonicalTileId, pngRasterTile.GetType(), null, pngRasterTile.Exceptions));
			}
			else
			{
				DataRecieved(tile, pngRasterTile);
			}
		});
	}
}
