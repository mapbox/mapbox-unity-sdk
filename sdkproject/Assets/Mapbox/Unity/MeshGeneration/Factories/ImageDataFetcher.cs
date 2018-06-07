using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System;
public class ImageDataFetcher : DataFetcher
{
	public Action<UnityTile, RasterTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, TileErrorEventArgs> FetchingError = (t, s) => { };

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
			if (rasterTile.HasError)
			{
				FetchingError(tile, new TileErrorEventArgs(tile.CanonicalTileId, rasterTile.GetType(), tile, rasterTile.Exceptions));
			}
			else
			{
				DataRecieved(tile, rasterTile);
			}
		});
	}
}
