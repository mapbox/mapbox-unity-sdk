using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
public class VectorDataFetcher : DataFetcher
{
	public Action<UnityTile, VectorTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, TileErrorEventArgs> FetchingError = (t, s) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public void FetchVector(CanonicalTileId canonicalTileId, string mapid, UnityTile tile = null, bool useOptimizedStyle = false, Style style = null)
	{
		var vectorTile = (useOptimizedStyle) ? new VectorTile(style.Id, style.Modified) : new VectorTile();
		tile.AddTile(vectorTile);
		vectorTile.Initialize(_fileSource, tile.CanonicalTileId, mapid, () =>
		{
			if (vectorTile.HasError)
			{
				FetchingError(tile, new TileErrorEventArgs(tile.CanonicalTileId, vectorTile.GetType(), tile, vectorTile.Exceptions));
				tile.VectorDataState = TilePropertyState.Error;
			}
			else
			{
				DataRecieved(tile, vectorTile);
			}
		});
	}
}
