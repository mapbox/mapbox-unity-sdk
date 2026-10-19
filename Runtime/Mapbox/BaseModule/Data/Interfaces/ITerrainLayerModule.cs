using System;
using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.Interfaces
{
	public interface ITerrainLayerModule : ILayerModule
	{
		/// <summary>
		/// Elevation at a point, with 0 meaning both "sea level" and "no data". Anything that needs
		/// those two told apart goes through IMapInformation.TryQueryElevation, which the terrain
		/// module assigns on construction.
		/// </summary>
		float QueryElevation(CanonicalTileId tileId, float x, float y);
		IEnumerator LoadTileData(CanonicalTileId tileId, Action<TerrainData> callback = null);
	}
}