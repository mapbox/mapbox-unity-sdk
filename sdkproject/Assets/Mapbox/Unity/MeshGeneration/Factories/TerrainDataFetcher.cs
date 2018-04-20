using Mapbox.Map;
using Mapbox.Unity;
using Mapbox.Unity.MeshGeneration.Data;
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
	public Action<RawPngRasterTile, UnityTile> DataRecieved = (s, t) => { };
	public Action<TileErrorEventArgs, UnityTile> FetchingError = (s, t) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchTerrain(CanonicalTileId canonicalTileId, string mapid, UnityTile tile = null) 
	{
		var pngRasterTile = new RawPngRasterTile();
		pngRasterTile.Initialize(_fileSource, canonicalTileId, mapid, () =>
		{
			if (pngRasterTile.HasError)
			{
				FetchingError(new TileErrorEventArgs(canonicalTileId, pngRasterTile.GetType(), null, pngRasterTile.Exceptions), tile);
			}

			DataRecieved(pngRasterTile, tile);
		});
	}
}

public class ImageDataFetcher : DataFetcher
{
	public Action<RasterTile, UnityTile> DataRecieved = (s, t) => { };
	public Action<TileErrorEventArgs, UnityTile> FetchingError = (s, t) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchImage(CanonicalTileId canonicalTileId, string mapid, UnityTile tile = null, bool useRetina = false)
	{
		var pngRasterTile = new RawPngRasterTile();
		pngRasterTile.Initialize(_fileSource, canonicalTileId, mapid, () =>
		{
			RasterTile rasterTile;
			if (mapid.StartsWith("mapbox://", StringComparison.Ordinal))
			{
				rasterTile = useRetina ? new RetinaRasterTile() : new RasterTile();
			}
			else
			{
				rasterTile = useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
			}

			rasterTile.Initialize(_fileSource, tile.CanonicalTileId, mapid, () =>
			{
				if (rasterTile.HasError)
				{
					FetchingError(new TileErrorEventArgs(tile.CanonicalTileId, rasterTile.GetType(), tile, rasterTile.Exceptions), tile);
					return;
				}

				DataRecieved(pngRasterTile, tile);
			});
		});
	}
}
