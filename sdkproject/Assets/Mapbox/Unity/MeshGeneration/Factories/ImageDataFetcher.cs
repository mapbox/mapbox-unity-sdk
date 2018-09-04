using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
using System.Collections.Generic;

public class ImageDataFetcher : DataFetcher
{
	public Action<UnityTile, RasterTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, RasterTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchImage(CanonicalTileId canonicalTileId, string mapid, UnityTile tile = null, bool useRetina = false)
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

		if (tile != null)
		{
			tile.AddTile(rasterTile);
		}

		rasterTile.Initialize(_fileSource, tile.CanonicalTileId, mapid, () =>
		{
			if (tile.CanonicalTileId != rasterTile.Id)
			{
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				return;
			}

			if (rasterTile.HasError)
			{
				FetchingError(tile, rasterTile, new TileErrorEventArgs(tile.CanonicalTileId, rasterTile.GetType(), tile, rasterTile.Exceptions));
			}
			else
			{
				DataRecieved(tile, rasterTile);
			}

		});
	}
}
