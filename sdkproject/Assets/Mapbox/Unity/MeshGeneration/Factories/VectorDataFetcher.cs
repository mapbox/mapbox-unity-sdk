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
	public override void FetchData(DataFetcherParameters parameters)
	{
		var vectorDaraParameters = parameters as VectorDataFetcherParameters;
		if(vectorDaraParameters == null)
		{
			return;
		}
		var vectorTile = (vectorDaraParameters.useOptimizedStyle) ? new VectorTile(vectorDaraParameters.style.Id, vectorDaraParameters.style.Modified) : new VectorTile();
		vectorDaraParameters.tile.AddTile(vectorTile);
		vectorTile.Initialize(_fileSource, vectorDaraParameters.tile.CanonicalTileId, vectorDaraParameters.mapid, () =>
		{
			if (vectorTile.HasError)
			{
				FetchingError(vectorDaraParameters.tile, new TileErrorEventArgs(vectorDaraParameters.tile.CanonicalTileId, vectorTile.GetType(), vectorDaraParameters.tile, vectorTile.Exceptions));
				vectorDaraParameters.tile.VectorDataState = TilePropertyState.Error;
			}
			else
			{
				DataRecieved(vectorDaraParameters.tile, vectorTile);
			}
		});
	}
}
