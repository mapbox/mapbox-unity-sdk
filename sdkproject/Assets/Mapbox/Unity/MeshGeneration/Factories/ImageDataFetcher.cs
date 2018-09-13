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
	public override void FetchData(DataFetcherParameters parameters)
	{
		var imageDataParameters = parameters as ImageDataFetcherParameters;
		if(imageDataParameters == null)
		{
			return;
		}
		RasterTile rasterTile;
		if (imageDataParameters.mapid.StartsWith("mapbox://", StringComparison.Ordinal))
		{
			rasterTile = imageDataParameters.useRetina ? new RetinaRasterTile() : new RasterTile();
		}
		else
		{
			rasterTile = imageDataParameters.useRetina ? new ClassicRetinaRasterTile() : new ClassicRasterTile();
		}

		if (imageDataParameters.tile != null)
		{
			imageDataParameters.tile.AddTile(rasterTile);
		}

		rasterTile.Initialize(_fileSource, imageDataParameters.tile.CanonicalTileId, imageDataParameters.mapid, () =>
		{
			if (imageDataParameters.tile.CanonicalTileId != rasterTile.Id)
			{
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				return;
			}

			if (rasterTile.HasError)
			{
				FetchingError(imageDataParameters.tile, rasterTile, new TileErrorEventArgs(imageDataParameters.tile.CanonicalTileId, rasterTile.GetType(), imageDataParameters.tile, rasterTile.Exceptions));
			}
			else
			{
				DataRecieved(imageDataParameters.tile, rasterTile);
			}

		});
	}
}
