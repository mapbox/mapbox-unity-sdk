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
		var vectorDaraParameters = parameters as VectorDataFetcherParameters;
		if(vectorDaraParameters == null)
		{
			return;
		}
		var vectorTile = (vectorDaraParameters.useOptimizedStyle) ? new VectorTile(vectorDaraParameters.style.Id, vectorDaraParameters.style.Modified) : new VectorTile();
		vectorDaraParameters.tile.AddTile(vectorTile);
		vectorTile.Initialize(_fileSource, vectorDaraParameters.tile.CanonicalTileId, vectorDaraParameters.tilesetId, () =>
		{
			if (vectorDaraParameters.tile.CanonicalTileId != vectorTile.Id)
			{
				//this means tile object is recycled and reused. Returned data doesn't belong to this tile but probably the previous one. So we're trashing it.
				return;
			}
			if (vectorTile.HasError)
			{
				FetchingError(vectorDaraParameters.tile, vectorTile, new TileErrorEventArgs(vectorDaraParameters.tile.CanonicalTileId, vectorTile.GetType(), vectorDaraParameters.tile, vectorTile.Exceptions));
			}
			else
			{
				DataRecieved(vectorDaraParameters.tile, vectorTile);
			}
		});
	}
}
