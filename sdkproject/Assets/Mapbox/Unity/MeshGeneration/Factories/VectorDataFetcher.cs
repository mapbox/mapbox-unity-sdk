using Mapbox.Map;
using Mapbox.Unity.Map;
using Mapbox.Unity.MeshGeneration.Data;
using Mapbox.Unity.MeshGeneration.Enums;
using System;
public class VectorDataFetcher : DataFetcher
{
	public Action<UnityTile, VectorTile> DataRecieved = (t, s) => { };
	public Action<UnityTile, VectorTile, TileErrorEventArgs> FetchingError = (t, r, s) => { };

	//tile here should be totally optional and used only not to have keep a dictionary in terrain factory base
	public override void FetchData(DataFetcherParameters parameters)
	{
		var vectorDataParameters = parameters as VectorDataFetcherParameters;
		if(vectorDataParameters == null)
		{
			return;
		}
		var vectorTile = (vectorDataParameters.useOptimizedStyle) ? new VectorTile(vectorDataParameters.style.Id, vectorDataParameters.style.Modified) : new VectorTile();
		if (vectorDataParameters.tile != null)
		{
			vectorDataParameters.tile.AddTile(vectorTile);
		}
		vectorTile.Initialize(_fileSource, vectorDataParameters.canonicalTileId, vectorDataParameters.tilesetId, () =>
		{
			if (vectorDataParameters.tile != null && vectorDataParameters.tile.CanonicalTileId != vectorTile.Id)
			{
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				return;
			}
			if (vectorTile.HasError)
			{
				FetchingError(vectorDataParameters.tile, vectorTile, new TileErrorEventArgs(vectorDataParameters.canonicalTileId, vectorTile.GetType(), vectorDataParameters.tile, vectorTile.Exceptions));
			}
			else
			{
				DataRecieved(vectorDataParameters.tile, vectorTile);
			}
		});
	}
}
